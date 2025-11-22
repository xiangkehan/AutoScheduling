using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的哨位人员管理部分
    /// 包含哨位人员的添加、移除、保存、手动添加参与人员等逻辑
    /// </summary>
    public partial class SchedulingViewModel
    {
        // 此文件将包含：
        // - 为哨位添加人员 (StartAddPersonnelToPosition, SubmitAddPersonnelToPositionAsync, CancelAddPersonnelToPosition)
        // - 临时移除人员 (RemovePersonnelFromPosition)
        // - 撤销更改 (RevertPositionChanges)
        // - 保存为永久 (SavePositionChangesAsync, ShowSaveConfirmationDialog)
        // - 手动添加参与人员 (StartManualAddPersonnel, SubmitManualAddPersonnelAsync, CancelManualAddPersonnel, RemoveManualPersonnel)
        // - 人员提取和视图模型更新 (ExtractPersonnelFromPositions, UpdatePositionPersonnelViewModels)
    }
}
