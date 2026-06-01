using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance;
    public GameObject StartPanel;
    public GameObject MainPanel;
    public GameObject SignPanel;
    public GameObject TipGo;
    public Text TipText;
    public GameObject TextTipGo;
    
    public Button StartBtn;
    public Button SignBtn;
    
    public Button ClearBtn;
    public Button UploadBtn;
    public Button BackBtn;
    public Text SendMsg;
    public Text MsgInfo;
    public BrushManager brushManager;
    public RawImageSaver rawImageSaver;
    public TcpSender sender;
    public bool isSignOver = false;
    public SensitiveWordFilter  sensitiveWordFilter;
    public Texture2D texture;
    private void Awake()
    {
        Instance = this;
        Loom.RunAsync(() => { });
        sensitiveWordFilter = new SensitiveWordFilter();
        StartPanel.SetActive(true);
        MainPanel.SetActive(false);
        SignPanel.SetActive(false);
        StartBtn.onClick.AddListener(() =>
        {
            StartPanel.SetActive(false);
            MainPanel.SetActive(true);
            MsgInfo.text = "";
            TextTipGo.SetActive(true);
            isSignOver = false;
        });
        SignBtn.onClick.AddListener(() =>
        {
            if (!string.IsNullOrWhiteSpace(MsgInfo.text))
            {
                SendMsg.text = "留言信息：" + sensitiveWordFilter.Filter(MsgInfo.text);
                brushManager.OnClickClear();
                MainPanel.SetActive(false);
                SignPanel.SetActive(true);
            }
            else
            {
                TipGo.SetActive(true);
                TipText.text = "请输入留言";
            }
        });
        ClearBtn.onClick.AddListener(() =>
        {
            brushManager.OnClickClear();
        });
        UploadBtn.onClick.AddListener(() =>
        {
            if (isSignOver)
            {
                //sender.SendMessage(SendMsg.text.Replace("留言信息：",""));
                //sender.SendTexture(rawImageSaver.GetTextureFromRawImage());
                sender.SendTextAndImage(SendMsg.text.Replace("留言信息：",""),rawImageSaver.GetTextureFromRawImage());
                SignPanel.SetActive(false);
                StartPanel.SetActive(true);
            }
            else
            {
                TipGo.SetActive(true);
                TipText.text = "请进行签名";
            }
        });
        BackBtn.onClick.AddListener(() =>
        {
            SignPanel.SetActive(false);
            MainPanel.SetActive(true);
        });
        ChineseFilter.LoadChinese();
    }

    public void SendTextTest()
    {
        sender.SendText("测试");
    }

    public void SendTextTest2()
    {
        sender.SendTexture(texture);
    }
}
