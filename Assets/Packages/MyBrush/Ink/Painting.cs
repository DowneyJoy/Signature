using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable]
public class BrushInfo
{
    public float min = 0.1f;
    public float max = 0.3f;
    public float reduice = 0.006f;
}

public class Painting : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool isInitEnable = true;//是否默认初始化
    public ImageBlendUtility imageBlendUtility;
    public Texture brushTypeTexture;//画笔纹理，半透明

    public BrushInfo brushInfo;//笔触信息
    public Color brushColor = Color.black;//笔触颜色

    [Header("拖动粘连密度")]
    public int num_drag_density = 100;//纹理密度
    [Header("点击粘连密度")]
    public int num_down_density = 5;//点击密度
    public OCRSample ocrSample;

    void OnEnable()
    {
        #region 是否默认初始化
        if (isInitEnable)
        {
            BrushInit();// 笔触初始化
        } 
        #endregion
    }

    #region 笔触初始化
    /// <summary>
    /// 笔触初始化
    /// </summary>
    public void BrushInit()
    {
        //brushColor = Color.white;//默认笔触颜色
        brushInfo = new BrushInfo();        
    } 
    #endregion

    #region 设置画笔宽度
    /// <summary>
    /// 设置画笔宽度
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    float SetScale(float distance)
    {
        float Scale = 0;
        if (distance < 100)
        {
            Scale = brushInfo.max - brushInfo.reduice * distance;
        }
        else
        {
            Scale = brushInfo.min - brushInfo.reduice * distance;
        }
        if (Scale <= 0.05f)
        {
            Scale = 0.05f;
        }
        return Scale;
    }
    #endregion

    #region 清除纹理
    /// <summary>
    /// 清除纹理
    /// </summary>
    /// <param name="destTexture"></param>
    void Clear(RenderTexture destTexture)
    {
        Graphics.SetRenderTarget(destTexture);
        GL.PushMatrix();
        GL.Clear(true, true, new Color(1, 1, 1, 0));
        GL.PopMatrix();
    }
    #endregion

    private float brushScale = 0.5f;//笔刷大小
    private float lastDistance;
    Vector3 startPosition = Vector3.zero;
    Vector3 endPosition = Vector3.zero;
    #region 抬起鼠标 坐标清零
    /// <summary>
    /// 抬起鼠标 坐标清零
    /// </summary>
    void OnMouseUp()
    {
        startPosition = Vector3.zero;
        brushScale = 0.5f;
        a = 0;
        b = 0;
        s = 0;
    }
    #endregion
    #region 移动鼠标  画线
    /// <summary>
    /// 移动鼠标  画线
    /// </summary>
    /// <param name="pos"></param>
    void OnMouseMove(Vector3 pos)
    {
        if (startPosition == Vector3.zero)
        {
            startPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        }
        endPosition = pos;

        float distance = Vector3.Distance(startPosition, endPosition);
        brushScale = SetScale(distance);
        ThreeOrderBezierCurse(pos, distance, 4.5f);

        startPosition = endPosition;
        lastDistance = distance;
    }
    #endregion

    private Vector3[] PositionArray = new Vector3[3];//二阶
    private Vector3[] PositionArray1 = new Vector3[4];//三阶
    private float[] speedArray = new float[4];
    private int a = 0, b = 0, s = 0;
    #region 二阶贝塞尔曲线
    /// <summary>
    /// 二阶贝塞尔曲线
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="distance"></param>
    public void TwoOrderBezierCurse(Vector3 pos, float distance)
    {
        PositionArray[a] = pos;
        a++;
        if (a == 3)
        {
            for (int index = 0; index < num_drag_density; index++)
            {
                Vector3 middle = (PositionArray[0] + PositionArray[2]) / 2;
                PositionArray[1] = (PositionArray[1] - middle) / 2 + middle;

                float t = (1.0f / num_drag_density) * index / 2;
                Vector3 target = Mathf.Pow(1 - t, 2) * PositionArray[0] + 2 * (1 - t) * t * PositionArray[1] +
                                 Mathf.Pow(t, 2) * PositionArray[2];
                float deltaSpeed = (float)(distance - lastDistance) / num_drag_density;
                imageBlendUtility.DoImageBlend(new Vector2((int)target.x, (int)target.y), brushColor, brushTypeTexture, SetScale(lastDistance + (deltaSpeed * index)));
            }
            PositionArray[0] = PositionArray[1];
            PositionArray[1] = PositionArray[2];
            a = 2;
        }
        else
        {
            imageBlendUtility.DoImageBlend(new Vector2((int)endPosition.x, (int)endPosition.y), brushColor, brushTypeTexture, brushScale);
        }
    }
    #endregion
    #region 三阶贝塞尔曲线，获取连续4个点坐标，通过调整中间2点坐标，画出部分（我使用了num/1.5实现画出部分曲线）来使曲线平滑;通过速度控制曲线宽度。
    /// <summary>
    /// 三阶贝塞尔曲线，获取连续4个点坐标，通过调整中间2点坐标，画出部分（我使用了num/1.5实现画出部分曲线）来使曲线平滑;通过速度控制曲线宽度。
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="distance"></param>
    /// <param name="targetPosOffset"></param>
    private void ThreeOrderBezierCurse(Vector3 pos, float distance, float targetPosOffset)
    {
        //记录坐标
        PositionArray1[b] = pos;
        b++;
        //记录速度
        speedArray[s] = distance;
        s++;
        if (b == 4)
        {
            Vector3 temp1 = PositionArray1[1];
            Vector3 temp2 = PositionArray1[2];

            //修改中间两点坐标
            Vector3 middle = (PositionArray1[0] + PositionArray1[2]) / 2;
            PositionArray1[1] = (PositionArray1[1] - middle) * 1.5f + middle;
            middle = (temp1 + PositionArray1[3]) / 2;
            PositionArray1[2] = (PositionArray1[2] - middle) * 2.1f + middle;

            for (int index1 = 0; index1 < num_drag_density / 1.5f; index1++)
            {
                float t1 = (1.0f / num_drag_density) * index1;
                Vector3 target = Mathf.Pow(1 - t1, 3) * PositionArray1[0] +
                                 3 * PositionArray1[1] * t1 * Mathf.Pow(1 - t1, 2) +
                                 3 * PositionArray1[2] * t1 * t1 * (1 - t1) + PositionArray1[3] * Mathf.Pow(t1, 3);
                //float deltaspeed = (float)(distance - lastDistance) / num;
                //获取速度差值（存在问题，参考）
                float deltaspeed = (float)(speedArray[3] - speedArray[0]) / num_drag_density;
                //float randomOffset = Random.Range(-1/(speedArray[0] + (deltaspeed * index1)), 1 / (speedArray[0] + (deltaspeed * index1)));
                //模拟毛刺效果
                float randomOffset = Random.Range(-targetPosOffset, targetPosOffset);
                imageBlendUtility.DoImageBlend(new Vector2((int)(target.x + randomOffset), (int)(target.y + randomOffset)), brushColor, brushTypeTexture, SetScale(speedArray[0] + (deltaspeed * index1)));
            }

            PositionArray1[0] = temp1;
            PositionArray1[1] = temp2;
            PositionArray1[2] = PositionArray1[3];

            speedArray[0] = speedArray[1];
            speedArray[1] = speedArray[2];
            speedArray[2] = speedArray[3];
            b = 3;
            s = 3;
        }
        else
        {
            imageBlendUtility.DoImageBlend(new Vector2((int)endPosition.x, (int)endPosition.y), brushColor, brushTypeTexture, brushScale);
        }
    }
    #endregion

    public void OnPointerDown(PointerEventData eventData)
    {
        for (int i = 0; i < num_down_density; i++)
        {
            OnMouseMove(eventData.position);
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if(ocrSample!=null)
            ocrSample.RunOCR();
        OnMouseUp();
    }
    public void OnDrag(PointerEventData eventData)
    {
        OnMouseMove(eventData.position);
    }
}