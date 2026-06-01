using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowItemPool : ObjectPool<ShowItem>
{
    public ShowItemPool(Func<ShowItem> createFunc, Action<ShowItem> onGet = null, Action<ShowItem> onRelease = null, Action<ShowItem> onDestroy = null, int maxSize = 0) : base(createFunc, onGet, onRelease, onDestroy, maxSize)
    {
        
    }
}
