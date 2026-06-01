using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class ColorManager : MonoBehaviour
{
    private ColorRGB colorRGB;
    private ColorPanel colorPlane;
    private ColorCircle colorCircle;

    public Slider sliderCRGB;//RGB滑动条
    public Image colorShow;//当前颜色

    public Button btn_OK;//确认
    public Button btn_Cancel;//取消

    public static ColorManager _instance;
    void Awake()
    {
        _instance = this;
    }
    void Start()
    {
        colorRGB = GetComponentInChildren<ColorRGB>();
        colorPlane = GetComponentInChildren<ColorPanel>();
        colorCircle = GetComponentInChildren<ColorCircle>();

        sliderCRGB.onValueChanged.AddListener(OnCRGBValueChanged);
        colorCircle.GetCirclePos += SetColorShowByCirclePos;

        btn_OK.onClick.AddListener(CloseColorPanel);
        btn_Cancel.onClick.AddListener(CloseColorPanel);
    }

    #region 设置显示颜色  根据色环位置
    /// <summary>
    /// 设置显示颜色  根据色环位置
    /// </summary>
    /// <param name="pos"></param>
    private void SetColorShowByCirclePos(Vector2 pos)
    {
        Color getColor = colorPlane.GetColorByPosition(pos);
        colorShow.color = getColor;
    } 
    #endregion

    #region 改变RGB滑动条值
    /// <summary>
    /// 改变RGB滑动条值
    /// </summary>
    /// <param name="value"></param>
    void OnCRGBValueChanged(float value)
    {
        Color endColor = colorRGB.GetColorBySliderValue(value);
        colorPlane.SetColorPanel(endColor);
        colorCircle.SetCirclePos();
    }
    #endregion

    #region 关闭颜色面板
    /// <summary>
    /// 关闭颜色面板
    /// </summary>
    public void CloseColorPanel()
    {
        gameObject.transform.parent.gameObject.SetActive(false);
    }
    #endregion
}
