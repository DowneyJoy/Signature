using System;

/// <summary>
/// 日期时间工具类
/// </summary>
public static class DateTimeUtility
{
    /// <summary>
    /// 获取当前日期，格式：yyyy.MM.dd
    /// </summary>
    /// <returns>例如：2026.05.13</returns>
    public static string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy.MM.dd");
    }

    /// <summary>
    /// 获取当前时间（24小时制），格式：HH:mm:ss
    /// </summary>
    /// <returns>例如：09:34</returns>
    public static string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }

    /// <summary>
    /// 获取当前日期和时间，格式：yyyy.MM.dd HH:mm
    /// </summary>
    /// <returns>例如：2026.05.13 09:34</returns>
    public static string GetCurrentDateTime()
    {
        return DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
    }

    /// <summary>
    /// 根据指定格式获取当前时间
    /// </summary>
    /// <param name="format">自定义格式字符串</param>
    /// <returns>格式化后的日期时间字符串</returns>
    public static string GetCurrentDateTime(string format)
    {
        return DateTime.Now.ToString(format);
    }
}