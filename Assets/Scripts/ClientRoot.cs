using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityUtils;

public class ClientRoot : Singleton<ClientRoot>
{
    public List<ShowItem> showItems = new List<ShowItem>();
    public float duration = 15f;
    public ShowItemPool showItemPool;
    public Transform ShowItemParent;
    public GameObject ShowItemPrefab;
    public List<SignInfo> signInfos = new List<SignInfo>();
    public int currentSign = 0;
    void Start()
    {
        Loom.RunAsync(() => { });
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
        LoadLocalInfo();
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
        
        StartCoroutine(WaitStart());
    }


    IEnumerator WaitStart()
    {
        ShowItem sh1 = showItemPool.Get();
        currentSign = 0;
        ShowSignInfo(sh1);
        yield return new WaitForSeconds(duration/5f);
        ShowItem sh2 = showItemPool.Get();
        ShowSignInfo(sh2);
        yield return new WaitForSeconds(duration/5f);
        ShowItem sh3 = showItemPool.Get();
        ShowSignInfo(sh3);
        yield return new WaitForSeconds(duration/5f);
        ShowItem sh4 = showItemPool.Get();
        ShowSignInfo(sh4);
        yield return new WaitForSeconds(duration/5f);
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
        if (currentSign == signInfos.Count)
        {
            //Debug.Log("Restart");
            currentSign = 0;
        }

        if (currentSign < signInfos.Count)
        {
            showItem.Show(signInfos[currentSign]);
            currentSign++;
        }
    }
}
