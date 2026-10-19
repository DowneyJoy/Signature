using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//******* 此脚本和 UDPSevPhoto在一起作为服务器端,测试接收到的图片，并把图片在视图显示*******

public class LoadImageFromClient : MonoBehaviour
{
    public RawImage  newImage;
    public Transform showPanel;

    void Start()
    {
        UDPSevPhoto.instance.UDPserverEvent += ReceiveByteFromUDPServer;
    }

    /// <summary>
    /// 发来的字节包括：图片的字节长度（前四个字节）和图片字节
    /// 得到发来的字节中图片字节长度和图片字节数组
    /// </summary>
    void ReceiveByteFromUDPServer(Texture2D newTexture)
    {
        newImage.gameObject.SetActive(true);
        newImage.texture = newTexture;
        Invoke("SetDefultImage", 20f);//图片显示20秒后消失
    }

    void SetDefultImage()
    {
        newImage.texture = null;
    }

    void OnDisable()
    {
        UDPSevPhoto.instance.UDPserverEvent -= ReceiveByteFromUDPServer;
    }
}
