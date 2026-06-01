using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
 
/// <summary>
/// 
/// * Writer：June
/// 
/// * Data：2021.11.10
/// 
/// * Function：DFA算法
/// 
/// * Remarks：用于过滤敏感词
/// 
/// </summary>
 
 
public class DFAAlgorithm : MonoBehaviour
{
    private Hashtable hashtable;
    public List<string> filterList = new List<string>();
    [TextArea(1, 3)] public string speakStr;
 
    private void Start() => InitFilter(filterList);
 
 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            string resStr = StringCheckAndReplace(speakStr); 
            Debug.Log($"输出的结果：{resStr}");
        }
    }
    
 
    /// <summary>
    /// 初始化筛选器
    /// </summary>
    /// <param name="wordList">目标过滤词容器（链表）</param>
    private void InitFilter(List<string> wordList)
    {
        //初始化哈希表
        hashtable = new Hashtable(wordList.Count);
        //根据过滤词容器，确定外循环次数
        for (int i = 0; i < wordList.Count; i++)
        {
            //局部临时哈希表
            Hashtable tmpHs = hashtable;
            for (int j = 0; j < wordList[i].Length; j++)
            {
                //将字符串拆分成单个字符
                char ch = wordList[i][j];
                //判断哈希表中，是否已经包含有当前字符作为的键值
                if (tmpHs.ContainsKey(ch))
                {
                    tmpHs = (Hashtable)tmpHs[ch];
                }
                else
                {
                    Hashtable newHs = new Hashtable();
                    newHs.Add("IsEnd", 0);  //默认添加0，表示当前字符不是最后一个字符
                    tmpHs.Add(ch, newHs);   //将新的哈希表作为值，存在当前字符作为键中的哈希表（即哈希表中嵌套哈希表）
                    //取带有IsEnd哈希表，根据当前是否是最后一个字符，重新修改值
                    tmpHs = newHs;
                }
                if (j == (wordList[i].Length - 1))
                {
                    if (tmpHs.ContainsKey("IsEnd")) tmpHs["IsEnd"] = 1;
                    else tmpHs.Add("IsEnd", 1);
                }
            }
        }
    }
 
 
    /// <summary>
    /// 字符串检测并替换
    /// </summary>
    /// <param name="targetStr">目标字符串</param>
    /// <returns></returns>
    private string StringCheckAndReplace(string targetStr)
    {
        StringBuilder stringBuilder = new StringBuilder(targetStr);
        int len = 0;
        for (int i = 0; i < targetStr.Length; )
        {
            len = SensitiveWordsLength(targetStr, i);
            //判定如果没有过滤词则不做处理
            if (len == 0)
            {
                i++;
                continue;
            }
            for (int j = 0; j < len; j++)
            {
                stringBuilder[i + j] = '*';
            }
            i += len;
        }
        return stringBuilder.ToString();
    }
 
 
    /// <summary>
    /// 敏感词长度
    /// </summary>
    /// <param name="targetStr">目标字符串</param>
    /// <param name="beginIndex">开始遍历的索引</param>
    /// <returns></returns>
    private int SensitiveWordsLength(string targetStr, int beginIndex)
    {
        //当前所在的哈希表（节点）
        Hashtable curHs = hashtable;
        //记录长度
        int len = 0;
        //索引从给定的开始
        for (int i = beginIndex; i < targetStr.Length; i++)
        {
            char ch = targetStr[i];
            //判断当前字符是否有效   使用ASCII判断
            if (ch > 32 && ch < 126) continue;
            //新建一个临时哈希表,指向子哈希表(子节点)
            Hashtable newtmpHs = (Hashtable)curHs[ch];
            if (newtmpHs != null)
            {
                //判定是否是末节点
                if ((int)newtmpHs["IsEnd"] == 1) len = i + 1 - beginIndex;
                else curHs = newtmpHs;  //指向子节点(子哈希表)
            }
            else break;
        }
        return len;
    }
}