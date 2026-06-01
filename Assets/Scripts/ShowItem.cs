using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class ShowItem : MonoBehaviour
{
    public Text SignItemName;
    public RawImage SignItemImage;
    public Vector3 startPos;
    public float endPosX;
    public Sequence sequence;
    public float duration = 3f;
    public bool isShow = false;
    private Tween moveTween;   // 改为 Tween 类型（可同时持有 Sequence 或单 Tween）
    public int ID = 0;
    void Awake()
    {
        transform.localScale = Vector3.zero;
    }

    public void SetName(string name)
    {
        SignItemName.text = "<color=#FFFFFF00>XXX</color>" + name;
    }

    public void SetImage(Texture tex)
    {
        SignItemImage.texture = tex;
    }

    public void Show(SignInfo signInfo = null)
    {
        isShow = true;
        transform.localScale = Vector3.one;
        if (signInfo != null)
        {
            SetName(signInfo.InfoContent);
            SetImage(signInfo.InfoImage);
        }
    }

    public void PlayAnimation()
    {
        transform.localPosition =  startPos;
        transform.localScale = Vector3.zero;
        sequence.Stop();
        sequence = Sequence.Create()
            .Group(Tween.LocalPositionX(transform, endPosX, duration, Ease.Linear))
            .OnComplete(() =>
            {
                ClientRoot.Instance.showItemPool.Release(this);
                ClientRoot.Instance.ShowSignInfo(ID);
                isShow = false;
            });
    }
}