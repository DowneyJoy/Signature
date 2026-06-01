using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.IO;
using UnityEngine.UI;
//******* 此脚本作为服务器端,测试接收信息和图片，并把图片在视图显示*******
public class UDPSevPhoto : MonoBehaviour
{

    public static UDPSevPhoto instance;
    Socket socket; //目标socket
    EndPoint clientEnd; //客户端
    IPEndPoint ipEnd; //侦听端口
    [HideInInspector]
    public string recvStr; //接收的字符串
    string sendStr; //发送的字符串

    byte[] recvData; //接收的数据，必须为字节
    byte[] sendData = new byte[1024]; //发送的数据，必须为字节
    int recvLen; //接收的数据长度
    Thread connectThread; //连接线程
    [HideInInspector]
    public bool isStartSend = false;
    int port;

    bool isSendImage;
    public delegate void UDPServerDeledate(Texture2D byths);
    public event UDPServerDeledate UDPserverEvent;

    //接收到的图片字节数组的图片字节长度
    int imageLength;

    string imageStr;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        port           = 7788;
        isServerActive = true;
        InitSocket(); //在这里初始化server
    }

    void Update()
    {
        timerInterval += Time.deltaTime;
        if (isStartCheck)
        {
            if (timerInterval > 6f)
            {
                print("网络连接异常");
                timerInterval = 0f;
            }
        }

        if (isSendImage)
        {
            ParseBYTeArr(newConbineStr);
            newConbineStr = null;
            isSendImage   = false;
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    void InitSocket()
    {
        //定义侦听端口,侦听任何IP 
        ipEnd = new IPEndPoint(IPAddress.Any, port);
        //定义套接字类型,在主线程中定义 
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        //服务端需要绑定ip 
        socket.Bind(ipEnd);
        //定义客户端
        IPEndPoint sender = new IPEndPoint(IPAddress.Broadcast, 0);
        clientEnd = (EndPoint)sender;
        print("local：等待连接数据");
        //开启一个线程连接
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }

    /// <summary>
    /// 服务器向客户端发送消息
    /// </summary>
    /// <param name="sendStr"></param>
    public void SocketSend(string sendStr)
    {
        //清空发送缓存 
        sendData = new byte[20000];
        //数据类型转换 
        sendData = Encoding.UTF8.GetBytes(sendStr);
        //发送给指定客户端 
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, clientEnd);
    }

    bool isServerActive = false;

    /// <summary>
    /// 服务器接收来自客户端的消息
    /// </summary>
    void SocketReceive()
    {
        //进入接收循环 
        while (isServerActive)
        {
            //对data清零 
            recvData = new byte[1500];
            try
            {
                //获取服务端端数据
                recvLen = socket.ReceiveFrom(recvData, ref clientEnd);
                if (isServerActive == false)
                {
                    break;
                }
            }
            catch
            {

            }
            if (recvLen > 0)
            {
                recvStr = Encoding.UTF8.GetString(recvData, 0, recvLen);
                //输出接收到的数据
                if (recvStr == "alive")
                {
                    HeartCheck();
                }
                //此段是发送图片端在发送图片后给出的一条识别图片信息的信号标记
                else if (recvStr == "这是图片")
                {
                    //当接收到的信息为这是图片时，判断接收到的图片包数量是否够，不够就发送未收到的包的标识号，让客户端再发送一下
                    CheckPackage();

                }
                else if (recvLen > 18) //图片包头为29个字节
                {
                    //合并发来的图片
                    ConmbineString(recvStr);
                }
            }
        }
    }

    //未发送包的标识号
    string reSendPackageindex;
    /// <summary>
    /// 当接收到客户端发送的“这是图片”消息后，判断接收到的图片包是否完整
    /// </summary>
    void CheckPackage()
    {
        reSendPackageindex = null;
        if (doneIndex.Count <= 0)
        {
            print("接收成功");
            for (int i = 0; i < newImageDic.Count; i++)
            {
                if (newImageDic.TryGetValue(i, out dicStr))
                {
                    newConbineStr = newConbineStr + dicStr;
                }
            }
            isSendImage = true;
            newImageCount = 0;
            newStrIndex = 0;
            isFirst = true;
            newImageDic.Clear();
            doneIndex.Clear();
        }
        else
        {
            print("接收失败，重新请求");
            //判断哪些包没有收到
            for (int i = 0; i < doneIndex.Count; i++)
            {
                reSendPackageindex = doneIndex[i] + "_" + reSendPackageindex;
            }

            SocketSend(reSendPackageindex);
            print("请求发送未成功包");
        }
    }

    string newConbineStr;
    string newImageName;
    int newImageCount = 0;
    int newStrIndex = 0;
    string newImageMessage;
    //判断是否是第一次接受消息
    bool isFirst = true;
    string oldImageName;
    Dictionary<int, string> newImageDic = new Dictionary<int, string>();
    List<int> doneIndex = new List<int>();
    string dicStr;
    //将分包发来的消息合成一个包
    void ConmbineString(string perStr)
    {
        //0.图片名字（21字节）--1.包的长度（1000为起始点，4字节）--2.包的下标（1000为起始点4个字节）--3.包的内容
        //分割字符串 "_"
        string[] strs = perStr.Split('_');
        //名字
        newImageName = strs[0];
        newImageCount = int.Parse(strs[1]) - 1000;
        newStrIndex = int.Parse(strs[2]) - 1000;
        newImageMessage = strs[3];

        if (isFirst)
        {
            oldImageName = newImageName;
            isFirst = false;
            newConbineStr = null;
            //将将要收到的包的标识号存进集合里边，每接收到对应的数据就移除该标识号
            for (int i = 0; i < newImageCount; i++)
            {
                doneIndex.Add(i);
            }
        }

        if (newImageName == oldImageName)
        {
            if (!newImageDic.ContainsKey(newStrIndex))
            {
                //每接收到对应的数据就移除该标识号
                try
                {
                    doneIndex.Remove(newStrIndex);
                }
                catch
                {
                    print("数据传输失败");
                }

                newImageDic.Add(newStrIndex, newImageMessage);
            }
        }
    }

    float timerInterval = 0;
    bool isStartCheck = false;
    void HeartCheck()
    {
        isStartCheck = true;
        timerInterval = 0f;
        SocketSend("keeping");
        print("连接正常");
    }

    /// <summary>
    /// 发来的字节包括：图片的字节长度（前四个字节）和图片字节
    /// 得到发来的字节中图片字节长度和图片字节数组
    /// </summary>
    void ParseBYTeArr(string receStr)
    {
        byte[] bytes = Convert.FromBase64String(receStr);

        string timestamp = GetTimeStamp().ToString();
        string filename = "Assets/UDPPhoto/" + timestamp + "UDP.jpg";//把接收到的UDP图片存到本地资源文件夹下
        File.WriteAllBytes(filename, bytes);

        Texture2D tex2D = new Texture2D(100, 100);
        tex2D.LoadImage(bytes);

        if (UDPserverEvent != null)
        {
            UDPserverEvent(tex2D);
        }
    }

    //连接关闭
    void SocketQuit()
    {
        //最后关闭socket
        if (socket != null)
        {
            try
            {
                socket.Close();
            }
            catch
            {

            }
        }
        Debug.LogWarning("local：断开连接");
    }

    void OnDisable()
    {
        isServerActive = false;
        SocketQuit();
        Thread.Sleep(100);
    }

    public static long GetTimeStamp(bool bflag = true)
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        long ret;
        if (bflag)
            ret = Convert.ToInt64(ts.TotalSeconds);
        else
            ret = Convert.ToInt64(ts.TotalMilliseconds);
        return ret;
    }
}
