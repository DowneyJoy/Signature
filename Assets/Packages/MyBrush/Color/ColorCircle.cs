using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class ColorCircle : MonoBehaviour
{
    public delegate void RetureTextuePosition(Vector2 pos);
    public event RetureTextuePosition GetCirclePos;//获取色环位置

    public RectTransform CirclePanelRectTransform;//选色面板

    private RectTransform rt;//色环位置
    private float width = 256;
    private float height = 256;

    void Start () {
        rt = GetComponent<RectTransform>();
        width = CirclePanelRectTransform.sizeDelta.x;
        height = CirclePanelRectTransform.sizeDelta.y;
    }

    #region 限制色环位置
    /// <summary>
    /// 限制色环位置
    /// </summary>
    public void OnDrag()
    {
        if (rt.anchoredPosition.x <= 0)
            rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y);
        if (rt.anchoredPosition.y <= 0)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0);
        if (rt.anchoredPosition.x >= width - 1)
            rt.anchoredPosition = new Vector2(width - 1, rt.anchoredPosition.y);
        if (rt.anchoredPosition.y >= height - 1)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, height - 1);
    }
    #endregion

    #region 设置色环位置
    /// <summary>
    /// 设置色环位置
    /// </summary>
    public void SetCirclePos()
    {
        GetCirclePos(rt.anchoredPosition);
    } 
    #endregion

}
