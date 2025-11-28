using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 导出格式选择对话框
/// </summary>
public sealed partial class ExportFormatDialog : ContentDialog
{
    /// <summary>
    /// 选中的导出格式
    /// </summary>
    public ExportFormat SelectedFormat { get; private set; } = ExportFormat.Excel;

    /// <summary>
    /// 导出选项
    /// </summary>
    public ExportOptions Options { get; private set; } = new();

    public ExportFormatDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 对话框打开时的处理
    /// </summary>
    private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        // 设置无障碍属性
        SetAccessibilityProperties();

        // 默认选中 Excel
        ExcelRadioButton.IsChecked = true;
    }

    /// <summary>
    /// 设置无障碍属性
    /// </summary>
    private void SetAccessibilityProperties()
    {
        ExcelRadioButton.SetValue(AutomationProperties.NameProperty, "导出为 Excel 格式");
        ExcelRadioButton.SetValue(AutomationProperties.HelpTextProperty, "导出为 Excel 文件，包含表格数据、统计信息和格式化样式");

        CsvRadioButton.SetValue(AutomationProperties.NameProperty, "导出为 CSV 格式");
        CsvRadioButton.SetValue(AutomationProperties.HelpTextProperty, "导出为 CSV 文件，纯文本格式，适合导入其他系统");

        PdfRadioButton.SetValue(AutomationProperties.NameProperty, "导出为 PDF 格式");
        PdfRadioButton.SetValue(AutomationProperties.HelpTextProperty, "导出为 PDF 文件，包含表格数据、统计信息和页眉页脚");

        IncludeEmptyCellsCheckBox.SetValue(AutomationProperties.NameProperty, "包含未分配的单元格");
        HighlightConflictsCheckBox.SetValue(AutomationProperties.NameProperty, "高亮显示冲突的单元格");
        IncludeStatisticsCheckBox.SetValue(AutomationProperties.NameProperty, "包含统计信息");
        IncludeScoresCheckBox.SetValue(AutomationProperties.NameProperty, "包含软约束评分");
    }

    /// <summary>
    /// 格式单选按钮选中变化
    /// </summary>
    private void FormatRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        // 如果控件还未初始化完成，直接返回
        if (PdfNoticeBar == null)
            return;

        // 更新选中的格式
        if (ExcelRadioButton.IsChecked == true)
        {
            SelectedFormat = ExportFormat.Excel;
            PdfNoticeBar.IsOpen = false;
        }
        else if (CsvRadioButton.IsChecked == true)
        {
            SelectedFormat = ExportFormat.Csv;
            PdfNoticeBar.IsOpen = false;
        }
        else if (PdfRadioButton.IsChecked == true)
        {
            SelectedFormat = ExportFormat.Pdf;
            PdfNoticeBar.IsOpen = true;
        }
    }

    /// <summary>
    /// 主按钮点击
    /// </summary>
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            // 收集导出选项
            Options = new ExportOptions
            {
                Format = SelectedFormat,
                IncludeEmptyCells = IncludeEmptyCellsCheckBox.IsChecked == true,
                HighlightConflicts = HighlightConflictsCheckBox.IsChecked == true,
                IncludeStatistics = IncludeStatisticsCheckBox.IsChecked == true,
                IncludeScores = IncludeScoresCheckBox.IsChecked == true
            };

            // PDF 格式暂不支持
            if (SelectedFormat == ExportFormat.Pdf)
            {
                ShowValidationError("PDF 导出功能正在开发中，请选择其他格式");
                args.Cancel = true;
                return;
            }
        }
        catch (System.Exception ex)
        {
            ShowValidationError($"操作失败: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// 显示验证错误
    /// </summary>
    private void ShowValidationError(string message)
    {
        ValidationErrorBar.Message = message;
        ValidationErrorBar.IsOpen = true;
    }

    /// <summary>
    /// Esc 键快捷键处理
    /// </summary>
    private void EscapeAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Hide();
    }
}

/// <summary>
/// 导出格式
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Excel 格式
    /// </summary>
    Excel,

    /// <summary>
    /// CSV 格式
    /// </summary>
    Csv,

    /// <summary>
    /// PDF 格式
    /// </summary>
    Pdf
}

/// <summary>
/// 导出选项
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// 导出格式
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Excel;

    /// <summary>
    /// 包含空单元格
    /// </summary>
    public bool IncludeEmptyCells { get; set; } = true;

    /// <summary>
    /// 高亮冲突
    /// </summary>
    public bool HighlightConflicts { get; set; } = true;

    /// <summary>
    /// 包含统计信息
    /// </summary>
    public bool IncludeStatistics { get; set; } = true;

    /// <summary>
    /// 包含约束评分
    /// </summary>
    public bool IncludeScores { get; set; } = false;
}
