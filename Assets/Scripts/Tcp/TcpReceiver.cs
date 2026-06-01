using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class TcpReceiver : MonoBehaviour
{
    public int listenPort = 12346;

    private TcpListener listener;
    private Thread listenThread;
    private bool isRunning = true;

    public GameObject SignItem;
    public SignItemInfo currentInfo;
    public Transform SignTransform;
    //public List<SignInfo> SignInfos;
    public Sequence showSequence;
    public string content;
    void Start() => StartListener();

    void StartListener()
    {
        listenThread = new Thread(ListenLoop);
        listenThread.Start();
    }

    void ListenLoop()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();
            Debug.Log($"TCP 接收端启动，端口 {listenPort}");
            while (isRunning)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        catch (Exception e) { Debug.LogError($"监听异常: {e.Message}"); }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = null;
        try
        {
            stream = client.GetStream();
            while (isRunning && client.Connected)
            {
                // 1. 读取消息类型
                byte[] typeBuf = new byte[1];
                if (!ReadExact(stream, typeBuf, 1)) break;
                byte msgType = typeBuf[0];

                // 2. 读取消息 ID（4 字节）
                byte[] idBuf = new byte[4];
                if (!ReadExact(stream, idBuf, 4)) break;
                int msgId = BitConverter.ToInt32(idBuf, 0);

                if (msgType == 0) // 纯文字
                {
                    byte[] lenBuf = new byte[4];
                    if (!ReadExact(stream, lenBuf, 4)) break;
                    int dataLen = BitConverter.ToInt32(lenBuf, 0);
                    if (dataLen <= 0 || dataLen > 1024 * 1024) break;
                    byte[] data = new byte[dataLen];
                    if (!ReadExact(stream, data, dataLen)) break;
                    string text = Encoding.UTF8.GetString(data);

                    Loom.QueueOnMainThread(() => DisplayText(text));
                    Debug.Log($"收到文字 (MsgId={msgId}): {text}");

                    // 发送确认（可选：携带文字内容）
                    SendAck(stream, msgId, $"已收到文字：{text}");
                }
                else if (msgType == 1) // 纯图片
                {
                    byte[] lenBuf = new byte[4];
                    if (!ReadExact(stream, lenBuf, 4)) break;
                    int dataLen = BitConverter.ToInt32(lenBuf, 0);
                    if (dataLen <= 0 || dataLen > 50 * 1024 * 1024) break;
                    byte[] imgData = new byte[dataLen];
                    if (!ReadExact(stream, imgData, dataLen)) break;

                    Loom.QueueOnMainThread(() => DisplayImage(imgData));
                    Debug.Log($"收到图片 (MsgId={msgId})，大小={dataLen}");

                    SendAck(stream, msgId, $"已收到图片，大小{dataLen}");
                }
                else if (msgType == 2) // 图文混合
                {
                    // 读取体长（4 字节）
                    byte[] bodyLenBuf = new byte[4];
                    if (!ReadExact(stream, bodyLenBuf, 4)) break;
                    int bodyLen = BitConverter.ToInt32(bodyLenBuf, 0);
                    if (bodyLen <= 0 || bodyLen > 100 * 1024 * 1024) break;

                    // 读取文字长度
                    byte[] textLenBuf = new byte[4];
                    if (!ReadExact(stream, textLenBuf, 4)) break;
                    int textLen = BitConverter.ToInt32(textLenBuf, 0);
                    if (textLen < 0 || textLen > bodyLen - 8) break;

                    byte[] textData = new byte[textLen];
                    if (!ReadExact(stream, textData, textLen)) break;
                    string text = Encoding.UTF8.GetString(textData);

                    // 读取图片长度
                    byte[] imgLenBuf = new byte[4];
                    if (!ReadExact(stream, imgLenBuf, 4)) break;
                    int imgLen = BitConverter.ToInt32(imgLenBuf, 0);
                    if (imgLen < 0 || imgLen > bodyLen - 4 - textLen) break;

                    byte[] imgData = new byte[imgLen];
                    if (!ReadExact(stream, imgData, imgLen)) break;

                    Loom.QueueOnMainThread(() =>
                    {
                        DisplayImage(text,imgData);
                    });
                    Debug.Log($"收到图文消息 (MsgId={msgId}): 文字=\"{text}\", 图片大小={imgLen}");

                    SendAck(stream, msgId, $"已成功接收图片文字信息：{text}");
                }
                else
                {
                    // 未知类型，丢弃该消息（需要读取完剩余数据）
                    byte[] lenBuf = new byte[4];
                    if (!ReadExact(stream, lenBuf, 4)) break;
                    int dataLen = BitConverter.ToInt32(lenBuf, 0);
                    if (dataLen > 0 && dataLen < 1024 * 1024)
                    {
                        byte[] dump = new byte[dataLen];
                        ReadExact(stream, dump, dataLen);
                    }
                    Debug.LogWarning($"未知消息类型 {msgType}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"客户端处理异常: {e.Message}");
        }
        finally
        {
            stream?.Close();
            client.Close();
        }
    }

    // 发送确认消息（包含消息 ID）
    private void SendAck(NetworkStream stream, int msgId, string ackText)
    {
        try
        {
            byte[] ackData = Encoding.UTF8.GetBytes(ackText);
            // 协议: 类型(1) + MsgId(4) + 长度(4) + 文本
            byte[] buffer = new byte[1 + 4 + 4 + ackData.Length];
            int offset = 0;
            buffer[offset++] = 3;   // 确认类型
            Buffer.BlockCopy(BitConverter.GetBytes(msgId), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(ackData.Length), 0, buffer, offset, 4); offset += 4;
            Buffer.BlockCopy(ackData, 0, buffer, offset, ackData.Length);
            stream.Write(buffer, 0, buffer.Length);
            Debug.Log($"发送确认: MsgId={msgId}, 内容={ackText}");
        }
        catch (Exception e)
        {
            Debug.LogError($"发送确认失败: {e.Message}");
        }
    }

    private bool ReadExact(NetworkStream stream, byte[] buffer, int exactLen, int timeoutMs = 5000)
    {
        int offset = 0;
        DateTime start = DateTime.Now;
        while (offset < exactLen)
        {
            // 检查是否超时
            if ((DateTime.Now - start).TotalMilliseconds > timeoutMs)
            {
                Debug.LogError($"读取超时，需要 {exactLen} 字节，已读 {offset} 字节");
                return false;
            }
            int read = stream.Read(buffer, offset, exactLen - offset);
            if (read == 0)
            {
                // 对方关闭连接
                return false;
            }
            offset += read;
        }
        return true;
    }
    private void DisplayText(string text)
    {
        Debug.Log($"收到文字: {text}");
        currentInfo.SetName(text);
        content = text;
    }

    private async void DisplayImage(byte[] imageData)
    {
        Texture2D tex = new Texture2D(2, 2);
        try
        {
            if (tex.LoadImage(imageData))
            {
                currentInfo.SetImage(tex);
                GameObject go = currentInfo.gameObject;
                go.SetActive(true);
                go.transform.localScale = Vector3.zero;
                go.transform.localPosition = new Vector3(0,0,0);
                await FileUtility.SaveInfo(imageData,content);
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
                        // 释放 Texture2D 资源
                        if (tex != null)
                            UnityEngine.Object.Destroy(tex);
                    });
            }
            else
            {
                Debug.LogError("图片解码失败");
                UnityEngine.Object.Destroy(tex);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DisplayImage 异常: {e.Message}");
            UnityEngine.Object.Destroy(tex);
        }
    }
    private async void DisplayImage(string text,byte[] imageData)
    {
        Texture2D tex = new Texture2D(2, 2);
        try
        {
            if (tex.LoadImage(imageData))
            {
                currentInfo.SetName(text);
                currentInfo.SetImage(tex);
                GameObject go = currentInfo.gameObject;
                go.SetActive(true);
                go.transform.localScale = Vector3.zero;
                go.transform.localPosition = new Vector3(0,0,0);
                content = text;
                await FileUtility.SaveInfo(imageData,content);
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
                        // 释放 Texture2D 资源
                        // if (tex != null)
                            // UnityEngine.Object.Destroy(tex);
                    });
            }
            else
            {
                Debug.LogError("图片解码失败");
                UnityEngine.Object.Destroy(tex);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DisplayImage 异常: {e.Message}");
            UnityEngine.Object.Destroy(tex);
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        listener?.Stop();
        listenThread?.Abort();
    }
}