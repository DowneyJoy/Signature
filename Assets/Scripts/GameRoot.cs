using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TuanjieAI.UI;
using UnityEngine;
using UnityEngine.Rendering;
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
    public Button BackMainBtn;
    public Text SendMsg;
    public Text MsgInfo;
    public BrushManager brushManager;
    public SignPad signPad;
    public SignPad signPad2;
    public RawImageSaver rawImageSaver;
    public TcpSender sender;
    public bool isSignOver = false;
    public SensitiveWordFilter  sensitiveWordFilter;
    public Texture2D texture;
    public CustomSystemInfo systemInfo;
    public string defaultStr = "不忘初心，牢记使命！";
    public Text TipTextMsg;
    public Text MsgText;
    public Text MultiMsgText;
    public Text MsgUpText;
    public Text MsgDownText;
    public Button SkipBtn;
    private void Awake()
    {
        Instance = this;
        Loom.RunAsync(() => { });
        sensitiveWordFilter = new SensitiveWordFilter();
        systemInfo = ConfigManger.LoadInfo("CustomSystemInfo.json");
        StartPanel.SetActive(true);
        MainPanel.SetActive(false);
        SignPanel.SetActive(false);
        defaultStr = ConfigManger.LoadInfoString("content.txt");
        defaultStr = sensitiveWordFilter.Filter(defaultStr);
        TipTextMsg.text = defaultStr;
        MultiMsgText.text = "留言信息：";
        StartBtn.onClick.AddListener(() =>
        {
            
            isSignOver = false;
            StartPanel.SetActive(false);
            MainPanel.SetActive(true);
            MsgInfo.text = "";
            TextTipGo.SetActive(true);
            MsgDownText.text = "";
            MsgUpText.text = "";
            UpdateMsg();
            
            //隔断
            brushManager.OnClickClear();
            //SignPanel.SetActive(true);
            signPad.Clear();
            signPad2.Clear();
        });
        SignBtn.onClick.AddListener(() =>
        {
            string message1 = MsgInfo.text.Replace("留言信息：", "");
            string message2 = "铭记历史，坚守初心!";
            if (!string.IsNullOrWhiteSpace(message1))
            {
                message2 = sensitiveWordFilter.Filter(message1);
            }
            SendMsg.text = "留言信息："+message2;
            brushManager.OnClickClear();
            MainPanel.SetActive(false);
                
            //隔断
            signPad.Clear();
            signPad2.Clear();
            SignPanel.SetActive(true);
        });
        SkipBtn.onClick.AddListener(() =>
        {
            SendMsg.text = "留言信息：铭记历史，坚守初心!";
            brushManager.OnClickClear();
            MainPanel.SetActive(false);
                
            //隔断
            signPad.Clear();
            signPad2.Clear();
            SignPanel.SetActive(true);
        });
        ClearBtn.onClick.AddListener(() =>
        {
            brushManager.OnClickClear();
            
            //隔断
            signPad.Clear();
            signPad2.Clear();
        });
        UploadBtn.onClick.AddListener(() =>
        {
            UploadInfo();
        });
        BackBtn.onClick.AddListener(() =>
        {
            SignPanel.SetActive(false);
            //StartPanel.SetActive(true);
            MainPanel.SetActive(true);
        });
        BackMainBtn.onClick.AddListener(() =>
        {
            SignPanel.SetActive(false);
            MainPanel.SetActive(false);
            StartPanel.SetActive(true);
        });
        ChineseFilter.LoadChinese();
    }

    public void UploadInfo()
    {
        if (isSignOver)
        {
            string message = SendMsg.text.Replace("留言信息：", "");
            if (string.IsNullOrWhiteSpace(message))
                message = "铭记历史，坚守初心!";
            sender.SendTextAndImage(message,rawImageSaver.GetTextureFromRawImage());
            SignPanel.SetActive(false);
            StartPanel.SetActive(true);
        }
        else
        {
            TipGo.SetActive(true);
            TipText.text = "请进行签名";
        }
    }

    public void UpdateMsg()
    {
        MultiMsgText.text = "留言信息："+ MsgUpText.text + MsgDownText.text;
    }
    public void InitMsg()
    {
        MultiMsgText.text = "留言信息：";
        MsgUpText.text = "";
        MsgDownText.text = "";
    }
    public void SendTextTest()
    {
        sender.SendText("测试");
    }

    public void SendTextTest2()
    {
        sender.SendTexture(texture);
    }

    public void AddDefault()
    {
        MsgText.text += defaultStr;
    }
}
