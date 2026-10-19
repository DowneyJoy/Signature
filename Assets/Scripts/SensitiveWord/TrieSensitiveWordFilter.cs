using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 敏感词过滤器（Trie树实现，支持基础词库+用户自定义词库，自动保存用户词库）
/// </summary>
public class SensitiveWordFilter
{
    private class TrieNode
    {
        public Dictionary<char, TrieNode> Children = new Dictionary<char, TrieNode>();
        public bool IsEndOfWord = false;
    }

    private TrieNode root = new TrieNode();
    private HashSet<string> allSensitiveWords = new HashSet<string>(); // 所有词（用于保存）
    private bool ignoreCase = true;
    private bool convertHalfWidth = true;

    // 词库文件路径
    private static string baseWordPath => Path.Combine(Application.streamingAssetsPath, "sensitive_words.txt");
    private static string userWordPath => Path.Combine(Application.streamingAssetsPath, "sensitive_words_user.txt");

    /// <summary>
    /// 构造函数：加载基础词库 + 用户词库
    /// </summary>
    public SensitiveWordFilter()
    {
        LoadBaseWords();
        LoadUserWords();
        RebuildTrie();
    }

    // 加载只读基础词库（StreamingAssets）
    private void LoadBaseWords()
    {
        try
        {
            if (File.Exists(baseWordPath))
            {
                string[] lines = File.ReadAllLines(baseWordPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        allSensitiveWords.Add(NormalizeWord(trimmed));
                }
                Debug.Log($"加载基础词库成功，共 {lines.Length} 个词");
            }
            else
            {
                Debug.LogWarning($"基础词库文件不存在：{baseWordPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载基础词库失败：{e.Message}");
        }
    }

    // 加载用户自定义词库（persistentDataPath，可读写）
    private void LoadUserWords()
    {
        try
        {
            if (File.Exists(userWordPath))
            {
                string[] lines = File.ReadAllLines(userWordPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        allSensitiveWords.Add(NormalizeWord(trimmed));
                }
                Debug.Log($"加载用户词库成功，共 {lines.Length} 个词");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载用户词库失败：{e.Message}");
        }
    }

    // 保存用户词库到持久化路径
    private void SaveUserWords()
    {
        try
        {
            // 注意：allSensitiveWords 包含了基础词+用户词，我们只保存用户自己添加的
            // 但为了简单，这里保存全部词库（包含基础词），基础词重复保存也没问题。
            // 更好的做法：维护单独的 userWords HashSet，但需要额外逻辑。这里为了简洁，直接保存全部。
            // 若希望只保存用户添加的，可以维护一个 userCustomSet，见下方注释说明。
            string directory = Path.GetDirectoryName(userWordPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllLines(userWordPath, allSensitiveWords, Encoding.UTF8);
            Debug.Log($"用户词库已保存到：{userWordPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存用户词库失败：{e.Message}");
        }
    }

    // 重建Trie树（在加载词库或增删后调用）
    private void RebuildTrie()
    {
        root = new TrieNode();
        foreach (string word in allSensitiveWords)
        {
            InsertTrie(word);
        }
    }

    private void InsertTrie(string word)
    {
        TrieNode node = root;
        foreach (char c in word)
        {
            if (!node.Children.ContainsKey(c))
                node.Children[c] = new TrieNode();
            node = node.Children[c];
        }
        node.IsEndOfWord = true;
    }

    /// <summary>
    /// 添加敏感词（同时保存到用户词库文件）
    /// </summary>
    public void AddWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;
        string normalized = NormalizeWord(word);
        if (allSensitiveWords.Add(normalized))
        {
            InsertTrie(normalized);
            SaveUserWords();  // 持久化
            Debug.Log($"添加敏感词：{word}");
        }
    }

    /// <summary>
    /// 移除敏感词（同时从用户词库文件中删除）
    /// </summary>
    public bool RemoveWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        string normalized = NormalizeWord(word);
        if (allSensitiveWords.Remove(normalized))
        {
            RebuildTrie();  // 重建Trie树
            SaveUserWords();
            Debug.Log($"移除敏感词：{word}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检查文本是否包含敏感词
    /// </summary>
    public bool HasSensitiveWord(string text)
    {
        string processed = PreprocessText(text);
        for (int i = 0; i < processed.Length; i++)
        {
            TrieNode node = root;
            int j = i;
            while (j < processed.Length && node.Children.TryGetValue(processed[j], out node))
            {
                j++;
                if (node.IsEndOfWord)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 过滤敏感词，替换为等长星号(*)
    /// </summary>
    public string Filter(string text)
    {
        string processed = PreprocessText(text);
        StringBuilder result = new StringBuilder(processed);
        for (int i = 0; i < result.Length; i++)
        {
            TrieNode node = root;
            int matchLen = 0;
            int j = i;
            while (j < result.Length && node.Children.TryGetValue(result[j], out node))
            {
                j++;
                if (node.IsEndOfWord)
                    matchLen = j - i;
            }
            if (matchLen > 0)
            {
                for (int k = i; k < i + matchLen; k++)
                    result[k] = '*';
                i += matchLen - 1;
            }
        }
        return result.ToString();
    }

    // ========== 辅助方法 ==========

    private string NormalizeWord(string word)
    {
        string result = word;
        if (convertHalfWidth)
            result = ToHalfWidth(result);
        if (ignoreCase)
            result = result.ToLowerInvariant();
        return result;
    }

    private string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string result = text;
        if (convertHalfWidth)
            result = ToHalfWidth(result);
        if (ignoreCase)
            result = result.ToLowerInvariant();
        return result;
    }

    private static string ToHalfWidth(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        StringBuilder sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (c == 0x3000) // 全角空格
                sb.Append(' ');
            else if (c >= 0xFF01 && c <= 0xFF5E) // 全角字符范围
                sb.Append((char)(c - 0xFEE0));
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}