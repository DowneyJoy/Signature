using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{
    public Button KeyBoardBtn;
    public Button HandWriteBtn;
    public BrushManager brushManager;
    public GameObject KeyBoardGo;
    public GameObject HandWriteGo;

    private void Awake()
    {
        KeyBoardBtn.onClick.AddListener(() =>
        {
            KeyBoardGo.SetActive(true);
            HandWriteGo.SetActive(false);
        });

        HandWriteBtn.onClick.AddListener(() =>
        {
            HandWriteGo.SetActive(true);
            KeyBoardGo.SetActive(false);
        });
    }

    private void OnEnable()
    {
        brushManager.OnClickClear();
        KeyBoardGo.SetActive(true);
        HandWriteGo.SetActive(false);
    }
}
