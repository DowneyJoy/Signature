using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using PrimeTween;

public class UDPReceiver : MonoBehaviour
{
    public int listenPort = 12345;
    //public Text displayText;          // 显示文字消息
    //public RawImage displayImage;     // 显示图片

    private UdpClient udpServer;
    private Thread receiveThread;
    private bool isRunning = true;

    public GameObject SignItem;
    public SignItemInfo currentInfo;
    public Transform SignTransform;
    //public List<SignInfo> SignInfos;
    public Sequence showSequence;
    public string content;

    // 存储未完成接收的图片数据
    private class ImagePacketBuffer
    {
        public int totalPackets;
        public byte[][] packets;
        public bool[] received;
        public int lastUpdateTime;
    }
    private Dictionary<int, ImagePacketBuffer> imageBuffers = new Dictionary<int, ImagePacketBuffer>();

    void Start()
    {
        Loom.RunAsync(() => { });
        StartListening();
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
            while (isRunning)
            {
                byte[] data = udpServer.Receive(ref remoteEP);
                if (data.Length == 0) continue;

                byte type = data[0];
                switch (type)
                {
                    case 0x01:   // 文字消息
                        string msg = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                        Loom.QueueOnMainThread(() => DisplayText(msg));
                        break;

                    case 0x02:   // 图片数据包
                        ProcessImagePacket(data);
                        break;
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError("UDP 接收异常: " + ex.Message);
        }
    }

    private void ProcessImagePacket(byte[] packet)
    {
        int imageId = BitConverter.ToInt32(packet, 1);
        int seq = BitConverter.ToInt32(packet, 5);
        int total = BitConverter.ToInt32(packet, 9);
        byte[] payload = new byte[packet.Length - 13];
        Array.Copy(packet, 13, payload, 0, payload.Length);

        if (!imageBuffers.ContainsKey(imageId))
        {
            imageBuffers[imageId] = new ImagePacketBuffer
            {
                totalPackets = total,
                packets = new byte[total][],
                received = new bool[total],
                lastUpdateTime = Environment.TickCount
            };
        }

        var buffer = imageBuffers[imageId];
        if (!buffer.received[seq])
        {
            buffer.packets[seq] = payload;
            buffer.received[seq] = true;
        }

        // 检查是否收齐所有包
        bool allReceived = true;
        for (int i = 0; i < buffer.totalPackets; i++)
        {
            if (!buffer.received[i])
            {
                allReceived = false;
                break;
            }
        }

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

            Loom.QueueOnMainThread(() => DisplayImage(fullImage,buffer.totalPackets));
            imageBuffers.Remove(imageId);
        }
    }

    private void DisplayText(string text)
    {
        //Debug.Log($"收到信息：{text}");
        if(text == "传输测试")
            return;
        //GameObject go = Instantiate(SignItem, SignTransform);
        currentInfo = SignItem.GetComponent<SignItemInfo>();
        currentInfo.SetName(text);
        content = text;
    }

    private async void DisplayImage(byte[] imageData,int packetSize)
    {
        Debug.Log($"收到数据包数量：{packetSize}");
        if(currentInfo==null)
            return;
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);      // 自动识别 PNG/JPG
        currentInfo.SetImage(tex);
        GameObject go = currentInfo.gameObject;
        go.SetActive(true);
        go.transform.localScale = Vector3.zero;
        go.transform.localPosition = new Vector3(0,0,0);
        await FileUtility.SaveInfo(imageData,content);
        //await FileUtility.SaveTextWithUniTask(content);
        showSequence.Stop();
        showSequence = Sequence.Create()
            .Group(Tween.Scale(go.transform, Vector3.one * 1.2f, 1f))
            .Chain(Tween.Scale(go.transform, Vector3.one * 1f, 1f))
            .OnComplete(() =>
            {
                go.SetActive(false);
                SignInfo si = new SignInfo();
                si.InfoContent = currentInfo.SignItemName.text;
                si.InfoImage = currentInfo.SignItemImage.texture;
                ClientRoot.Instance.signInfos.Add(si);
                currentInfo = null;
            });
    }

    void OnDestroy()
    {
        isRunning = false;
        receiveThread?.Abort();
        udpServer?.Close();
    }
    
}