using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using hyjiacan.py4n;

namespace InnerKeyboard
{

    //键盘参数类
    public class KeyboardParam
    {
        public string InputStr;
        public string OutputStr;

        public KeyboardParam(string InStr, string OutStr = "")
        {
            InputStr = InStr;
            OutputStr = OutStr;
        }
    }

    //委托事件类
    public class EventCommon
    {

        public delegate void CallBack<T>(T para);

        public delegate void NorEvent();
    }

    public class Keyboard : MonoBehaviour
    {
        private RectTransform KeyboardWindow;
        private GameObject ComBtnPref;
        private Transform Line0, Line1, Line2, Line3, LineCN;
        private Button BackSpaceBtn, ShiftBtn, SpaceBtn, CancelBtn, EnterBtn, LangugeBtn, ClearBtn;
        private Image ShiftBG;

        private string[][] Line0_KeyValue = {
            new string[]{"`","1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "="},
            new string[]{"·", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+" }};

        private string[][] Line1_KeyValue = {
            new string[]{"q","w", "e", "r", "t", "y", "u", "i", "o", "p", "[", "]", "\\"},
            new string[]{"Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "{", "}", "|" }};

        private string[][] Line2_KeyValue = {
            new string[]{"a","s", "d", "f", "g", "h", "j", "k", "l", ";", "'"},
            new string[]{"A", "S", "D", "F", "G", "H", "J", "K", "L", ":", "\""}};

        private string[][] Line3_KeyValue = {
            new string[]{"z","x", "c", "v", "b", "n", "m", ",", ".", "/"},
            new string[]{"Z", "X", "C", "V", "B", "N", "M", "<", ">", "?"}};


        private string NewEditeString = "";

        [HideInInspector]
        public bool isShift = false;
        private bool isShiftLock = false;
        private float ShiftTime = 0f;
        private Color32 LockColor = new Color32(127, 171, 179, 255);


        private event EventCommon.NorEvent OnShiftOn = null;
        private event EventCommon.NorEvent OnShiftOff = null;

        private KeyboardParam KeyboardPara = null;//键盘参数

        
        private EventCommon.CallBack<KeyboardParam> call = null; //回调函数

        private static Keyboard instance = null;

        public static Keyboard Instance
        {
            get { return instance; }
        }
        [Header("中文输入")] private bool isCn = true;
        public GameObject CnObj;

        private Text languageText;
        public int nowPage, page, surplus;
        private string PinYinStr;
        public Text PinYinText;
        private string[] HanZiArr;

        public List<Text> HanziText;
        public BrushManager brushManager;
        //Awake
        private void Awake()
        {
            KeyboardWindow = this.transform.GetComponent<RectTransform>();
            ComBtnPref = Resources.Load<GameObject>("KeyItem");
            Line0 = KeyboardWindow.Find("KB_BG/KeyBtns/ComBtnLine0");
            Line1 = KeyboardWindow.Find("KB_BG/KeyBtns/ComBtnLine1");
            Line2 = KeyboardWindow.Find("KB_BG/KeyBtns/ComBtnLine2");
            Line3 = KeyboardWindow.Find("KB_BG/KeyBtns/ComBtnLine3");
            LineCN = KeyboardWindow.Find("KB_BG/ComBtnLineCN");

            BackSpaceBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine3/BackSpaceBtn").GetComponent<Button>();
            ShiftBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine3/ShiftBtn").GetComponent<Button>();
            SpaceBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine4/SpaceBtn").GetComponent<Button>();
            CancelBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine4/right/CancelBtn").GetComponent<Button>();
            EnterBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine4/right/EnterBtn").GetComponent<Button>();
            LangugeBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine4/left/LangugeBtn").GetComponent<Button>();
            ClearBtn = KeyboardWindow.Find("KB_BG/KeyBtns/BtnLine4/left/ClearBtn").GetComponent<Button>();

            ShiftBG = ShiftBtn.GetComponent<Image>();
            languageText = LangugeBtn.GetComponentInChildren<Text>();
            languageText.text = "中/<color=#9A9A9A>En</color>";
            CnObj.SetActive(isCn);
        }

        void Start()
        {
            KeyboardWindow.localScale = Vector3.zero;
            InitComBtn();

            BackSpaceBtn.onClick.AddListener(ClickBackSpace);
            ShiftBtn.onClick.AddListener(ClickShift);
            SpaceBtn.onClick.AddListener(ClickSpace);
            CancelBtn.onClick.AddListener(ClickCancel);
            EnterBtn.onClick.AddListener(ClickEnter);
            LangugeBtn.onClick.AddListener(ClickLanguage);
            ClearBtn.onClick.AddListener(ClickClear);

            instance = this;
        }


        //初始化按钮
        private void InitComBtn()
        {
            InstantLineComBtns(Line0, Line0_KeyValue);
            InstantLineComBtns(Line1, Line1_KeyValue);
            InstantLineComBtns(Line2, Line2_KeyValue);
            InstantLineComBtns(Line3, Line3_KeyValue);
        }

        //按行实例化按钮
        private void InstantLineComBtns(Transform LineTran,string[][] KeyValues)
        {
            for (int i = 0; i < KeyValues[0].Length; i++)
            {
                GameObject TempObj = GameObject.Instantiate<GameObject>(ComBtnPref);
                TempObj.transform.SetParent(LineTran);
                TempObj.transform.localScale = Vector3.one;
                ComBtn comBtnCtrl = TempObj.GetComponent<ComBtn>();
                if (comBtnCtrl != null)
                {
                    comBtnCtrl.SetKeyValue(KeyValues[0][i], KeyValues[1][i]);
                    OnShiftOn += comBtnCtrl.OnShiftOn;
                    OnShiftOff += comBtnCtrl.OnShiftOff;
                } 
            }
        }

        //输入内容追加
        public void AddComBtnString(string str)
        {
            if (!isCn)
            {
                NewEditeString += str;
                if (KeyboardPara != null)
                    KeyboardPara.OutputStr = NewEditeString;
                call?.Invoke(KeyboardPara);
                if (isShift && !isShiftLock)
                {
                    isShift = false;
                    ShiftBG.color = new Color(128, 128, 128, 255);
                    OnShiftOff();
                }
            }
            else
            {
                Regex reg = new Regex(@"[a-zA-Z]+");
                if (reg.IsMatch(str))
                { //纯字母
                    PinYinStr += str;
                    PinYinText.text = PinYinStr;
                    //HanZiArr = Pinyin4Net .GetHanzi(PinYinStr, true);
                    HanZiArr = ChineseFilter.GetSortedHanziWithCache(PinYinStr);
                    UpdateHanZi();
                }
                else {
                    NewEditeString += str;
                    if (KeyboardPara != null)
                        KeyboardPara.OutputStr = NewEditeString;
                    call?.Invoke(KeyboardPara);
                }
            }
        }

  
        // 唤起键盘
        public void ShowKeyboard(KeyboardParam para, EventCommon.CallBack<KeyboardParam> call)
        {
            KeyboardPara = para;
            NewEditeString = KeyboardPara.InputStr;

            KeyboardWindow.localScale = Vector3.one;
            if (call != null)
                this.call = call;
        }

        //语言点击事件
        public void ClickLanguage()
        {
            isCn = !isCn;
            if (isCn)
            {
                languageText.text = "中/<color=#9A9A9A>En</color>";
                CnObj.SetActive(true);
                ClearCnPinYin();
            }
            else
            {
                languageText.text = "En/<color=#9A9A9A>中</color>";
                CnObj.SetActive(false);
            }
        }

        //取消点击事件
        public void ClickCancel()
        {
            ClearCnPinYin();
            NewEditeString = ""; 
            if (KeyboardPara != null)
                KeyboardPara.OutputStr = KeyboardPara.InputStr;
            call?.Invoke(KeyboardPara);
            KeyboardPara = null;
            KeyboardWindow.localScale = Vector3.zero;
        }

        //确认点击事件
        public void ClickEnter()
        {
            ClearCnPinYin();
            if (KeyboardPara != null)
                KeyboardPara.OutputStr = NewEditeString;
            KeyboardWindow.localScale = Vector3.zero;
            call?.Invoke(KeyboardPara);
            KeyboardPara = null;
        }
        //点击汉字
        public void ClickHanZi(Text hz) {
            NewEditeString += hz.text; 
            NewEditeString = GameRoot.Instance.sensitiveWordFilter.Filter(NewEditeString);
            if (KeyboardPara != null)
                KeyboardPara.OutputStr = NewEditeString;
            call?.Invoke(KeyboardPara);
            ClearCnPinYin();
        }

        public void ClearCnPinYin()
        {
            PinYinStr = "";
            PinYinText.text = PinYinStr;
            Clear();
            HanZiArr= null;
            brushManager.OnClickClear();
        }
        //点击上一页
        public void ClickLast()
        {
            if (nowPage == 0)
                nowPage = 0;
            else
                nowPage--;
            Display(HanZiArr);
        }

        //点击下一页
        public void ClickNext() {
            if (nowPage == page)
                nowPage = page;
            else
                nowPage++;
            Display(HanZiArr);
        }

        public void GetHandWriteResult(string result)
        {
            string[] handwrite = new []{result};
            HanZiArr = handwrite;
            UpdateHanZi();
        }
        /// <summary>
        /// 更新汉字
        /// </summary>
        /// <param name="page"></param>
        private void UpdateHanZi()
        {
            if (HanZiArr.Length > 0)
            {
                page = HanZiArr.Length / 10;
                surplus = HanZiArr.Length % 10;
                nowPage = 0;
                //Debug.Log($"HanZiArr:{HanZiArr.Length}, Page:{page}, NowPage:{nowPage},  surplus:{surplus}");
                Display(HanZiArr);
            }
            else
            {
                Clear();
            }
        }
        /// <summary>
        /// 更新显示
        /// </summary>
        /// <param name="str"></param>
        void Display(string[] str)
        {
            Clear();
            if(HanZiArr == null)
                return;
            if (page == 0 && surplus != 0) {
                for (int i = 0; i < surplus; i++) {
                    HanziText[i].text = str[i];
                }
		
            } else if (page > 0) {
                if (nowPage == page) {
                    for (int i = 0; i < surplus; i++) {
                        HanziText[i].text = str [page * 10 + i];
                    }
                } else {
                    for (int i = 0; i < 10; i++) {
                        HanziText[i].text = str [nowPage * 10 + i];
                    }
                }
            }
        }
        void Clear()
        {
            for (int i = 0; i < HanziText.Count; i++)
            {
                HanziText[i].text = "";
            }
        }

        //shift点击事件
        public void ClickShift()
        {
            if (Time.time - ShiftTime <= 0.5f)
            {
                ShiftTime = Time.time;
                isShift = true;
                isShiftLock = true;
                ShiftBG.color = LockColor;
                OnShiftOn();
            }
            else
            {
                if (isShift)
                {
                    ShiftTime = Time.time;
                    isShift = false;
                    isShiftLock = false;
                    ShiftBG.color = new Color(128, 128, 128, 255);
                    OnShiftOff();
                }
                else
                {
                    ShiftTime = Time.time;
                    isShift = true;
                    isShiftLock = false;
                    OnShiftOn();
                }
            }
        }

        //空格点击事件
        public void ClickSpace()
        {
            NewEditeString += " ";
            if (KeyboardPara != null)
                KeyboardPara.OutputStr = NewEditeString;
            call?.Invoke(KeyboardPara);
        }

        //清除点击事件
        public void ClickClear()
        {
            NewEditeString = ""; 
            if (KeyboardPara != null)
                KeyboardPara.OutputStr = NewEditeString;
            call?.Invoke(KeyboardPara);
        }

        //回退点击事件
        public void ClickBackSpace()
        {
            if (!isCn)
            {
                if (!string.IsNullOrEmpty(NewEditeString))
                {
                    NewEditeString = NewEditeString.Substring(0, NewEditeString.Length - 1);
                    if (KeyboardPara != null)
                        KeyboardPara.OutputStr = NewEditeString;
                    call?.Invoke(KeyboardPara);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(PinYinStr))
                {
                    PinYinStr = PinYinStr.Substring(0, PinYinStr.Length - 1);
                    PinYinText.text = PinYinStr;
                    HanZiArr = Pinyin4Net.GetHanzi(PinYinStr, false);
                    UpdateHanZi();
                }
                else
                {
                    if (!string.IsNullOrEmpty(NewEditeString))
                    {
                        NewEditeString = NewEditeString.Substring(0, NewEditeString.Length - 1);
                        if (KeyboardPara != null)
                            KeyboardPara.OutputStr = NewEditeString;
                        call?.Invoke(KeyboardPara);
                    }
                }
            }

        }
    }
}
