using AutoScheduling3.DTOs.ImportExport;
using System.Text.Json;
using Windows.Storage;

namespace AutoScheduling3.TestData.Export;

/// <summary>
/// 测试数据导出器
/// 负责将生成的测试数据导出到文件或字符串
/// </summary>
public class TestDataExporter
{
    /// <summary>
    /// 导出数据到文件路径（传统方式）
    /// </summary>
    /// <param name="data">要导出的数据</param>
    /// <param name="filePath">文件路径</param>
    /// <exception cref="ArgumentNullException">当data或filePath为null时抛出</exception>
    public async Task ExportToFileAsync(ExportData data, string filePath)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var json = ExportToJson(data);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 导出数据到StorageFile（WinUI3方式）
    /// </summary>
    /// <param name="data">要导出的数据</param>
    /// <param name="file">StorageFile对象</param>
    /// <exception cref="ArgumentNullException">当data或file为null时抛出</exception>
    public async Task ExportToStorageFileAsync(ExportData data, StorageFile file)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (file == null)
            throw new ArgumentNullException(nameof(file));

        var json = ExportToJson(data);
        await FileIO.WriteTextAsync(file, json);
    }

    /// <summary>
    /// 导出数据为JSON字符串
    /// </summary>
    /// <param name="data">要导出的数据</param>
    /// <returns>JSON格式的字符串</returns>
    /// <exception cref="ArgumentNullException">当data为null时抛出</exception>
    public string ExportToJson(ExportData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var options = GetJsonOptions();
        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// 获取JSON序列化选项
    /// </summary>
    /// <returns>配置好的JsonSerializerOptions</returns>
    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
    }
}
