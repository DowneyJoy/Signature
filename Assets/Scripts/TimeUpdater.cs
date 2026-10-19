using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 时间显示控制器：每秒刷新一次当前时间（格式：HH:mm）
/// </summary>
public class TimeUpdater : MonoBehaviour
{
    [Header("UI 组件")]
    public Text ClockTimer;               // 用于显示时间的 Text 组件
    public Text MonthTimer;

    [Header("刷新设置")]
    public bool startOnEnable = true;   // 启用时自动开始刷新

    private void Awake()
    {
        MonthTimer.text = DateTimeUtility.GetCurrentDate();
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartUpdating();
    }

    private void OnDisable()
    {
        CancelInvoke();                 // 停止所有 Invoke 调用
    }

    /// <summary>
    /// 开始每秒更新时间
    /// </summary>
    public void StartUpdating()
    {
        CancelInvoke();                 // 避免重复调用
        InvokeRepeating(nameof(UpdateTime), 0f, 1f); // 立即执行，之后每秒一次
    }

    /// <summary>
    /// 停止更新时间
    /// </summary>
    public void StopUpdating()
    {
        CancelInvoke();
    }

    /// <summary>
    /// 手动刷新一次当前时间
    /// </summary>
    public void UpdateTime()
    {
        if (ClockTimer != null)
        {
            //Debug.Log(DateTimeUtility.GetCurrentTime());
            ClockTimer.text = DateTimeUtility.GetCurrentTime(); // 使用之前定义的工具类
        }
        else
            Debug.LogWarning("TimeUpdater: timeText 未赋值！");
    }
}