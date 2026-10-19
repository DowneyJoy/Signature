using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI; 

public class UDPServer : MonoBehaviour
{
    //以下默认都是私有的成员 
    Socket     socket;                    //目标socket 
    EndPoint   clientEnd;                 //客户端 
    IPEndPoint ipEnd;                     //侦听端口 
    string     recvStr;                   //接收的字符串 
    string     sendStr;                   //发送的字符串 
    byte[]     recvData = new byte[2048]; //接收的数据，必须为字节 
    byte[]     sendData = new byte[2048]; //发送的数据，必须为字节 
    int        recvLen;                   //接收的数据长度 
    Thread connectThread;                 //连接线程

    public Text text;


    void Start()
    {
        text.GetComponent<Text>();
        InitSocket(); //在这里初始化server
        
    }

    public void Update()
    {
        text.text = "" + recvStr;
    }

    //初始化
    void InitSocket()
    {
        //定义侦听端口,侦听任何IP
        ipEnd = new IPEndPoint(IPAddress.Any, 7788);
        //定义套接字类型,在主线程中定义
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //服务端需要绑定ip
        socket.Bind(ipEnd);
        //定义客户端
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

        clientEnd = (EndPoint)sender;
        print("waiting for UDP dgram");
        //开启一个线程连接，必须的，否则主线程卡死
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }

    void SocketSend(string sendStr)
    {
        //清空发送缓存
        sendData = new byte[2048];
        //数据类型转换
        sendData = Encoding.UTF8.GetBytes(sendStr);
        //发送给指定客户端
        socket.SendTo(sendData,  clientEnd);
    }

    //服务器接收
    void SocketReceive()
    {
        //进入接收循环
        while (true)
        {
            //对data清零
            recvData = new byte[2048];
            //获取客户端，获取客户端数据，用引用给客户端赋值
            recvLen = socket.ReceiveFrom(recvData, ref clientEnd);
            print("message from: " + clientEnd.ToString()); //打印客户端信息
            //输出接收到的数据
            recvStr = Encoding.UTF8.GetString(recvData, 0, recvLen);

            print("我是服务器，接收到客户端的数据" + recvStr);
           
            //将接收到的数据经过处理再发送出去
            sendStr = "From Server: " + recvStr;
            SocketSend(sendStr);
        }
    }

    //连接关闭
    void SocketQuit()
    {
        //关闭线程
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        //最后关闭socket
        if (socket != null)
            socket.Close();
        print("disconnect");
    }

    void OnApplicationQuit()
    {
        SocketQuit();
    }
}
