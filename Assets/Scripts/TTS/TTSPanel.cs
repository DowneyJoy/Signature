using UnityEngine;
using UnityEngine.UI;

namespace TuanjieAI.TTS
{
    /// <summary>
    /// 文字转语音面板 UI 控制器：管理文本输入、参数滑块、播放控制。
    /// </summary>
    public class TTSPanel : MonoBehaviour
    {
        [Header("UI 控件引用")]
        public InputField textInput;
        public Slider rateSlider;
        public Slider volumeSlider;
        public Button speakButton;
        public Button stopButton;
        public Button clearButton;
        public Text statusText;
        public Text rateValueText;
        public Text volumeValueText;

        [Header("TTS 管理器")]
        public TextToSpeechManager ttsManager;

        private void Start()
        {
            BindUI();
            InitializeValues();

            if (ttsManager != null)
            {
                ttsManager.OnStatusChanged += OnStatusChanged;
            }
        }

        private void BindUI()
        {
            speakButton.onClick.AddListener(OnSpeakClicked);
            stopButton.onClick.AddListener(OnStopClicked);
            clearButton.onClick.AddListener(OnClearClicked);

            rateSlider.onValueChanged.AddListener(v =>
            {
                rateValueText.text = Mathf.RoundToInt(v).ToString();
            });

            volumeSlider.onValueChanged.AddListener(v =>
            {
                volumeValueText.text = Mathf.RoundToInt(v).ToString();
            });

            // 回车提交
            textInput.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnSpeakClicked();
            });
        }

        private void InitializeValues()
        {
            rateSlider.minValue = -10;
            rateSlider.maxValue = 10;
            rateSlider.wholeNumbers = true;
            rateSlider.value = 0;
            rateValueText.text = "0";

            volumeSlider.minValue = 0;
            volumeSlider.maxValue = 100;
            volumeSlider.wholeNumbers = true;
            volumeSlider.value = 100;
            volumeValueText.text = "100";
        }

        private void OnSpeakClicked()
        {
            if (ttsManager == null)
            {
                statusText.text = "TTS 管理器未设置";
                statusText.color = Color.red;
                return;
            }

            string text = textInput.text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                statusText.text = "请输入要朗读的文本";
                statusText.color = Color.red;
                return;
            }

            ttsManager.rate = Mathf.RoundToInt(rateSlider.value);
            ttsManager.volume = Mathf.RoundToInt(volumeSlider.value);
            ttsManager.Speak(text);
        }

        private void OnStopClicked()
        {
            if (ttsManager != null)
                ttsManager.Stop();
        }

        private void OnClearClicked()
        {
            textInput.text = string.Empty;
            statusText.text = string.Empty;
        }

        private void OnStatusChanged(string status)
        {
            statusText.text = status;
            if (status.Contains("失败") || status.Contains("超时") || status.Contains("为空") || status.Contains("未设置"))
                statusText.color = Color.red;
            else if (status.Contains("正在"))
                statusText.color = new Color(0.35f, 0.7f, 1f);
            else if (status.Contains("完毕") || status.Contains("停止"))
                statusText.color = new Color(0.3f, 0.85f, 0.4f);
            else
                statusText.color = Color.white;
        }
    }
}
