using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的手动指定管理部分
    /// 包含手动指定的 CRUD 操作、表单管理、验证逻辑
    /// </summary>
    public partial class SchedulingViewModel
    {
        // 此文件将包含：
        // - 创建手动指定 (StartCreateManualAssignment, SubmitCreateManualAssignmentAsync, CancelCreateManualAssignment)
        // - 编辑手动指定 (StartEditManualAssignment, SubmitEditManualAssignmentAsync, CancelEditManualAssignment)
        // - 删除手动指定 (DeleteManualAssignmentAsync)
        // - 表单验证 (ValidateManualAssignment - 两个重载版本)
        // - 验证错误管理 (ClearValidationErrors)
    }
}
