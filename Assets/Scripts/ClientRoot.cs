using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PrimeTween;
using TuanjieAI.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityUtils;

public class ClientRoot : Singleton<ClientRoot>
{
    public List<ShowItem> staticShowItems = new List<ShowItem>();
    public List<ShowItem> showItems = new List<ShowItem>();
    public float duration = 15f;
    public ShowItemPool showItemPool;
    public Transform ShowItemParent;
    public GameObject ShowItemPrefab;
    public List<SignInfo> signInfos = new List<SignInfo>();
    public List<SignInfo> currentShowItems;
    public int currentSign = 0;
    public int currentPage = 0;
    public int allPages = 0;
    public RepeatTimer repeatTimer;
    public CustomSystemInfo customSystemInfo;
    public Text SumText;
    public Text PageText;
    public GameObject HorseAnim;
    public HorseParticleSystem  HorseParticle;
    void Start()
    {
        Loom.RunAsync(() => { });

        #region OldVersion

        showItemPool = new ShowItemPool(() =>
        {
            GameObject go = Instantiate(ShowItemPrefab);
            go.transform.SetParent(ShowItemParent);
            ShowItem showItem = go.AddComponent<ShowItem>();
            return showItem;
        },null,null,null,10);
        foreach (ShowItem showItem in showItems)
        {
            showItemPool.Release(showItem);
        }

        #endregion

        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        customSystemInfo = ConfigManger.LoadInfo("CustomSystemInfo.json");
        LoadLocalInfo();
        currentSign = 0;
    }

    public async void LoadLocalInfo()
    {
        signInfos.Clear();
        string timer = DateTimeUtility.GetCurrentDateTime("yy-MM-dd");
        string foldName = Path.Combine(Application.streamingAssetsPath, timer);
        if (!Directory.Exists(foldName))
        {
            Directory.CreateDirectory(foldName);
        }
        else
        {
            signInfos.AddRange(await FileUtility.LoadSignInfoAsync(foldName));
        }
        SumText.text = $"今日留言数量：{signInfos.Count}";
        //allPages = PaginationHelper.GetTotalPages(signInfos.Count, 8);
        //currentPage = 0;
        //if(signInfos.Count == 0)
        //    UpdatePage(0,0,0);
        //else
        //    UpdatePage(signInfos.Count,allPages,currentPage+1);
        //repeatTimer.StartTimer();
        StartCoroutine(WaitStart());
    }

    public void UpdatePage(int sum,int sumPage,int current)
    {
        SumText.text = $"今日留言数量：{sum}";
        PageText.text = $"{current}/{sumPage}";
    }

    public void AddShowItem(SignInfo signInfo)
    {
        signInfos.Add(signInfo);
        allPages = PaginationHelper.GetTotalPages(signInfos.Count, 8);
        UpdatePage(signInfos.Count,allPages,currentPage+1);
        if (signInfos.Count <= 8)
        {
            for (int i = 0; i < 8; i++)
            {
                if(i<signInfos.Count)
                    staticShowItems[i].AnimationShow(signInfos[i]);
                else
                {
                    staticShowItems[i].AnimationHide();
                }
            }
        }
        else
        {
            currentPage = allPages - 1;
            currentShowItems = PaginationHelper.GetPage<SignInfo>(signInfos, currentPage, 8);
            UpdatePage(signInfos.Count,allPages,currentPage+1);
            for (int i = 0; i < 8; i++)
            {
                if(i<currentShowItems.Count)
                    staticShowItems[i].AnimationShow(currentShowItems[i]);
                else
                {
                    staticShowItems[i].AnimationHide();
                }
            }
            currentPage = 0;
            repeatTimer.ResumeTimer();
        }
    }

    public void PasueAnim()
    {
        repeatTimer.PauseTimer();
        for (int i = 0; i < staticShowItems.Count; i++)
        {
            staticShowItems[i].AnimationHide();
        }
    }

    public void ChangeInfo()
    {
        if(signInfos.Count == 0)
            return;

        currentShowItems = PaginationHelper.GetPage<SignInfo>(signInfos, currentPage, 8);
        UpdatePage(signInfos.Count,allPages,currentPage+1);
        for (int i = 0; i < 8; i++)
        {
            if(i<currentShowItems.Count)
                staticShowItems[i].AnimationShow(currentShowItems[i]);
            else
            {
                staticShowItems[i].AnimationHide();
            }
        }
        currentPage++;
        if (currentPage >= allPages && allPages != 0)
        {
            currentPage = 0;
        }

        if (signInfos.Count <= 8)
        {
            repeatTimer.PauseTimer();
        }
    }

    #region Old Version

    IEnumerator WaitStart()
    {
        yield return new WaitForSeconds(duration/showItems.Count/2);
        ShowItem sh1 = showItemPool.Get();
        currentSign = 0;
        ShowSignInfo(sh1);
        yield return new WaitForSeconds(duration/showItems.Count);
        ShowItem sh2 = showItemPool.Get();
        ShowSignInfo(sh2);
        yield return new WaitForSeconds(duration/showItems.Count);
        ShowItem sh3 = showItemPool.Get();
        ShowSignInfo(sh3);
        yield return new WaitForSeconds(duration/showItems.Count);
        ShowItem sh4 = showItemPool.Get();
        ShowSignInfo(sh4);
        yield return new WaitForSeconds(duration/showItems.Count);
        ShowItem sh5 = showItemPool.Get();
        ShowSignInfo(sh5);
    }

    public void ShowSignInfo(ShowItem showItem)
    { 
        showItem.PlayAnimation();
        if (currentSign < signInfos.Count)
        {
            showItem.Show(signInfos[currentSign]);
            currentSign++;
        }
    }

    public void ShowSignInfo(int index)
    {
        ShowItem showItem = showItemPool.Get();
        showItem.PlayAnimation();
        //Debug.Log(currentSign);
        if (currentSign == signInfos.Count)
        {
            Debug.Log("Last Item");
            currentSign = 0;
        }

        if (currentSign < signInfos.Count)
        {
            showItem.Show(signInfos[currentSign]);
            currentSign++;
        }
    }

    public void AddShowItemOld(SignInfo signInfo)
    {
        signInfos.Add(signInfo);
        HorseParticle.ShowParticle(signInfo);
        SumText.text = $"今日留言数量：{signInfos.Count}";
    }
    #endregion
}

public static class PaginationHelper
{
    /// <summary>
    /// 计算总页数
    /// </summary>
    /// <param name="totalItems">元素总数</param>
    /// <param name="pageSize">每页元素数量</param>
    /// <returns>总页数</returns>
    public static int GetTotalPages(int totalItems, int pageSize)
    {
        if (pageSize <= 0) return 0;
        return (totalItems + pageSize - 1) / pageSize;
    }

    /// <summary>
    /// 从列表中获取指定页的数据
    /// </summary>
    public static List<T> GetPage<T>(List<T> source, int pageIndex, int pageSize)
    {
        if (source == null || pageSize <= 0) return new List<T>();
        int startIndex = pageIndex * pageSize;
        if (startIndex >= source.Count) return new List<T>();
        int count = Mathf.Min(pageSize, source.Count - startIndex);
        return source.GetRange(startIndex, count);
    }
}