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
                byte[] typeBuf = new byte[1];
                if (!ReadExact(stream, typeBuf, 1)) break;
                byte msgType = typeBuf[0];

                byte[] lenBuf = new byte[4];
                if (!ReadExact(stream, lenBuf, 4)) break;
                int dataLen = BitConverter.ToInt32(lenBuf, 0);
                if (dataLen <= 0 || dataLen > 50 * 1024 * 1024) break;

                byte[] data = new byte[dataLen];
                if (!ReadExact(stream, data, dataLen)) break;

                if (msgType == 0) // 文字
                {
                    string text = Encoding.UTF8.GetString(data);
                    Loom.QueueOnMainThread(() =>
                    {
                        DisplayText(text);
                    });
                }
                else if (msgType == 1) // 图片
                {
                    Loom.QueueOnMainThread(() => DisplayImage(data));
                }
                else if (msgType == 2) // 图文混合消息
                {
                    try
                    {
                        // data 的结构: [文字长度(4字节)][文字内容(变长)][图片长度(4字节)][图片内容(变长)]
                        if (data.Length < 8) throw new Exception("数据太短");

                        int textLen = BitConverter.ToInt32(data, 0);
                        if (textLen <= 0 || textLen > data.Length - 4) throw new Exception("文字长度异常");

                        string text = Encoding.UTF8.GetString(data, 4, textLen);

                        int imgStart = 4 + textLen;
                        if (imgStart + 4 > data.Length) throw new Exception("图片长度字段越界");
                        int imgLen = BitConverter.ToInt32(data, imgStart);
                        if (imgLen <= 0 || imgStart + 4 + imgLen > data.Length) throw new Exception("图片长度异常");

                        byte[] imgData = new byte[imgLen];
                        Buffer.BlockCopy(data, imgStart + 4, imgData, 0, imgLen);

                        // 在主线程更新UI
                        Loom.QueueOnMainThread(() =>
                        {
                            DisplayImage(text,imgData);
                        });
                        
                        // ========== 新增：发送确认消息 ==========
                        string ackMessage = $"已成功接收图片文字信息：{text}";
                        byte[] ackData = Encoding.UTF8.GetBytes(ackMessage);
                        // 确认消息协议: [消息类型=3] + [数据长度(4字节)] + [确认内容]
                        byte[] ackBuffer = new byte[1 + 4 + ackData.Length];
                        ackBuffer[0] = 3; // 自定义类型3表示确认
                        Buffer.BlockCopy(BitConverter.GetBytes(ackData.Length), 0, ackBuffer, 1, 4);
                        Buffer.BlockCopy(ackData, 0, ackBuffer, 5, ackData.Length);
                        stream.Write(ackBuffer, 0, ackBuffer.Length);
                        Debug.Log($"已向发送端返回确认: {ackMessage}");
                        
                        Debug.Log($"收到图文消息: 文字=\"{text}\", 图片大小={imgLen}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"解析复合消息失败: {e.Message}");
                        // 不break，尝试继续处理下一条消息
                    }
                }
            }
        }
        catch (Exception e) { Debug.LogError($"客户端处理异常: {e.Message}"); }
        finally { stream?.Close(); client.Close(); }
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
                });
        }
        else
            Debug.LogError("图片解码失败");
    }
    private async void DisplayImage(string text,byte[] imageData)
    {
        //currentInfo = null;
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(imageData))
        {
            currentInfo.SetName(text);
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
                    //currentInfo = null;
                });
        }
        else
            Debug.LogError("图片解码失败");
    }

    void OnDestroy()
    {
        isRunning = false;
        listener?.Stop();
        listenThread?.Abort();
    }
}