using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FileUtility
{
    public static async UniTask SaveInfo(byte[] pngData,string content)
    {
        string timer = DateTimeUtility.GetCurrentDateTime("yy-MM-dd");
        string foldName = Path.Combine(Application.streamingAssetsPath, timer);
    
        await UniTask.SwitchToThreadPool();
    
        if (!Directory.Exists(foldName))
            Directory.CreateDirectory(foldName);
        string name = DateTimeUtility.GetCurrentDateTime("HH-mm-ss");
        string pngName = name +".png";
        await SavePngAsync(pngData, foldName, pngName);
        string txtName = name +".txt";
        await SaveTextWithUniTask(content,foldName,txtName);
    }
    /// <summary>
    /// 异步保存PNG
    /// </summary>
    /// <param name="pngData"></param>
    /// <param name="filePath"></param>
    public static async UniTask SavePngAsync(byte[] pngData,string foldName,string fileName)
    {
        string filePath = foldName +"/"+ fileName;
        await File.WriteAllBytesAsync(filePath, pngData);
        await UniTask.SwitchToMainThread();
        //Debug.Log($"保存完成: {filePath}");
    }
    /// <summary>
    /// 异步保存文本
    /// </summary>
    /// <param name="content"></param>
    /// <param name="fileName"></param>
    public static async UniTask SaveTextWithUniTask(string content,string foldName,string fileName)
    {
        string filePath = foldName +"/"+ fileName;
        await File.WriteAllTextAsync(filePath, content);
        await UniTask.SwitchToMainThread();     
        //Debug.Log($"保存完成: {filePath}");
    }
    /// <summary>
    /// 读取文本
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string LoadText(string fileName)
    {
        if (File.Exists(fileName))
            return File.ReadAllText(fileName);
        else
            return null;
    }
    /// <summary>
    /// 异步读取PNG
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static async UniTask<Texture2D> LoadPngAsync(string fileName)
    {
        // 后台线程读取字节
        byte[] fileData = await UniTask.RunOnThreadPool(() => File.ReadAllBytes(fileName));
    
        // 回到主线程创建纹理
        await UniTask.SwitchToMainThread();
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
            return texture;
        return null;
    }
    /// <summary>
    /// 读取文件夹下所有.txt文件
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public static async UniTask<Dictionary<string, string>> ReadAllTxtFilesAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"文件夹不存在: {folderPath}");
            return null;
        }

        string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");
        var contents = new Dictionary<string, string>();

        await UniTask.SwitchToThreadPool();

        foreach (string filePath in txtFiles)
        {
            try
            {
                string fileName = Path.GetFileName(filePath).Replace(".txt","");
                string content = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
                lock (contents) // 多线程写入字典需加锁
                {
                    contents[fileName] = content;
                    //Debug.Log($"{fileName}: {content}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"读取失败 {filePath}: {e.Message}");
            }
        }

        await UniTask.SwitchToMainThread();
        return contents;
    }
    public static async UniTask<Dictionary<string,Texture2D>> LoadAllPngAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"文件夹不存在: {folderPath}");
            return null;
        }

        string[] pngFiles = Directory.GetFiles(folderPath, "*.png");
        var contents = new Dictionary<string, Texture2D>();

        // 切换到后台线程读取文件字节
        await UniTask.SwitchToThreadPool();

        var fileDataList = new List<byte[]>();
        var fileNames = new List<string>();
        foreach (string filePath in pngFiles)
        {
            try
            {
                byte[] data = await File.ReadAllBytesAsync(filePath);
                lock (fileDataList) fileDataList.Add(data);
                string fileName = Path.GetFileName(filePath).Replace(".png","");
                lock (fileNames)
                {
                    fileNames.Add(fileName);                    
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"异步读取失败 {filePath}: {e.Message}");
            }
        }

        // 回到主线程创建纹理
        await UniTask.SwitchToMainThread();
        for (int i = 0; i < pngFiles.Length && i < fileDataList.Count; i++)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileDataList[i]))
            {
                
                tex.name = Path.GetFileNameWithoutExtension(pngFiles[i]);
                lock (contents)
                {
                    //Debug.Log($"{fileNames[i]}");
                    contents[fileNames[i]] = tex;
                }
            }
            else
            {
                Debug.LogWarning($"创建纹理失败: {pngFiles[i]}");
                Object.Destroy(tex);
            }
        }

        return contents;
    }

    public static async UniTask<List<SignInfo>> LoadSignInfoAsync(string folderPath)
    {
        var contents = new List<SignInfo>();
        var fileNames = await GetTxtFilePathsAsync(folderPath);
        for (int i = 0; i < fileNames.Count; i++)
        {
            fileNames[i] = fileNames[i].Replace(".txt","");
        }
        var textResult = await ReadAllTxtFilesAsync(folderPath);
        var pngResult = await LoadAllPngAsync(folderPath);
        for (int i = 0; i < fileNames.Count; i++)
        {
            SignInfo signInfo = new SignInfo();

            string content = "";
            textResult.TryGetValue(fileNames[i], out content);
            signInfo.InfoContent = content;
            Texture2D tex = new Texture2D(2, 2);
            pngResult.TryGetValue(fileNames[i], out tex);
            signInfo.InfoImage = tex;
            contents.Add(signInfo);
        }
        return contents;
    }
    public static List<string> GetTxtFilePaths(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"文件夹不存在: {folderPath}");
            return null;
        }

        // 获取所有 .txt 文件的完整路径
        string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");
        return new List<string>(txtFiles);
    }
    
    public static async UniTask<List<string>> GetTxtFilePathsAsync(string folderPath)
    {
        // 1. 切换到后台线程执行同步文件操作
        await UniTask.SwitchToThreadPool();

        List<string> fileNames = new List<string>();
        if (Directory.Exists(folderPath))
        {
            string[] fullPaths = Directory.GetFiles(folderPath, "*.txt");
            foreach (string path in fullPaths)
            {
                fileNames.Add(Path.GetFileName(path));
            }
        }
        else
        {
            Debug.LogError($"文件夹不存在: {folderPath}");
        }

        // 2. 回到主线程（便于后续更新 UI）
        await UniTask.SwitchToMainThread();
        return fileNames;
    }
}
