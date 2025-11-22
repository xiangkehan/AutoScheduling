using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的命令定义部分
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 核心命令

        /// <summary>
        /// 加载初始数据命令
        /// </summary>
        public IAsyncRelayCommand LoadDataCommand { get; }

        /// <summary>
        /// 下一步命令
        /// </summary>
        public IRelayCommand NextStepCommand { get; }

        /// <summary>
        /// 上一步命令
        /// </summary>
        public IRelayCommand PreviousStepCommand { get; }

        /// <summary>
        /// 执行排班命令
        /// </summary>
        public IAsyncRelayCommand ExecuteSchedulingCommand { get; }

        /// <summary>
        /// 取消向导命令
        /// </summary>
        public IRelayCommand CancelCommand { get; }

        #endregion

        #region 模板和约束命令

        /// <summary>
        /// 加载模板命令
        /// </summary>
        public IAsyncRelayCommand<int> LoadTemplateCommand { get; }

        /// <summary>
        /// 加载约束数据命令
        /// </summary>
        public IAsyncRelayCommand LoadConstraintsCommand { get; }

        /// <summary>
        /// 保存为模板命令
        /// </summary>
        public IAsyncRelayCommand SaveAsTemplateCommand { get; }

        #endregion

        #region 手动指定命令

        /// <summary>
        /// 开始创建手动指定命令
        /// </summary>
        public IRelayCommand StartCreateManualAssignmentCommand { get; }

        /// <summary>
        /// 提交创建手动指定命令
        /// </summary>
        public IAsyncRelayCommand SubmitCreateManualAssignmentCommand { get; }

        /// <summary>
        /// 取消创建手动指定命令
        /// </summary>
        public IRelayCommand CancelCreateManualAssignmentCommand { get; }

        /// <summary>
        /// 开始编辑手动指定命令
        /// </summary>
        public IRelayCommand<ManualAssignmentViewModel> StartEditManualAssignmentCommand { get; }

        /// <summary>
        /// 提交编辑手动指定命令
        /// </summary>
        public IAsyncRelayCommand SubmitEditManualAssignmentCommand { get; }

        /// <summary>
        /// 取消编辑手动指定命令
        /// </summary>
        public IRelayCommand CancelEditManualAssignmentCommand { get; }

        /// <summary>
        /// 删除手动指定命令
        /// </summary>
        public IAsyncRelayCommand<ManualAssignmentViewModel> DeleteManualAssignmentCommand { get; }

        #endregion

        #region 哨位人员管理命令

        /// <summary>
        /// 开始为哨位添加人员命令
        /// </summary>
        public IRelayCommand<PositionDto> StartAddPersonnelToPositionCommand { get; }

        /// <summary>
        /// 提交为哨位添加人员命令
        /// </summary>
        public IAsyncRelayCommand SubmitAddPersonnelToPositionCommand { get; }

        /// <summary>
        /// 取消为哨位添加人员命令
        /// </summary>
        public IRelayCommand CancelAddPersonnelToPositionCommand { get; }

        /// <summary>
        /// 从哨位移除人员命令
        /// </summary>
        public IRelayCommand<(int positionId, int personnelId)> RemovePersonnelFromPositionCommand { get; }

        /// <summary>
        /// 撤销哨位更改命令
        /// </summary>
        public IRelayCommand<int> RevertPositionChangesCommand { get; }

        /// <summary>
        /// 保存哨位更改为永久命令
        /// </summary>
        public IAsyncRelayCommand<int> SavePositionChangesCommand { get; }

        #endregion

        #region 手动添加参与人员命令

        /// <summary>
        /// 开始手动添加参与人员命令（不属于任何哨位）
        /// </summary>
        public IRelayCommand StartManualAddPersonnelCommand { get; }

        /// <summary>
        /// 提交手动添加参与人员命令
        /// </summary>
        public IAsyncRelayCommand SubmitManualAddPersonnelCommand { get; }

        /// <summary>
        /// 取消手动添加参与人员命令
        /// </summary>
        public IRelayCommand CancelManualAddPersonnelCommand { get; }

        /// <summary>
        /// 移除手动添加的人员命令
        /// </summary>
        public IRelayCommand<int> RemoveManualPersonnelCommand { get; }

        #endregion
    }
}
