using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 定时器脚本：每隔固定时间触发一次事件
/// 挂载到任意 GameObject 上，通过 Inspector 绑定要执行的方法
/// </summary>
public class RepeatTimer : MonoBehaviour
{
    [Header("时间设置")]
    [Tooltip("触发间隔（秒）")]
    public float interval = 1.0f;

    [Tooltip("是否在 Start 时自动启动")]
    public bool autoStart = true;

    [Tooltip("是否在启动时立即执行一次（不等待第一个间隔）")]
    public bool executeImmediately = false;

    [Header("事件")]
    [Tooltip("每次到达间隔时间时触发")]
    public UnityEvent onTick;

    private Coroutine timerCoroutine;
    private bool isPaused = false;

    void Start()
    {
        if (autoStart)
        {
            StartTimer();
        }
    }

    /// <summary>
    /// 启动定时器（如果已启动则先停止再重新启动）
    /// </summary>
    public void StartTimer()
    {
        if (ClientRoot.Instance)
            interval = ClientRoot.Instance.customSystemInfo.timeoutDuration;
        StopTimer();
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        isPaused = false;
    }

    /// <summary>
    /// 暂停定时器（恢复时继续计时，而不是重置）
    /// </summary>
    public void PauseTimer()
    {
        if (timerCoroutine != null)
        {
            isPaused = true;
        }
    }

    /// <summary>
    /// 恢复定时器
    /// </summary>
    public void ResumeTimer()
    {
        isPaused = false;
    }

    /// <summary>
    /// 切换暂停/恢复
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    /// <summary>
    /// 重置定时器（重新计时，不影响运行状态）
    /// </summary>
    public void ResetTimer()
    {
        if (timerCoroutine != null)
        {
            // 先停止再重新启动，以重置计时
            StopTimer();
            StartTimer();
        }
    }

    /// <summary>
    /// 修改间隔时间并重启（可选）
    /// </summary>
    public void SetInterval(float newInterval)
    {
        interval = newInterval;
        // 如果定时器正在运行，可以选择重启以应用新间隔
        if (timerCoroutine != null)
        {
            ResetTimer();
        }
    }

    private IEnumerator TimerCoroutine()
    {
        // 如果需要立即执行第一次
        if (executeImmediately)
        {
            onTick?.Invoke();
        }

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < interval)
            {
                // 如果暂停，不增加时间
                if (!isPaused)
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            // 触发事件
            onTick?.Invoke();

            // 继续循环（如果游戏对象被销毁，协程会自动停止）
        }
    }

    void OnDestroy()
    {
        // 停止协程，避免内存泄漏
        StopTimer();
    }
}