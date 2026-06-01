using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UDPSender : MonoBehaviour
{
    public string targetIP = "192.168.10.159";   // 广播地址或指定接收端IP
    public int targetPort = 12345;
    public int packetSize = 1400;               // 小于 MTU (1472)

    private UdpClient udpClient;
    private IPEndPoint targetEndPoint;

    // UI 相关（示例）
    // public InputField messageInput;
    // public Button sendTextButton;
    // public Button sendImageButton;
    public RawImage previewImage;               // 显示要发送的图片

    private Texture2D currentTexture;

    void Start()
    {
        udpClient = new UdpClient();
        targetIP = ConfigManger.LoadInfoString("ipconfig.txt");
        targetEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);

        // sendTextButton.onClick.AddListener(() => SendMessage(messageInput.text));
        // sendImageButton.onClick.AddListener(() => SelectAndSendImage());
    }

    /// <summary>发送文字消息</summary>
    public void SendMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        byte[] data = Encoding.UTF8.GetBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = 0x01;                    // 类型：文字
        Array.Copy(data, 0, packet, 1, data.Length);
        udpClient.Send(packet, packet.Length, targetEndPoint);
        Debug.Log($"发送文字: {msg}");
    }

    /// <summary>选择并发送图片（通过 NativeFilePicker 或直接使用 Texture）</summary>
    public void SelectAndSendImage()
    {
        // 实际项目中可使用 NativeFilePicker 或从相机/相册获取
        // 这里演示从 Resources 加载或使用已有 Texture2D
        // 示例：从 StreamingAssets 加载一个测试图片
        StartCoroutine(LoadAndSendImage());
    }

    IEnumerator LoadAndSendImage()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.jpg");
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(path))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
                SendTexture(tex);
                previewImage.texture = tex;
            }
        }
    }

    /// <summary>发送图片（自动分包）</summary>
    public void SendTexture(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG();   // 或 EncodeToJPG
        SendImageBytes(imageBytes);
    }

    private void SendImageBytes(byte[] imageData)
    {
        int totalPackets = Mathf.CeilToInt((float)imageData.Length / packetSize);
        int imageId = UnityEngine.Random.Range(0, 10000);   // 简易图片ID

        for (int i = 0; i < totalPackets; i++)
        {
            int offset = i * packetSize;
            int len = Mathf.Min(packetSize, imageData.Length - offset);
            // 包结构: 类型(1) + 图片ID(4) + 序号(4) + 总包数(4) + 数据
            byte[] packet = new byte[1 + 4 + 4 + 4 + len];
            packet[0] = 0x02;   // 类型：图片数据包
            Array.Copy(BitConverter.GetBytes(imageId), 0, packet, 1, 4);
            Array.Copy(BitConverter.GetBytes(i), 0, packet, 5, 4);
            Array.Copy(BitConverter.GetBytes(totalPackets), 0, packet, 9, 4);
            Array.Copy(imageData, offset, packet, 13, len);

            udpClient.Send(packet, packet.Length, targetEndPoint);
        }
        //Debug.Log($"图片发送完成，总包数: {totalPackets}, 大小: {imageData.Length} 字节");
    }

    void OnDestroy()
    {
        udpClient?.Close();
    }
}