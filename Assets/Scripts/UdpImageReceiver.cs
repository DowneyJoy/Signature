using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class UdpImageReceiver : MonoBehaviour
{
    public int listenPort = 12345;
    public RawImage displayImage; // 用于显示图片
    private UdpClient udpServer;
    private Thread receiveThread;
    private Dictionary<int, PacketBuffer> packetBuffers = new Dictionary<int, PacketBuffer>();
    private int currentImageId = 0;
    class PacketBuffer
    {
        public int totalPackets;
        public byte[][] packets;
        public bool[] received;
        public int lastUpdateTime;
    }

    void Start()
    {
        StartListening();
        Loom.RunAsync(() => { });
    }

    void StartListening()
    {
        udpServer = new UdpClient(listenPort);
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.Start();
    }

    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                byte[] data = udpServer.Receive(ref remoteEP);
                // 解析包头
                int seq = BitConverter.ToInt32(data, 0);
                int total = BitConverter.ToInt32(data, 4);
                byte[] payload = new byte[data.Length - 8];
                Array.Copy(data, 8, payload, 0, payload.Length);

                int imageKey = currentImageId; // 实际应该用发送端标识 + 图片序列号，简化用全局ID
                if (!packetBuffers.ContainsKey(imageKey))
                {
                    packetBuffers[imageKey] = new PacketBuffer
                    {
                        totalPackets = total,
                        packets = new byte[total][],
                        received = new bool[total],
                        lastUpdateTime = Environment.TickCount
                    };
                }
                var buffer = packetBuffers[imageKey];
                if (!buffer.received[seq])
                {
                    buffer.packets[seq] = payload;
                    buffer.received[seq] = true;
                }

                // 检查是否收齐所有包
                bool allReceived = true;
                for (int i = 0; i < buffer.totalPackets; i++)
                    if (!buffer.received[i]) { allReceived = false; break; }

                if (allReceived)
                {
                    // 合并数据
                    int totalSize = 0;
                    for (int i = 0; i < buffer.totalPackets; i++)
                        totalSize += buffer.packets[i].Length;
                    byte[] fullImage = new byte[totalSize];
                    int offset = 0;
                    for (int i = 0; i < buffer.totalPackets; i++)
                    {
                        Array.Copy(buffer.packets[i], 0, fullImage, offset, buffer.packets[i].Length);
                        offset += buffer.packets[i].Length;
                    }
                    // 在主线程显示图片
                    Loom.QueueOnMainThread(() => DisplayImage(fullImage));
                    packetBuffers.Remove(imageKey);
                    currentImageId++; // 下一张图片使用新ID
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("接收异常: " + e);
        }
    }

    void DisplayImage(byte[] imageData)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);
        displayImage.texture = tex;
    }

    void OnDestroy()
    {
        receiveThread?.Abort();
        udpServer?.Close();
    }
}