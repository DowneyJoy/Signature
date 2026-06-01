using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameObject 对象池管理器
/// </summary>
public static class GameObjectPool
{
    private static Dictionary<string, ObjectPool<GameObject>> pools = new Dictionary<string, ObjectPool<GameObject>>();

    /// <summary>
    /// 预创建对象池
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="initialCount">初始预创建数量</param>
    /// <param name="maxSize">池最大容量（超过则直接销毁）</param>
    /// <param name="parent">可选父节点（用于整理层级）</param>
    public static void Preload(GameObject prefab, int initialCount, int maxSize = 0, Transform parent = null)
    {
        if (prefab == null) return;
        string key = prefab.name;
        if (!pools.ContainsKey(key))
            CreatePool(prefab, maxSize, parent);

        var pool = pools[key];
        var toCreate = initialCount - pool.CountInactive;
        if (toCreate > 0)
        {
            for (int i = 0; i < toCreate; i++)
            {
                GameObject obj = CreateNew(prefab, parent);
                pool.Release(obj);
            }
        }
    }

    /// <summary>
    /// 从池中获取一个对象
    /// </summary>
    public static GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;
        string key = prefab.name;
        if (!pools.ContainsKey(key))
            CreatePool(prefab, parent: parent);

        var pool = pools[key];
        GameObject obj = pool.Get();
        obj.transform.SetParent(parent, false);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    public static void Release(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        // 查找对应的池（通过预制体名称）
        string key = obj.name.Replace("(Clone)", "");
        if (pools.TryGetValue(key, out var pool))
        {
            pool.Release(obj);
        }
        else
        {
            // 如果没有池，直接销毁
            Object.Destroy(obj);
        }
    }

    /// <summary>
    /// 清空所有池
    /// </summary>
    public static void ClearAll()
    {
        foreach (var pool in pools.Values)
            pool.Clear();
        pools.Clear();
    }

    private static void CreatePool(GameObject prefab, int maxSize = 0, Transform parent = null)
    {
        string key = prefab.name;
        if (pools.ContainsKey(key)) return;

        var pool = new ObjectPool<GameObject>(
            createFunc: () => CreateNew(prefab, parent),
            onGet: obj => obj.SetActive(true),
            onRelease: obj => obj.SetActive(false),
            onDestroy: obj => Object.Destroy(obj),
            maxSize: maxSize
        );
        pools[key] = pool;
    }

    private static GameObject CreateNew(GameObject prefab, Transform parent)
    {
        GameObject obj = Object.Instantiate(prefab, parent);
        obj.name = prefab.name; // 移除 "(Clone)"，便于识别
        return obj;
    }
}