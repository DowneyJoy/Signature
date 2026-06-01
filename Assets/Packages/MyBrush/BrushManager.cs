using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PenType
{
    WriteBrush,
    WriteLine
}
public class BrushManager : MonoBehaviour
{
    public PenType penType = PenType.WriteBrush;
    public bool isClearOnDisable = true;//是否关闭时清除

    public Painting painting;//画图
    public ImageBlendUtility imageBlendUtility;//笔刷
    public TouchLineRender lineRender;//画线

    public RenderTexture texRender_Brush;//笔刷画布
    public RenderTexture texRender_Line;//线条画布

    public Button btn_Mode;//笔刷模式
    public Button btn_Clear;//清除 
    public Button btn_Color;//选色
    public GameObject panel_Color;//颜色选择页面

    //笔触按钮：细中粗
    public List<Button> btn_brush_list;
    public List<float> brush_scale_list = new List<float>()
    {0.6f,1f,1.5f };

    public RawImage rawImage_show;//显示画面

    [Header("拍照画面")]
    public RawImage rawImage_photo;//拍照画面

    [Header("是否替换背景图片")]
    public bool isSetBg = false;
    public Image img_bg;//背景
    public List<Button> btn_bg_list;
    public List<Sprite>sprite_bg_list;
    public Image img_bg_show;//合成的背景

    //public static BrushManager _instance;
    void Awake()
    {
        //_instance = this;
    }
    void Start()
    {
        #region 拍照画面
        if (rawImage_photo)
        {
            //rawImage_photo.texture = TakePhotos._instance.tex2D;
        }
        #endregion

        btn_Mode.onClick.AddListener(OnChangeBrusshMode);
        btn_Clear.onClick.AddListener(OnClickClear);
        btn_Color.onClick.AddListener(OnClickColor);

        #region 笔触按钮监听
        for (int i = 0; i < btn_brush_list.Count; i++)
        {
            int m = i;
            btn_brush_list[i].onClick.AddListener(() =>
            {
                ChangeLineScale(m);// 修改笔触大小
            });
        }
        #endregion

        #region 是否替换背景图片
        if (isSetBg)
        {
            #region 背景按钮监听
            for (int i = 0; i < btn_bg_list.Count; i++)
            {
                int m = i;
                btn_bg_list[i].onClick.AddListener(() =>
                {
                    img_bg.sprite = sprite_bg_list[m];
                    img_bg_show.sprite = sprite_bg_list[m];
                });
            }
            #endregion

            img_bg.sprite = sprite_bg_list[0];
            img_bg_show.sprite = sprite_bg_list[0];
        } 
        #endregion
    }
    void OnEnable()
    {
        panel_Color.SetActive(false);
        SetBrusshMode(penType);//设置笔刷模式
    }
    void OnDisable()
    {
        #region 是否关闭时清除
        if (isClearOnDisable)
        {
            OnClickClear();// 清除画布
        } 
        #endregion
    }

    #region 改变笔刷模式
    /// <summary>
    /// 改变笔刷模式
    /// </summary>
    public void OnChangeBrusshMode()
    {
        if (penType == PenType.WriteBrush)
        {
            penType = PenType.WriteLine;
        }
        else if (penType == PenType.WriteLine)
        {
            penType = PenType.WriteBrush;
        }
        OnClickClear();// 清除画布
        SetBrusshMode(penType);//设置笔刷模式
    }
    #endregion

    #region 设置笔刷模式
    /// <summary>
    /// 设置笔刷模式
    /// </summary>
    /// <param name="penType"></param>
    public void SetBrusshMode(PenType penType)
    {
        #region 显示留言纹理
        if (rawImage_show)
        {
            rawImage_show.texture = GetBrushRenderTexture();
        } 
        #endregion

        switch (penType)
        {
            case PenType.WriteBrush:
                imageBlendUtility.gameObject.SetActive(true);
                lineRender.gameObject.SetActive(false);
                break;
            case PenType.WriteLine:
                imageBlendUtility.gameObject.SetActive(false);
                lineRender.gameObject.SetActive(true);
                break;
        }
    }
    #endregion
    #region 获取留言纹理
    /// <summary>
    /// 获取留言纹理
    /// </summary>
    /// <returns></returns>
    public RenderTexture GetBrushRenderTexture()
    {
        RenderTexture rt = null;
        if (penType == PenType.WriteBrush)
        {
            rt = texRender_Brush;
        }
        else if (penType == PenType.WriteLine)
        {
            rt = texRender_Line;
        }
        return rt;
    }
    #endregion

    float value_scale;
    #region 修改笔触大小
    /// <summary>
    /// 修改笔触大小
    /// </summary>
    /// <param name="index"></param>
    public void ChangeLineScale(int index)
    {
        if (penType == PenType.WriteBrush)
        {
            value_scale = brush_scale_list[index];
            painting.brushInfo = new BrushInfo();
            painting.brushInfo.min *= value_scale;
            painting.brushInfo.max *= value_scale;
            painting.brushInfo.reduice *= value_scale;
        }
        else if (penType == PenType.WriteLine)
        {
            lineRender.SetLineWidth(index);
        }

        CloseColorPanel();//关闭颜色面板
    }
    #endregion

    #region 点击选色按钮
    /// <summary>
    /// 点击选色按钮
    /// </summary>
    public void OnClickColor()
    {
        if (panel_Color.activeSelf)
        {
            panel_Color.SetActive(false);
        }
        else
        {
            panel_Color.SetActive(true);
            ColorManager._instance.btn_OK.onClick.AddListener(ChangeBrushColor);
        }
    }
    #endregion

    #region 清除画布
    /// <summary>
    ///清除画布
    /// </summary>
    public void OnClickClear()
    {
        if (penType == PenType.WriteBrush)
        {
            imageBlendUtility.Clear();
        }
        else if (penType == PenType.WriteLine)
        {
            lineRender.ClearAll();
        }

        CloseColorPanel();//关闭颜色面板
    }
    #endregion


    #region 改变笔刷颜色
    /// <summary>
    /// 改变笔刷颜色
    /// </summary>
    public void ChangeBrushColor()
    {
        if (penType == PenType.WriteBrush)
        {
            painting.brushColor = ColorManager._instance.colorShow.color;
        }
        else if (penType == PenType.WriteLine)
        {
            lineRender.SetColor(ColorManager._instance.colorShow.color);
        }

        CloseColorPanel();//关闭颜色面板
    }
    #endregion

    #region 关闭颜色面板
    /// <summary>
    /// 关闭颜色面板
    /// </summary>
    public void CloseColorPanel()
    {
        if (panel_Color.activeSelf)
        {
            panel_Color.SetActive(false);
        }
    }
    #endregion
}
