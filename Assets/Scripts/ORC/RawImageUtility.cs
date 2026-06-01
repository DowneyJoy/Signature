using UnityEngine;
using UnityEngine.UI;

public static class RawImageUtility
{
    /// <summary>
    /// 从 RawImage 获取 Texture2D（确保可读写）
    /// </summary>
    /// <param name="rawImage">目标 RawImage</param>
    /// <returns>可读写的 Texture2D，失败返回 null</returns>
    public static Texture2D GetTexture2D(RawImage rawImage)
    {
        if (rawImage == null || rawImage.mainTexture == null)
            return null;

        Texture source = rawImage.mainTexture;
        int width = source.width;
        int height = source.height;

        // 情况1：源是 RenderTexture
        if (source is RenderTexture rt)
        {
            return FromRenderTexture(rt);
        }
        // 情况2：源是 Texture2D
        else if (source is Texture2D tex2D)
        {
            return FromTexture2D(tex2D);
        }
        // 情况3：其他类型（如 WebCamTexture、MovieTexture 等）
        else
        {
            Debug.LogError($"不支持的纹理类型: {source.GetType()}");
            return null;
        }
    }

    private static Texture2D FromRenderTexture(RenderTexture rt)
    {
        // 保存当前激活的 RenderTexture
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = previous;
        return tex;
    }

    private static Texture2D FromTexture2D(Texture2D source)
    {
        // 如果源纹理可读，直接复制像素
        if (source.isReadable)
        {
            Texture2D copy = new Texture2D(source.width, source.height, source.format, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }
        // 不可读时，通过临时 RenderTexture 中转
        else
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, tempRT);
            Texture2D result = FromRenderTexture(tempRT);
            RenderTexture.ReleaseTemporary(tempRT);
            return result;
        }
    }
}