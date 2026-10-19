 
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class SensitiveWordUtil
{
    private const string END_FLAG = "IsEnd";
    private static Hashtable _hashtable = new Hashtable();
    
    /// <summary>
    /// 初始化 过滤词 词库
    /// </summary>
    public static void InitSensitiveWordMap(string[] worlds)
    {
        _hashtable = new Hashtable(worlds.Length);
        foreach (string word in worlds)
        {
            Hashtable hashtable = _hashtable;
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];
                if (IsSymbol(c)) continue;
                if (hashtable.ContainsKey(c)) {
                    hashtable = (Hashtable)hashtable[c];
                }else {
                    var newHashtable = new Hashtable();
                    newHashtable.Add(END_FLAG, 0);
                    hashtable.Add(c, newHashtable);
                    hashtable = newHashtable;
                }
                if (i == word.Length - 1) {
                    if (hashtable.ContainsKey(END_FLAG)) {
                        hashtable[END_FLAG] = 1;
                    }else {
                        hashtable.Add(END_FLAG, 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 查找所有敏感词，找到则返回敏感词长度
    /// </summary>
    /// <param name="text">需要过滤的字符串</param>
    /// <param name="startIndex">查找的起始位置</param>
    /// <returns></returns>
    public static int SearchSensitiveWord(string text, int startIndex)
    {
        Hashtable newMap = _hashtable;
        bool flag = false;
        int len = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            char word = text[i];
            if (IsSymbol(word)) {
                len++;
                continue;
            }
            Hashtable temp = (Hashtable)newMap[word];
            if (temp != null) {
                if ((int)temp[END_FLAG] == 1) flag = true;
                else newMap = temp;
                len++;
            }
            else break;
        }
        if (!flag) len = 0;
        return len;
    }

    /// <summary>
    /// 找到内容字符串内所有敏感词
    /// </summary>
    /// <param name="text">需要处理的文本</param>
    /// <returns></returns>
    public static List<string> GetAllSensitiveWords(string text)
    {
        List<string> result = new List<string>();
        for (int i = 0; i < text.Length; i++)
        {
            int length = SearchSensitiveWord(text, i);
            if (length > 0)
            {
                result.Add(text.Substring(i, length));
                i = i + length - 1;
            }
        }
        return result;
    }

    /// <summary>
    /// 替换 需要剔除的 敏感字
    /// </summary>
    /// <param name="text">需要处理的文本</param>
    /// <returns></returns>
    public static string ReplaceSensitiveWords(string text)
    {
        int i = 0;
        StringBuilder builder = new StringBuilder(text);
        while (i < text.Length)
        {
            int len = SearchSensitiveWord(text, i);
            if (len > 0) {
                for (int j = 0; j < len; j++)
                {
                    builder[i + j] = '*';
                }
                i += len;
            }
            else ++i;
        }
        return builder.ToString();
    }

    /// <summary>
    /// 判断是否是一个符号
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private static bool IsSymbol(char c)
    {
        int ic = c;
        // 0x2E80-0x9FFF 东亚文字范围
        return !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) && (ic < 0x2E80 || ic > 0x9FFF);
    }
}
