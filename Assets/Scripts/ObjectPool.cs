using System;
using System.Collections.Generic;

/// <summary>
/// 通用对象池
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ObjectPool<T> where T : class
{
    private readonly Stack<T> stack = new Stack<T>();
    private readonly Func<T> createFunc;          // 创建对象的委托
    private readonly Action<T> onGet;             // 取出对象时的回调
    private readonly Action<T> onRelease;         // 放回对象时的回调
    private readonly Action<T> onDestroy;         // 销毁对象时的回调
    private int maxSize;                          // 池最大容量（0 表示无限制）

    public int CountAll { get; private set; }     // 已创建的总数（包含池内和正在使用的）
    public int CountInactive => stack.Count;      // 池内可用对象数
    public int CountActive => CountAll - stack.Count;

    public ObjectPool(
        Func<T> createFunc,
        Action<T> onGet = null,
        Action<T> onRelease = null,
        Action<T> onDestroy = null,
        int maxSize = 0)
    {
        this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        this.onGet = onGet;
        this.onRelease = onRelease;
        this.onDestroy = onDestroy;
        this.maxSize = maxSize;
    }

    /// <summary>从池中取出一个对象</summary>
    public T Get()
    {
        T obj;
        if (stack.Count > 0)
        {
            obj = stack.Pop();
        }
        else
        {
            obj = createFunc();
            CountAll++;
        }
        onGet?.Invoke(obj);
        return obj;
    }

    /// <summary>将对象放回池中</summary>
    public void Release(T obj)
    {
        if (obj == null) return;

        if (maxSize > 0 && stack.Count >= maxSize)
        {
            // 超过最大容量，直接销毁
            onDestroy?.Invoke(obj);
            CountAll--;
        }
        else
        {
            onRelease?.Invoke(obj);
            stack.Push(obj);
        }
    }

    /// <summary>清空池（销毁所有对象）</summary>
    public void Clear()
    {
        if (onDestroy != null)
        {
            foreach (var obj in stack)
                onDestroy(obj);
        }
        stack.Clear();
        CountAll = 0;
    }
}