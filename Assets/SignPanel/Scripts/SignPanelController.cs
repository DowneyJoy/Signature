using UnityEngine;
using UnityEngine.UI;

namespace TuanjieAI.UI
{
    /// <summary>
    /// 签名面板控制器：绑定保存/清除按钮，显示保存路径。
    /// </summary>
    public class SignPanelController : MonoBehaviour
    {
        public SignPad signPad;
        public Button saveButton;
        public Button clearButton;
        public Button eraseButton;
        public Text statusText;

        private static readonly Color EraseActiveColor = new Color(1f, 0.55f, 0.15f);
        private Color _eraseButtonBaseColor = Color.white;
        public Sprite PenSprite;
        public Sprite EraseSprite;
        public Image EraseImage;
        public Text eraseText;
        private void Start()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);
            if (eraseButton != null)
            {
                _eraseButtonBaseColor = eraseButton.image.color;
                EraseImage.sprite = PenSprite;
                eraseText.text = "画笔模式";
                eraseButton.onClick.AddListener(OnEraseClicked);
            }
            if (statusText != null)
                statusText.text = string.Empty;
        }

        private void OnSaveClicked()
        {
            if (signPad == null)
            {
                SetStatus("签名板未设置", Color.red);
                return;
            }

            string path = signPad.SaveAsPNG();
            if (path != null)
            {
                SetStatus($"已保存: {path}", new Color(0.2f, 0.8f, 0.3f));
            }
            else
            {
                SetStatus("保存失败：画布为空", Color.red);
            }
        }

        private void OnClearClicked()
        {
            if (signPad != null)
            {
                signPad.Clear();
                SetStatus("已清空", new Color(0.5f, 0.5f, 0.5f));
            }
        }
        private void OnEraseClicked()
        {
            if (signPad == null)
            {
                SetStatus("签名板未设置", Color.red);
                return;
            }

            bool eraserOn = signPad.ToggleEraseMode();
            if (eraseButton != null)
            {
                //eraseButton.image.color = eraserOn ? EraseActiveColor : _eraseButtonBaseColor;
                EraseImage.sprite = eraserOn ? EraseSprite : PenSprite;
                eraseText.text = eraserOn ? "橡皮擦模式":"画笔模式";
            }
                

            SetStatus(
                eraserOn ? "橡皮擦模式：涂抹即可擦除，再次点击切回画笔" : "已切回画笔模式",
                eraserOn ? EraseActiveColor : new Color(0.5f, 0.5f, 0.5f));
        }

        private void SetStatus(string msg, Color color)
        {
            if (statusText != null)
            {
                statusText.text = msg;
                statusText.color = color;
            }
        }
    }
}
