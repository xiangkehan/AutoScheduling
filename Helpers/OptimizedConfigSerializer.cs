using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 优化的配置序列化器
/// 性能优化：使用高性能 JsonSerializerOptions
/// </summary>
public static class OptimizedConfigSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false, // 减小体积
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // 忽略null值
        Converters = { new JsonStringEnumConverter() }, // 枚举转换
        PropertyNameCaseInsensitive = true // 不区分大小写
    };

    /// <summary>
    /// 序列化对象为JSON字符串
    /// </summary>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    /// <summary>
    /// 从JSON字符串反序列化对象
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, _options);
    }

    /// <summary>
    /// 尝试反序列化，失败时返回默认值
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = Deserialize<T>(json);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}
