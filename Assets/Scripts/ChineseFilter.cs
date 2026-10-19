using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using hyjiacan.py4n;
using UnityEngine;

public class ChineseFilter
{

    public static async UniTask LoadChinese()
    {
        // 假设 chinese.txt 放在 StreamingAssets 下
        string path = Path.Combine(Application.streamingAssetsPath, "chinese.txt");
        charPriority = await LoadPriorityDictAsync(path);
    }
    public static async UniTask<Dictionary<char, int>> LoadPriorityDictAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"文件不存在: {filePath}");
            return null;
        }

        await UniTask.SwitchToThreadPool();
        string content = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
        var dict = new Dictionary<char, int>();
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c)) continue;
            if (!dict.ContainsKey(c))
                dict[c] = i;
        }
        await UniTask.SwitchToMainThread();
        Debug.Log($"异步加载 {dict.Count} 个汉字");
        return dict;
    }
    // 简化的常用字清单 (可按需替换为完整的3500字表)
    private static string commonChineseChars = "的一是不了人我在有他这中大来上国个到说们为子和你地出道也时年得就那要下以";
    private static Dictionary<char, int> charPriority = new Dictionary<char, int>();

    public static void LoadPriority()
    {
        for (int i = 0; i < commonChineseChars.Length; i++)
        {
            // 索引越小，优先级越高（越常见）
            charPriority[commonChineseChars[i]] = i;
        }
    }
    public static string[] GetSortedHanzi(string pinyin)
    {
        // 1. 获取原始结果
        var hanziArray = Pinyin4Net.GetHanzi(pinyin, false);
        if (hanziArray == null || hanziArray.Length == 0)
            return hanziArray;

        // 2. 进行排序
        var sortedList = hanziArray
            .Select(h => h[0])                 // 转换为单个字符
            .OrderBy(c => GetPriority(c))      // 按优先级升序排序
            .Select(c => c.ToString())         // 转回字符串
            .ToList();

        return sortedList.ToArray();
    }

    private static int GetPriority(char c)
    {
        return charPriority.TryGetValue(c, out int priority) ? priority : int.MaxValue;
    }
    private static Dictionary<string, string[]> pinyinCache = new Dictionary<string, string[]>();

    public static string[] GetSortedHanziWithCache(string pinyin)
    {
        // 如果缓存中有，直接返回
        if (pinyinCache.ContainsKey(pinyin))
            return pinyinCache[pinyin];

        // 获取原始结果并排序
        var hanziArray = Pinyin4Net.GetHanzi(pinyin, false);
        if (hanziArray == null || hanziArray.Length == 0)
            return hanziArray;

        var sortedList = hanziArray.Select(h => h[0])
            .OrderBy(c => GetPriority(c))
            .Select(c => c.ToString())
            .ToArray();

        // 存入缓存后返回
        pinyinCache[pinyin] = sortedList;
        return sortedList;
    }
}
