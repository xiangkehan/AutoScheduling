using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyPinyinLib = TinyPinyin.PinyinHelper;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 拼音辅助类 - 提供中文转拼音和拼音首字母提取功能
/// </summary>
public static class PinyinHelper
{
    // 拼音缓存，提升性能
    private static readonly Dictionary<string, string> _pinyinInitialsCache = new();
    private static readonly Dictionary<string, string> _fullPinyinCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// 获取中文字符串的拼音首字母
    /// </summary>
    /// <param name="chinese">中文字符串</param>
    /// <returns>拼音首字母字符串（小写），例如："张三" -> "zs"</returns>
    public static string GetPinyinInitials(string chinese)
    {
        if (string.IsNullOrWhiteSpace(chinese))
        {
            return string.Empty;
        }

        // 检查缓存
        lock (_cacheLock)
        {
            if (_pinyinInitialsCache.TryGetValue(chinese, out var cached))
            {
                return cached;
            }
        }

        try
        {
            var result = new StringBuilder();

            foreach (char c in chinese)
            {
                // 如果是中文字符，获取拼音首字母
                if (TinyPinyinLib.IsChinese(c))
                {
                    var pinyin = TinyPinyinLib.GetPinyin(c);
                    if (!string.IsNullOrEmpty(pinyin))
                    {
                        result.Append(char.ToLower(pinyin[0]));
                    }
                }
                // 如果是字母或数字，直接添加
                else if (char.IsLetterOrDigit(c))
                {
                    result.Append(char.ToLower(c));
                }
            }

            var initials = result.ToString();

            // 缓存结果
            lock (_cacheLock)
            {
                if (!_pinyinInitialsCache.ContainsKey(chinese))
                {
                    _pinyinInitialsCache[chinese] = initials;
                }
            }

            return initials;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PinyinHelper: 获取拼音首字母失败: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取中文字符串的完整拼音（无空格连续）
    /// </summary>
    /// <param name="chinese">中文字符串</param>
    /// <returns>完整拼音字符串（小写，无空格），例如："张三" -> "zhangsan"</returns>
    public static string GetFullPinyin(string chinese)
    {
        if (string.IsNullOrWhiteSpace(chinese))
        {
            return string.Empty;
        }

        // 检查缓存
        lock (_cacheLock)
        {
            if (_fullPinyinCache.TryGetValue(chinese, out var cached))
            {
                return cached;
            }
        }

        try
        {
            var result = new StringBuilder();

            foreach (char c in chinese)
            {
                // 如果是中文字符，获取完整拼音
                if (TinyPinyinLib.IsChinese(c))
                {
                    var pinyin = TinyPinyinLib.GetPinyin(c);
                    if (!string.IsNullOrEmpty(pinyin))
                    {
                        result.Append(pinyin.ToLower());
                    }
                }
                // 如果是字母或数字，直接添加
                else if (char.IsLetterOrDigit(c))
                {
                    result.Append(char.ToLower(c));
                }
            }

            var fullPinyin = result.ToString();

            // 缓存结果
            lock (_cacheLock)
            {
                if (!_fullPinyinCache.ContainsKey(chinese))
                {
                    _fullPinyinCache[chinese] = fullPinyin;
                }
            }

            return fullPinyin;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PinyinHelper: 获取完整拼音失败: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取中文字符串的完整拼音（带空格分隔）
    /// </summary>
    /// <param name="chinese">中文字符串</param>
    /// <returns>完整拼音字符串（小写，空格分隔），例如："张三" -> "zhang san"</returns>
    public static string GetFullPinyinWithSeparator(string chinese)
    {
        if (string.IsNullOrWhiteSpace(chinese))
        {
            return string.Empty;
        }

        try
        {
            var result = new StringBuilder();

            foreach (char c in chinese)
            {
                // 如果是中文字符，获取完整拼音
                if (TinyPinyinLib.IsChinese(c))
                {
                    var pinyin = TinyPinyinLib.GetPinyin(c);
                    if (!string.IsNullOrEmpty(pinyin))
                    {
                        if (result.Length > 0)
                        {
                            result.Append(' ');
                        }
                        result.Append(pinyin.ToLower());
                    }
                }
                // 如果是字母或数字，直接添加
                else if (char.IsLetterOrDigit(c))
                {
                    if (result.Length > 0 && result[result.Length - 1] != ' ')
                    {
                        result.Append(' ');
                    }
                    result.Append(char.ToLower(c));
                }
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PinyinHelper: 获取完整拼音（带分隔符）失败: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 判断字符是否为中文字符
    /// </summary>
    /// <param name="c">要判断的字符</param>
    /// <returns>是否为中文字符</returns>
    public static bool IsChinese(char c)
    {
        return TinyPinyinLib.IsChinese(c);
    }

    /// <summary>
    /// 清空拼音缓存
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _pinyinInitialsCache.Clear();
            _fullPinyinCache.Clear();
            System.Diagnostics.Debug.WriteLine("PinyinHelper: 缓存已清空");
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存项数量</returns>
    public static (int InitialsCount, int FullPinyinCount) GetCacheStats()
    {
        lock (_cacheLock)
        {
            return (_pinyinInitialsCache.Count, _fullPinyinCache.Count);
        }
    }
}
