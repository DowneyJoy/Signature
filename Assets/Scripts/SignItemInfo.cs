using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class SignItemInfo : MonoBehaviour
{
    public Text SignItemName;
    public RawImage SignItemImage;

    public void SetName(string name)
    {
        SignItemName.text = name;
    }

    public void SetImage(Texture tex)
    {
        SignItemImage.texture = tex;
    }
}
