using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpSender : MonoBehaviour
{
    public string targetIP = "192.168.10.159";
    public int targetPort = 12346;

    [Header("重传配置")]
    public int maxRetryCount = 3;           // 最大重试次数
    public float retryTimeout = 2.0f;       // 等待确认的超时时间（秒）
    public float retryInterval = 1.0f;      // 重传间隔（秒）
    private TcpClient client;
    private NetworkStream stream;
    // 待发送队列（等待发送的原始消息）
    private Queue<MessageToSend> pendingQueue = new Queue<MessageToSend>();
    // 已发送等待确认的消息字典（key 为消息ID）
    private Dictionary<int, MessageToSend> waitingAck = new Dictionary<int, MessageToSend>();
    private int nextMessageId = 1;           // 自增消息ID
    private object syncLock = new object();   // 线程锁

    private Thread sendThread;                // 后台发送线程
    private Thread ackThread;                 // 确认接收线程
    private bool isRunning = true;

    // 消息封装类
    private class MessageToSend
    {
        public int Id;                // 唯一ID
        public int Type;              // 0:文字, 1:图片, 2:图文混合
        public string Text;           // 文字内容（Type=0或2时有效）
        public byte[] ImageData;      // 图片数据（Type=1或2时有效）
        public DateTime SendTime;     // 最后发送时间
        public int RetryCount;        // 已重试次数
        public bool IsConfirmed;      // 是否已确认
    }
    private void Awake()
    {
        targetIP = ConfigManger.LoadInfoString("ipconfig.txt");
        Connect();
        StartBackgroundThreads();
    }

    public void Connect()
    {
        if (client != null && client.Connected) return;
        try
        {
            client = new TcpClient();
            client.Connect(targetIP, targetPort);
            stream = client.GetStream();
            client.NoDelay = true;   // 禁用 Nagle，立即发送
            Debug.Log("TCP 连接成功，已启动重传机制");
        }
        catch (Exception e)
        {
            Debug.LogError($"连接失败: {e.Message}");
        }
    }
    private void StartBackgroundThreads()
    {
        sendThread = new Thread(SendLoop);
        sendThread.Start();
        ackThread = new Thread(ReceiveAckLoop);
        ackThread.Start();
    }
    // ---------- 对外发送接口（自动进入重传队列）----------
    public void SendText(string text)
    {
        lock (syncLock)
        {
            var msg = new MessageToSend
            {
                Id = nextMessageId++,
                Type = 0,
                Text = text,
                SendTime = DateTime.MinValue,
                RetryCount = 0
            };
            pendingQueue.Enqueue(msg);
            Debug.Log($"文字消息已加入待发送队列: {text}");
        }
    }

    public void SendTexture(Texture2D tex)
    {
        byte[] imageData = tex.EncodeToPNG();
        lock (syncLock)
        {
            var msg = new MessageToSend
            {
                Id = nextMessageId++,
                Type = 1,
                ImageData = imageData,
                SendTime = DateTime.MinValue,
                RetryCount = 0
            };
            pendingQueue.Enqueue(msg);
            Debug.Log($"图片消息已加入待发送队列，大小: {imageData.Length} 字节");
        }
    }

    public void SendTextAndImage(string text, Texture2D tex)
    {
        byte[] imageData = tex.EncodeToPNG();
        lock (syncLock)
        {
            var msg = new MessageToSend
            {
                Id = nextMessageId++,
                Type = 2,
                Text = text,
                ImageData = imageData,
                SendTime = DateTime.MinValue,
                RetryCount = 0
            };
            pendingQueue.Enqueue(msg);
            Debug.Log($"图文混合消息已加入待发送队列: {text}");
        }
    }

    // ---------- 后台发送线程 ----------
    private void SendLoop()
    {
        while (isRunning)
        {
            // 1. 从待发送队列取出消息立即发送
            MessageToSend msgToSend = null;
            lock (syncLock)
            {
                if (pendingQueue.Count > 0)
                    msgToSend = pendingQueue.Dequeue();
            }

            if (msgToSend != null)
            {
                // 尝试发送
                bool success = SendMessageInternal(msgToSend);
                if (success)
                {
                    msgToSend.SendTime = DateTime.Now;
                    lock (syncLock)
                    {
                        waitingAck[msgToSend.Id] = msgToSend;
                    }
                    Debug.Log($"消息 [{msgToSend.Id}] 已发送，等待确认");
                }
                else
                {
                    // 发送失败（如连接断开），重新放回队列头部稍后重试
                    Debug.LogWarning($"消息 [{msgToSend.Id}] 发送失败，稍后重试");
                    lock (syncLock)
                    {
                        // 放回队列头部（使用临时队列或直接 Enqueue 到尾部，简单处理放尾部）
                        pendingQueue.Enqueue(msgToSend);
                    }
                    Thread.Sleep(1000);
                }
            }

            // 2. 检查超时未确认的消息，进行重传
            List<MessageToSend> toRetry = new List<MessageToSend>();
            lock (syncLock)
            {
                foreach (var kv in waitingAck.Values)
                {
                    if (!kv.IsConfirmed && (DateTime.Now - kv.SendTime).TotalSeconds > retryTimeout)
                    {
                        toRetry.Add(kv);
                    }
                }
            }

            foreach (var msg in toRetry)
            {
                lock (syncLock)
                {
                    if (msg.RetryCount >= maxRetryCount)
                    {
                        Debug.LogError($"消息 [{msg.Id}] 重试 {maxRetryCount} 次仍无确认，丢弃");
                        waitingAck.Remove(msg.Id);
                    }
                    else
                    {
                        msg.RetryCount++;
                        // 重新发送
                        if (SendMessageInternal(msg))
                        {
                            msg.SendTime = DateTime.Now;
                            Debug.Log($"消息 [{msg.Id}] 重传第 {msg.RetryCount} 次");
                        }
                        else
                        {
                            Debug.LogWarning($"消息 [{msg.Id}] 重传发送失败，稍后继续");
                        }
                    }
                }
            }

            Thread.Sleep((int)(retryInterval * 1000));
        }
    }

    // 底层发送（增加消息 ID 字段）
private bool SendMessageInternal(MessageToSend msg)
{
    EnsureConnected();
    if (stream == null || !client.Connected) return false;

    try
    {
        if (msg.Type == 0) // 纯文字
        {
            byte[] textData = Encoding.UTF8.GetBytes(msg.Text);
            // 协议: 类型(1) + MsgId(4) + 长度(4) + 文字数据
            byte[] buffer = new byte[1 + 4 + 4 + textData.Length];
            int offset = 0;
            buffer[offset++] = 0;                                           // 类型
            Buffer.BlockCopy(BitConverter.GetBytes(msg.Id), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(textData.Length), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(textData, 0, buffer, offset, textData.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
        else if (msg.Type == 1) // 纯图片
        {
            byte[] imgData = msg.ImageData;
            byte[] buffer = new byte[1 + 4 + 4 + imgData.Length];
            int offset = 0;
            buffer[offset++] = 1;
            Buffer.BlockCopy(BitConverter.GetBytes(msg.Id), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(imgData.Length), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(imgData, 0, buffer, offset, imgData.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
        else if (msg.Type == 2) // 图文混合
        {
            byte[] textData = Encoding.UTF8.GetBytes(msg.Text);
            byte[] imgData = msg.ImageData;
            int bodyLen = 4 + textData.Length + 4 + imgData.Length;   // 文字长+文字+图片长+图片
            byte[] buffer = new byte[1 + 4 + 4 + bodyLen];             // 类型+MsgId+体长+body
            int offset = 0;
            buffer[offset++] = 2;
            Buffer.BlockCopy(BitConverter.GetBytes(msg.Id), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(bodyLen), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(textData.Length), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(textData, 0, buffer, offset, textData.Length); offset += textData.Length;
            Buffer.BlockCopy(BitConverter.GetBytes(imgData.Length), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(imgData, 0, buffer, offset, imgData.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
        return true;
    }
    catch (Exception e)
    {
        Debug.LogError($"发送异常: {e.Message}");
        CloseConnection();
        return false;
    }
}

// 接收确认消息（解析消息 ID）
private void ReceiveAckLoop()
{
    while (isRunning)
    {
        if (stream != null && client != null && client.Connected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    byte[] typeBuf = new byte[1];
                    if (!ReadExact(stream, typeBuf, 1)) continue;
                    byte msgType = typeBuf[0];

                    if (msgType == 3) // 确认消息
                    {
                        // 读取消息 ID (4 字节)
                        byte[] idBuf = new byte[4];
                        if (!ReadExact(stream, idBuf, 4)) continue;
                        int msgId = BitConverter.ToInt32(idBuf, 0);

                        // 读取长度
                        byte[] lenBuf = new byte[4];
                        if (!ReadExact(stream, lenBuf, 4)) continue;
                        int dataLen = BitConverter.ToInt32(lenBuf, 0);
                        if (dataLen < 0 || dataLen > 1024) continue;
                        byte[] data = new byte[dataLen];
                        if (dataLen > 0 && !ReadExact(stream, data, dataLen)) continue;
                        string ackText = dataLen > 0 ? Encoding.UTF8.GetString(data) : "";

                        Debug.Log($"[收到确认] MsgId={msgId}, 内容={ackText}");

                        // 精确匹配并移除等待队列中的消息
                        lock (syncLock)
                        {
                            if (waitingAck.ContainsKey(msgId))
                            {
                                waitingAck[msgId].IsConfirmed = true;
                                waitingAck.Remove(msgId);
                                Debug.Log($"消息 {msgId} 确认成功，已从等待队列移除");
                            }
                            else
                            {
                                Debug.LogWarning($"收到未知消息ID的确认: {msgId}");
                            }
                        }
                    }
                    else
                    {
                        // 忽略其他消息，但需要读取完整数据以避免粘包
                        // 先读取消息 ID（4 字节），再读长度（4 字节），然后跳过数据
                        byte[] idBuf = new byte[4];
                        if (!ReadExact(stream, idBuf, 4)) continue;
                        byte[] lenBuf = new byte[4];
                        if (!ReadExact(stream, lenBuf, 4)) continue;
                        int dataLen = BitConverter.ToInt32(lenBuf, 0);
                        if (dataLen > 0 && dataLen < 1024 * 1024)
                        {
                            byte[] dump = new byte[dataLen];
                            ReadExact(stream, dump, dataLen);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"确认接收线程异常: {e.Message}");
            }
        }
        Thread.Sleep(50);
    }
}

    // 确保连接有效
    private void EnsureConnected()
    {
        if (client == null || !client.Connected)
        {
            Connect();
            Thread.Sleep(500); // 等待连接建立
        }
    }

    
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

    private void CloseConnection()
    {
        stream?.Close();
        client?.Close();
        stream = null;
        client = null;
    }

    private void OnDestroy()
    {
        isRunning = false;
        // 等待线程自然退出，避免使用 Thread.Abort()
        if (sendThread != null && sendThread.IsAlive)
            sendThread.Join(3000);
        if (ackThread != null && ackThread.IsAlive)
            ackThread.Join(3000);
        CloseConnection();
    }
}