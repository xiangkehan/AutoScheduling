using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.DTOs.ImportExport;
using System;

namespace AutoScheduling3.Views.Settings;

/// <summary>
/// 导入导出进度对话框
/// </summary>
public sealed partial class ImportExportProgressDialog : ContentDialog
{
    public ImportExportProgressDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 更新导出进度
    /// </summary>
    public void UpdateExportProgress(ExportProgress progress)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            CurrentOperationText.Text = $"正在导出: {progress.CurrentTable}";
            RecordCountText.Text = $"{progress.ProcessedRecords} / {progress.TotalRecords} 条记录";
            PercentText.Text = $"{progress.PercentComplete:F0}%";
            ProgressBar.Value = progress.PercentComplete;
        });
    }

    /// <summary>
    /// 更新导入进度
    /// </summary>
    public void UpdateImportProgress(ImportProgress progress)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var operation = progress.CurrentOperation switch
            {
                "Validating" => "验证数据",
                "Importing" => "导入数据",
                "Verifying" => "验证完整性",
                _ => progress.CurrentOperation
            };

            CurrentOperationText.Text = $"{operation}: {progress.CurrentTable}";
            RecordCountText.Text = $"{progress.ProcessedRecords} / {progress.TotalRecords} 条记录";
            PercentText.Text = $"{progress.PercentComplete:F0}%";
            ProgressBar.Value = progress.PercentComplete;
        });
    }

    /// <summary>
    /// 设置完成状态
    /// </summary>
    public void SetCompleted()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            CurrentOperationText.Text = "操作完成";
            ProgressBar.Value = 100;
            PercentText.Text = "100%";
        });
    }
}
