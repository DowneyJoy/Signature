using System;
using System.Collections;
using System.IO;
using InnerKeyboard;
using UnityEngine;
using UnityEngine.UI;

public class OCRSample : MonoBehaviour
{
    public PPOCRv5 ocr;
    [Header("测试图")]
    public Texture2D testImage;
    public Button ocrImageBtn;
    public Text ocrDrawText;
    public Text ocrImageText;
    public RawImage Drawable;

    void Start()
    {
        ocrImageBtn.onClick.AddListener(OnOcrImageBtnClick);
        //StartCoroutine(RunOCRCoroutine());
    }

    IEnumerator RunOCRCoroutine()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            var marker = DateTime.Now;
            var texture = RawImageUtility.GetTexture2D(Drawable);
            string result = ocr.RunOCR(texture);
            var time = (DateTime.Now - marker).TotalMilliseconds;
            ocrDrawText.text = "手绘图识别：<color=yellow>" + result + "</color> 耗时：" + (int)time + "ms";
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void RunOCR()
    {
        var marker = DateTime.Now;
        var texture = RawImageUtility.GetTexture2D(Drawable);
        string result = ocr.RunOCR(texture);
        Keyboard.Instance.GetHandWriteResult(result);
        var time = (DateTime.Now - marker).TotalMilliseconds;
        ocrDrawText.text = "手绘图识别：<color=yellow>" + result + "</color> 耗时：" + (int)time + "ms";
    }
    void OnOcrImageBtnClick()
    {
        if (testImage == null) return;
        var marker = DateTime.Now;
        string result = ocr.RunOCR(testImage);
        if (!string.IsNullOrEmpty(result) && result.Length > 0)
        {
            var time = (DateTime.Now - marker).TotalMilliseconds;
            ocrImageText.text = "测试图识别：<color=yellow>" + result + "</color> 耗时：" + (int)time + "ms";
        }
    }
}
