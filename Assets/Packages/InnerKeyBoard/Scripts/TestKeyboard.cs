using UnityEngine;
using UnityEngine.UI;
using InnerKeyboard;

public class TestKeyboard : MonoBehaviour
{

    private Text EditText;
    private Text TipText;
    private Button TextBtn;
    public Button MessageBtn;

    private KeyboardParam KeyboardPara = new KeyboardParam("");  //键盘参数

    private Text NowEditText;
    private void Awake()
    {
        EditText = transform.Find("Image/MsgInfo").GetComponent<Text>();
        TipText =  transform.Find("Image/MsgTip").GetComponent<Text>();
        TextBtn = EditText.GetComponent<Button>();
        TipText.gameObject.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        TextBtn.onClick.AddListener(() => ClickText(EditText));
        MessageBtn.onClick.AddListener(() => ClickText(EditText));
    }
    void ClickText(Text sender)
    {
        TipText.gameObject.SetActive(false);
        NowEditText = sender;
        KeyboardPara.InputStr = sender.text;
        if (Keyboard.Instance)
            Keyboard.Instance.ShowKeyboard(KeyboardPara, EditCallBack);
    }

    void EditCallBack(KeyboardParam kbpara)
    {
        NowEditText.text = kbpara.OutputStr;
    }
}