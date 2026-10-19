using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class ColorPanel : MonoBehaviour, IPointerClickHandler, IDragHandler
{    
    public RawImage rawImage;
    public RectTransform circleRect;//色环位置

    Color[,] arrayColor;
    Texture2D tex2d;

    RectTransform rt;//颜色面板位置
    int TexPixelLength = 256;

    void Start()
    {
        rt = rawImage.GetComponent<RectTransform>();
        TexPixelLength = (int)rt.sizeDelta.x;

        //设置颜色区域
        arrayColor = new Color[TexPixelLength, TexPixelLength];
        tex2d = new Texture2D(TexPixelLength, TexPixelLength, TextureFormat.RGB24, true);
        rawImage.texture = tex2d;

        //设置面板颜色
        SetColorPanel(Color.red);
    }
    
    void Update()
    {
        #region 测试  随机色
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Color end = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
            SetColorPanel(end);
        } 
        #endregion
    }

    #region 设置面板颜色
    /// <summary>
    /// 设置面板颜色
    /// </summary>
    /// <param name="endColor"></param>
    public void SetColorPanel(Color endColor)
    {
        Color[] CalcArray = CalcArrayColor(endColor);
        tex2d.SetPixels(CalcArray);
        tex2d.Apply();
    }
    #endregion

    #region 计算颜色数组
    /// <summary>
    /// 计算颜色数组
    /// </summary>
    /// <param name="endColor"></param>
    /// <returns></returns>
    Color[] CalcArrayColor(Color endColor)
    {
        Color value = (endColor - Color.white) / (TexPixelLength - 1);
        for (int i = 0; i < TexPixelLength; i++)
        {
            arrayColor[i, TexPixelLength - 1] = Color.white + value * i;
        }
        for (int i = 0; i < TexPixelLength; i++)
        {
            value = (arrayColor[i, TexPixelLength - 1] - Color.black) / (TexPixelLength - 1);
            for (int j = 0; j < TexPixelLength; j++)
            {
                arrayColor[i, j] = Color.black + value * j;
            }
        }
        List<Color> listColor = new List<Color>();
        for (int i = 0; i < TexPixelLength; i++)
        {
            for (int j = 0; j < TexPixelLength; j++)
            {
                listColor.Add(arrayColor[j, i]);
            }
        }

        return listColor.ToArray();
    }
    #endregion

    #region 获取颜色 根据坐标
    /// <summary>
    /// 获取颜色 根据坐标
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Color GetColorByPosition(Vector2 pos)
    {
        Texture2D tempTex2d = (Texture2D)rawImage.texture;
        Color getColor = tempTex2d.GetPixel((int)pos.x, (int)pos.y);
        return getColor;
    }
    #endregion


    #region 调色板点击事件
    /// <summary>
    /// 调色板点击事件
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        Vector3 wordPos;
        //将UGUI的坐标转为世界坐标  
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera,
            out wordPos))
        {
            circleRect.position = wordPos;
            circleRect.GetComponent<ColorCircle>().OnDrag();// 限制色环位置
        }
        // 设置色环位置
        circleRect.GetComponent<ColorCircle>().SetCirclePos();
    }
    #endregion

    #region 调色板拖拽事件
    /// <summary>
    /// 调色板拖拽事件
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 wordPos;
        //将UGUI的坐标转为世界坐标  
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera,
            out wordPos))
        {
            circleRect.position = wordPos;
            circleRect.GetComponent<ColorCircle>().OnDrag();// 限制色环位置
        }

        // 设置色环位置
        circleRect.GetComponent<ColorCircle>().SetCirclePos();
    } 
    #endregion
}
