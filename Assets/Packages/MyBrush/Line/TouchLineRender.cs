using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum WidthType
{
    FixedWidth,//固定宽度
    WidthCurve//宽度曲线
}
public class TouchLineRender : MonoBehaviour, IDragHandler,IPointerDownHandler,IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    public WidthType widthType = WidthType.WidthCurve;
    public bool isInitEnable = true;//是否默认初始化

    public GameObject LinePrefab;
    public AnimationCurve[] WidthCurves;//线条宽度曲线
    public List<float> line_width_list = new List<float>() { 0.08f, 0.1f, 0.2f };
    public Color color_line = Color.red;//线条颜色
    private int index_select_width = 0;//线条宽度序号
    
    void OnEnable()
    {
        #region 是否默认初始化
        if (isInitEnable)
        {
            LineInit();// 线条初始化
        }
        #endregion
    }
    void Start()
    {

    }

    #region 线条初始化
    /// <summary>
    /// 线条初始化
    /// </summary>
    public void LineInit()
    {
        color_line = Color.red;//默认颜色
        index_select_width = 0;//默认序号
        ClearAll();//清除笔画
    }
    #endregion


    #region 设置笔触颜色
    /// <summary>
    /// 设置笔触颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetColor(Color color)
    {
        color_line = color;
    }
    #endregion

    #region 设置线条大小
    /// <summary>
    /// 设置线条大小
    /// </summary>
    /// <param name="index"></param>
    public void SetLineWidth(int index)
    {
        index_select_width = index;
    }
    #endregion

    #region 清除笔画
    /// <summary>
    /// 清除笔画
    /// </summary>
    public void ClearAll()
    {
        currentLine = null;
        foreach (var lineRenderer in lineRenderers)
        {
            if (lineRenderer.gameObject)
            {
                Destroy(lineRenderer.gameObject);
            }
        }
        lineRenderers.Clear();
    }
    #endregion


    private List<LineRenderer> lineRenderers = new List<LineRenderer>();//线条列表
    private LineRenderer currentLine;
    #region 创建线条  创建CullingMask ：MessageLine
    /// <summary>
    /// 创建线条  创建CullingMask ：MessageLine
    /// </summary>
    private void CreateLine()
    {
        GameObject go = Instantiate(LinePrefab, Camera.main.ScreenToWorldPoint(new Vector3(previousPosition.x, previousPosition.y, 15)), Quaternion.identity);
        go.layer = LayerMask.NameToLayer("MessageLine");
        LineRenderer line = go.GetComponent<LineRenderer>();
        currentLine = line;
        lineRenderers.Add(line);
        line.startColor = color_line;
        line.endColor = color_line;
        line.material.color = color_line;
        #region 设置线条宽度
        if (widthType == WidthType.WidthCurve)
        {
            line.startWidth = line_width_list[index_select_width];
            line.endWidth = line_width_list[index_select_width];
        }
        else if (widthType == WidthType.FixedWidth)
        {
            line.widthCurve = WidthCurves[index_select_width];
        } 
        #endregion
        go.SetActive(false);
    } 
    #endregion

    private bool isInRange;
    public Action OnDraw;
    private Vector2 previousPosition;  
    #region 拖拽画线
    /// <summary>
    /// 拖拽画线
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {
        if (isInRange)
        {
            if (eventData.delta.magnitude > 0.1f &&
                currentLine != null)
            {
                currentLine.gameObject.SetActive(true);
                lineIndex++;
                previousPosition = eventData.position;
                currentLine.positionCount = lineIndex; //设置顶点数
                currentLine.SetPosition(lineIndex - 1,
                    Camera.main.ScreenToWorldPoint(new Vector3(previousPosition.x, previousPosition.y, 15))); //设置顶点位置

                if (OnDraw != null) OnDraw();
            }
        }
    }
    #endregion

    private int lineIndex;
    public void OnPointerDown(PointerEventData eventData)
    {
        previousPosition = eventData.position;
        lineIndex = 0;
        CreateLine();//创建线条      
    }
    public void OnPointerUp(PointerEventData eventData)
    {

    }
    public void OnPointerExit(PointerEventData eventData)
    {
        isInRange = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isInRange = true;
    }
}
