using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    private SensitiveWordFilter filter;

    void Awake()
    {
        filter = new SensitiveWordFilter(); // 自动加载 StreamingAssets/sensitive_words.txt
        // 或者手动传入词表：new SensitiveWordFilter(new List<string>{"词1","词2"});
    }

    // 发送消息前的过滤
    public void SendMessage(string rawMessage)
    {
        if (filter.HasSensitiveWord(rawMessage))
        {
            string filteredMsg = filter.Filter(rawMessage);
            Debug.Log($"包含敏感词，已过滤：{filteredMsg}");
            // 实际发送 filteredMsg
        }
        else
        {
            Debug.Log($"消息正常：{rawMessage}");
        }
    }

    // 动态添加用户自定义屏蔽词
    public void AddUserBlockWord(string word)
    {
        filter.AddWord(word);
    }

    // 移除屏蔽词
    public void RemoveUserBlockWord(string word)
    {
        filter.RemoveWord(word);
    }
}