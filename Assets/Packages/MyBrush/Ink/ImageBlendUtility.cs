using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageBlendUtility : MonoBehaviour
{
    public RenderTexture texRender; //画布
    public Material mat; //给定的shader新建材质
    public RawImage raw; //使用UGUI的RawImage显示，方便进行添加UI
    public Color color_bg;//背景色
    public GameRoot gameRoot;
    
    void Start()
    {
        if (texRender == null)
            texRender = new RenderTexture((int)raw.rectTransform.sizeDelta.x, (int)raw.rectTransform.sizeDelta.y, 24, RenderTextureFormat.ARGB32);
    }
    void OnEnable()
    {
        Clear();;//清除笔画
    }

    #region 笔刷图片纹理混合
    /// <summary>
    /// 笔刷图片纹理混合
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="sourceTexture"></param>
    /// <param name="scale"></param>
    public void DoImageBlend(Vector2 position, Color color, Texture sourceTexture, float scale)
    {
        DrawBrush(texRender, (int)position.x, (int)position.y, sourceTexture, color, scale);
    }
    void DrawBrush(RenderTexture destTexture, int x, int y, Texture sourceTexture, Color color, float scale)
    {
        DrawBrush(destTexture, new Rect(x, y, sourceTexture.width, sourceTexture.height), sourceTexture, color, scale);
    }
    void DrawBrush(RenderTexture destTexture, Rect destRect, Texture sourceTexture, Color color, float scale)
    {
        //检查是否有人签名
        if(gameRoot!=null)
            gameRoot.isSignOver = true;
        
        float left = destRect.xMin - destRect.width * scale / 2.0f;
        float right = destRect.xMin + destRect.width * scale / 2.0f;
        float top = destRect.yMin - destRect.height * scale / 2.0f;
        float bottom = destRect.yMin + destRect.height * scale / 2.0f;

        Graphics.SetRenderTarget(destTexture);

        GL.PushMatrix();
        GL.LoadOrtho();

        mat.SetTexture("_MainTex", sourceTexture);
        mat.SetColor("_Color", color);
        mat.SetPass(0);

        GL.Begin(GL.QUADS);

        //Vector2 v2 = RectTransformUtility.WorldToScreenPoint(Camera.main, LB.position);

        //Camera:overlay模式(左上角)
        Vector2 v2 = raw.rectTransform.position - new Vector3(raw.rectTransform.sizeDelta.x * 0.5f, raw.rectTransform.sizeDelta.y * -0.5f);

        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3((left - v2.x) / raw.rectTransform.sizeDelta.x, 1 - (v2.y - top) / raw.rectTransform.sizeDelta.y, 0);
        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3((right - v2.x) / raw.rectTransform.sizeDelta.x, 1 - (v2.y - top) / raw.rectTransform.sizeDelta.y, 0);
        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3((right - v2.x) / raw.rectTransform.sizeDelta.x, 1 - (v2.y - bottom) / raw.rectTransform.sizeDelta.y, 0);
        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3((left - v2.x) / raw.rectTransform.sizeDelta.x, 1 - (v2.y - bottom) / raw.rectTransform.sizeDelta.y, 0);

        //GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(left / Screen.width, top / Screen.height, 0);
        //GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(right / Screen.width, top / Screen.height, 0);
        //GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(right / Screen.width, bottom / Screen.height, 0);
        //GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(left / Screen.width, bottom / Screen.height, 0);

        GL.End();
        GL.PopMatrix();

        raw.texture = texRender;
    } 
    #endregion

    #region 清除画布
    /// <summary>
    /// 清除画布
    /// </summary>
    public void Clear()
    {
        if(gameRoot!=null)
            gameRoot.isSignOver = false;
        Graphics.SetRenderTarget(texRender);
        GL.PushMatrix();
        GL.Clear(true, true, color_bg);
        GL.PopMatrix();
    } 
    #endregion
}
