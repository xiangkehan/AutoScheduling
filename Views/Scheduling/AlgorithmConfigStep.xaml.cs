using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 算法配置步骤用户控件
/// </summary>
public sealed partial class AlgorithmConfigStep : UserControl
{
    public AlgorithmConfigViewModel ViewModel { get; }

    public AlgorithmConfigStep()
    {
        // 从服务定位器获取ViewModel
        ViewModel = Helpers.ServiceLocator.GetService<AlgorithmConfigViewModel>();
        this.InitializeComponent();
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    /// <returns>是否验证通过</returns>
    public bool Validate()
    {
        return ViewModel.ValidateAll();
    }
}
