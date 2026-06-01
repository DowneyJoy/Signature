using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpSender : MonoBehaviour
{
    public string targetIP = "192.168.10.159";
    public int targetPort = 12346;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isReceiving = true;
    private void Awake()
    {
        targetIP = ConfigManger.LoadInfoString("ipconfig.txt");
    }

    public void Connect()
    {
        if (client != null && client.Connected) return;
        try
        {
            client = new TcpClient();
            client.Connect(targetIP, targetPort);
            client.NoDelay = true;  // 禁用 Nagle，小包立即发送
            stream = client.GetStream();
            
            // 启动接收线程
            receiveThread = new Thread(ReceiveAck);
            receiveThread.Start();
            Debug.Log("TCP 连接成功，已启动确认接收线程");
            Debug.Log("TCP 连接成功");
        }
        catch (Exception e) { Debug.LogError($"连接失败: {e.Message}"); }
    }

    public void SendText(string text)
    {
        if (!EnsureConnected()) return;
        byte[] textData = Encoding.UTF8.GetBytes(text);
        byte[] type = { 0 };
        byte[] len = BitConverter.GetBytes(textData.Length);
        try
        {
            stream.Write(type, 0, 1);
            stream.Write(len, 0, 4);
            stream.Write(textData, 0, textData.Length);
            Debug.Log($"发送文字: {text}");
        }
        catch (Exception e) { HandleSendError(e); }
    }

    public void SendTexture(Texture2D tex)
    {
        if (!EnsureConnected()) return;
        byte[] imageData = tex.EncodeToPNG();
        byte[] type = { 1 };
        byte[] len = BitConverter.GetBytes(imageData.Length);
        try
        {
            stream.Write(type, 0, 1);
            stream.Write(len, 0, 4);
            stream.Write(imageData, 0, imageData.Length);
            Debug.Log($"发送图片: {imageData.Length} 字节");
        }
        catch (Exception e) { HandleSendError(e); }
    }

    public void SendTextAndImage(string text, Texture2D tex)
    {
        if (!EnsureConnected()) return;
        byte[] textData = Encoding.UTF8.GetBytes(text);
        byte[] imageData = tex.EncodeToPNG();

        // 消息体总长度 = 文字长度(4) + 文字数据 + 图片长度(4) + 图片数据
        int bodyLen = 4 + textData.Length + 4 + imageData.Length;
        byte[] buffer = new byte[1 + 4 + bodyLen];
        int offset = 0;

        buffer[offset++] = 2;                                 // 消息类型
        Buffer.BlockCopy(BitConverter.GetBytes(bodyLen), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(textData.Length), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(textData, 0, buffer, offset, textData.Length); offset += textData.Length;
        Buffer.BlockCopy(BitConverter.GetBytes(imageData.Length), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(imageData, 0, buffer, offset, imageData.Length);

        stream.Write(buffer, 0, buffer.Length);
        Debug.Log($"发送图文: 文字=\"{text}\", 图片大小={imageData.Length}");
    }


    private bool EnsureConnected()
    {
        if (client == null || !client.Connected) Connect();
        return client != null && client.Connected;
    }

    private void HandleSendError(Exception e)
    {
        Debug.LogError($"发送失败: {e.Message}");
        CloseConnection();
    }

    // 接收确认消息的线程
    private void ReceiveAck()
    {
        try
        {
            while (isReceiving && client != null && client.Connected)
            {
                if (stream.DataAvailable)
                {
                    // 读取消息类型
                    byte[] typeBuf = new byte[1];
                    if (!ReadExact(stream, typeBuf, 1)) break;
                    byte msgType = typeBuf[0];

                    if (msgType == 3) // 确认消息
                    {
                        byte[] lenBuf = new byte[4];
                        if (!ReadExact(stream, lenBuf, 4)) break;
                        int dataLen = BitConverter.ToInt32(lenBuf, 0);
                        if (dataLen <= 0 || dataLen > 1024) break;
                        byte[] data = new byte[dataLen];
                        if (!ReadExact(stream, data, dataLen)) break;
                        string ackText = Encoding.UTF8.GetString(data);
                        Debug.Log($"[确认消息] {ackText}");
                    }
                }
                Thread.Sleep(10);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"接收确认消息异常: {e.Message}");
        }
    }

// 辅助读取指定长度（复用原有逻辑，需添加超时）
    private bool ReadExact(NetworkStream stream, byte[] buffer, int exactLen, int timeoutMs = 5000)
    {
        int offset = 0;
        DateTime start = DateTime.Now;
        while (offset < exactLen)
        {
            if ((DateTime.Now - start).TotalMilliseconds > timeoutMs) return false;
            int read = stream.Read(buffer, offset, exactLen - offset);
            if (read <= 0) { Thread.Sleep(5); continue; }
            offset += read;
        }
        return true;
    }

// 关闭连接时终止线程
    private void CloseConnection()
    {
        isReceiving = false;
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
        stream = null;
        client = null;
    }

    private void OnDestroy() => CloseConnection();
}