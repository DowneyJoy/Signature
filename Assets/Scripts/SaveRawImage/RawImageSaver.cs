using System;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;

public class RawImageSaver : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;          // 目标RawImage
    [SerializeField] private string fileName = "capture.png"; // 保存文件名
    [SerializeField] private string fileNameFormat = "yyyy-MM-dd_HH-mm-ss";
    public Texture2D tmpTexture;

    // 公开方法：保存当前RawImage画面（带透明通道）
    public async void SaveCurrentFrame()
    {
        // 1. 生成带时间戳的文件名
        string timestamp = System.DateTime.Now.ToString(fileNameFormat);
        string fileName = $"{timestamp}.png";

        // 2. 主线程：捕获纹理并编码为PNG（保留透明度）
        byte[] imageData = await CaptureAndEncodeAsync();

        if (imageData == null)
        {
            Debug.LogError("编码失败，无法保存图片");
            return;
        }

        // 3. 异步写入文件（不阻塞主线程）
        string savePath = GetWritablePath(fileName);
        await WriteFileAsync(savePath, imageData);

        //Debug.Log($"透明通道图片已保存: {savePath}");
        // 4. 将图片发送到显示端
        // if (udpSender != null)
        // {
        //     udpSender.SendBytes(imageData);
        // }
    }

    // 主线程执行：从RawImage获取纹理并编码为PNG
    private Task<byte[]> CaptureAndEncodeAsync()
    {
        var tcs = new TaskCompletionSource<byte[]>();
        try
        {
            Texture2D tex = GetTextureFromRawImage();
            if (tex == null)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }
            // EncodeToPNG 会保留Alpha通道（前提是纹理格式支持，如RGBA32）
            byte[] data = tex.EncodeToPNG();
            tcs.SetResult(data);
        }
        catch (System.Exception e)
        {
            tcs.SetException(e);
        }
        return tcs.Task;
    }

    // 从RawImage提取Texture2D（兼容RenderTexture和普通Texture2D）
    public Texture2D GetTextureFromRawImage()
    {
        Texture source = rawImage.mainTexture;
        if (source == null) return null;

        int width = source.width;
        int height = source.height;

        // 处理 RenderTexture
        if (source is RenderTexture rt)
        {
            // 创建RGBA32格式的Texture2D，自动支持透明
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            tmpTexture = tex;
            return tex;
        }
        // 处理 Texture2D
        else if (source is Texture2D tex2D)
        {
            // 确保纹理可读
            if (tex2D.isReadable)
            {
                // 复制像素（保留原始Alpha）
                Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.SetPixels(tex2D.GetPixels());
                copy.Apply();
                tmpTexture = copy;
                return copy;
            }
            else
            {
                // 不可读时通过临时RenderTexture中转
                RenderTexture tempRT = RenderTexture.GetTemporary(width, height);
                Graphics.Blit(tex2D, tempRT);
                Texture2D result = GetTextureFromRenderTexture(tempRT);
                RenderTexture.ReleaseTemporary(tempRT);
                tmpTexture = result;
                return result;
            }
        }
        else
        {
            Debug.LogError("不支持的纹理类型: " + source.GetType());
            tmpTexture =  null;
            return null;
        }
    }

    private Texture2D GetTextureFromRenderTexture(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    // 获取可写入的路径（优先使用持久化路径，也可按需配置）
    private string GetWritablePath(string fileName)
    {
        // 推荐：保存到持久化数据路径（所有平台可写）
        // return Path.Combine(Application.persistentDataPath, fileName);

        // 如果坚持要写入StreamingAssets（仅限编辑器/Windows等可写平台）：
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            return Path.Combine(Application.streamingAssetsPath, fileName);
        else
           return Path.Combine(Application.persistentDataPath, fileName);
    }

    // 异步写入文件（不阻塞主线程）
    private async Task WriteFileAsync(string path, byte[] data)
    {
        // 确保目录存在
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // 使用异步文件流写入
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            await fs.WriteAsync(data, 0, data.Length);
        }
    }
}