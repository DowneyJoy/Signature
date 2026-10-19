using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UdpImageSender : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint targetEndPoint;
    public string targetIP = "192.168.10.159";
    public int targetPort = 12345;
    public int packetSize = 1400; // 小于 MTU

    void Start()
    {
        udpClient = new UdpClient();
        targetEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
    }

    public void SendTexture(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG(); // 或 EncodeToJPG
        SendBytes(imageBytes);
    }

    public void SendBytes(byte[] data)
    {
        int totalPackets = Mathf.CeilToInt((float)data.Length / packetSize);
        for (int i = 0; i < totalPackets; i++)
        {
            int offset = i * packetSize;
            int len = Mathf.Min(packetSize, data.Length - offset);
            byte[] packet = new byte[len + 8]; // 4字节序号 + 4字节总包数
            byte[] seqBytes = System.BitConverter.GetBytes(i);
            byte[] totalBytes = System.BitConverter.GetBytes(totalPackets);
            System.Array.Copy(seqBytes, 0, packet, 0, 4);
            System.Array.Copy(totalBytes, 0, packet, 4, 4);
            System.Array.Copy(data, offset, packet, 8, len);

            udpClient.Send(packet, packet.Length, targetEndPoint);
        }
        Debug.Log($"发送 {totalPackets} 个包，共 {data.Length} 字节");
    }

    void OnDestroy()
    {
        udpClient?.Close();
    }
}