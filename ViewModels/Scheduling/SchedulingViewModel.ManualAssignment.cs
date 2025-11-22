using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.ViewModels.DataManagement;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的手动指定管理部分
    /// 负责手动指定的 CRUD 操作、表单管理和验证
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 创建手动指定方法

        /// <summary>
        /// 开始创建手动指定
        /// </summary>
        private void StartCreateManualAssignment()
        {
            NewManualAssignment = new CreateManualAssignmentDto
            {
                Date = StartDate.DateTime.Date,
                IsEnabled = true
            };
            
            // 通知所有包装属性
            OnPropertyChanged(nameof(NewManualAssignmentDate));
            OnPropertyChanged(nameof(NewManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(NewManualAssignmentPositionId));
            OnPropertyChanged(nameof(NewManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(NewManualAssignmentRemarks));
            
            IsCreatingManualAssignment = true;
        }

        /// <summary>
        /// 提交创建手动指定
        /// </summary>
        private async Task SubmitCreateManualAssignmentAsync()
        {
            if (NewManualAssignment == null)
                return;

            // 验证表单
            if (!ValidateManualAssignment(NewManualAssignment, out _))
            {
                // 验证错误已经设置到各个字段的错误属性中，不需要显示对话框
                return;
            }

            // 获取人员和哨位名称
            var personnel = SelectedPersonnels.FirstOrDefault(p => p.Id == NewManualAssignment.PersonnelId);
            var position = SelectedPositions.FirstOrDefault(p => p.Id == NewManualAssignment.PositionId);

            if (personnel == null || position == null)
            {
                await _dialogService.ShowWarningAsync("选择的人员或哨位不存在");
                return;
            }

            // 添加到临时列表
            _manualAssignmentManager.AddTemporary(
                NewManualAssignment,
                personnel.Name,
                position.Name
            );

            // 清空验证错误
            ClearValidationErrors();

            // 先清空 DTO
            NewManualAssignment = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(NewManualAssignmentDate));
            OnPropertyChanged(nameof(NewManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(NewManualAssignmentPositionId));
            OnPropertyChanged(nameof(NewManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(NewManualAssignmentRemarks));

            // 关闭表单
            IsCreatingManualAssignment = false;

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }

        /// <summary>
        /// 取消创建手动指定
        /// </summary>
        private void CancelCreateManualAssignment()
        {
            // 先清空 DTO，避免对话框关闭时绑定更新导致 null 引用
            NewManualAssignment = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(NewManualAssignmentDate));
            OnPropertyChanged(nameof(NewManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(NewManualAssignmentPositionId));
            OnPropertyChanged(nameof(NewManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(NewManualAssignmentRemarks));
            
            // 最后关闭对话框
            IsCreatingManualAssignment = false;
        }

        #endregion

        #region 编辑手动指定方法

        /// <summary>
        /// 开始编辑手动指定
        /// </summary>
        private void StartEditManualAssignment(ManualAssignmentViewModel? assignment)
        {
            if (assignment == null || !assignment.IsTemporary)
                return;

            EditingManualAssignment = assignment;
            EditingManualAssignmentDto = new UpdateManualAssignmentDto
            {
                Date = assignment.Date,
                PersonnelId = assignment.PersonnelId,
                PositionId = assignment.PositionId,
                TimeSlot = assignment.TimeSlot,
                Remarks = assignment.Remarks,
                IsEnabled = assignment.IsEnabled
            };
            
            // 通知所有包装属性
            OnPropertyChanged(nameof(EditingManualAssignmentDate));
            OnPropertyChanged(nameof(EditingManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(EditingManualAssignmentPositionId));
            OnPropertyChanged(nameof(EditingManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(EditingManualAssignmentRemarks));
            
            IsEditingManualAssignment = true;
        }

        /// <summary>
        /// 提交编辑手动指定
        /// </summary>
        private async Task SubmitEditManualAssignmentAsync()
        {
            if (EditingManualAssignment == null || EditingManualAssignmentDto == null)
                return;

            // 验证表单
            if (!ValidateManualAssignment(EditingManualAssignmentDto, out _))
            {
                // 验证错误已经设置到各个字段的错误属性中，不需要显示对话框
                return;
            }

            // 获取人员和哨位名称
            var personnel = SelectedPersonnels.FirstOrDefault(p => p.Id == EditingManualAssignmentDto.PersonnelId);
            var position = SelectedPositions.FirstOrDefault(p => p.Id == EditingManualAssignmentDto.PositionId);

            if (personnel == null || position == null)
            {
                await _dialogService.ShowWarningAsync("选择的人员或哨位不存在");
                return;
            }

            // 更新临时列表
            _manualAssignmentManager.UpdateTemporary(
                EditingManualAssignment.TempId,
                EditingManualAssignmentDto,
                personnel.Name,
                position.Name
            );

            // 清空验证错误
            ClearValidationErrors();

            // 先清空 DTO
            EditingManualAssignment = null;
            EditingManualAssignmentDto = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(EditingManualAssignmentDate));
            OnPropertyChanged(nameof(EditingManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(EditingManualAssignmentPositionId));
            OnPropertyChanged(nameof(EditingManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(EditingManualAssignmentRemarks));

            // 关闭表单
            IsEditingManualAssignment = false;

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }

        /// <summary>
        /// 取消编辑手动指定
        /// </summary>
        private void CancelEditManualAssignment()
        {
            // 先清空 DTO，避免对话框关闭时绑定更新导致 null 引用
            EditingManualAssignment = null;
            EditingManualAssignmentDto = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(EditingManualAssignmentDate));
            OnPropertyChanged(nameof(EditingManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(EditingManualAssignmentPositionId));
            OnPropertyChanged(nameof(EditingManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(EditingManualAssignmentRemarks));
            
            // 最后关闭对话框
            IsEditingManualAssignment = false;
        }

        #endregion

        #region 删除手动指定方法

        /// <summary>
        /// 删除手动指定
        /// </summary>
        private async Task DeleteManualAssignmentAsync(ManualAssignmentViewModel? assignment)
        {
            if (assignment == null || !assignment.IsTemporary)
                return;

            // 显示确认对话框
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "确认删除",
                Content = "确定要删除此手动指定吗？",
                PrimaryButtonText = "删除",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Secondary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return;

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                return;

            // 删除临时手动指定
            _manualAssignmentManager.RemoveTemporary(assignment.TempId);

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证手动指定表单
        /// </summary>
        private bool ValidateManualAssignment(CreateManualAssignmentDto dto, [NotNullWhen(false)] out string? error)
        {
            // 清空之前的错误
            ClearValidationErrors();

            bool isValid = true;

            // 验证日期范围
            if (dto.Date < StartDate.Date || dto.Date > EndDate.Date)
            {
                DateValidationError = "日期必须在排班开始日期和结束日期之间";
                isValid = false;
            }

            // 验证人员
            if (dto.PersonnelId <= 0)
            {
                PersonnelValidationError = "请选择人员";
                isValid = false;
            }
            else if (!SelectedPersonnels.Any(p => p.Id == dto.PersonnelId))
            {
                PersonnelValidationError = "选择的人员不在已选人员列表中";
                isValid = false;
            }

            // 验证哨位
            if (dto.PositionId <= 0)
            {
                PositionValidationError = "请选择哨位";
                isValid = false;
            }
            else if (!SelectedPositions.Any(p => p.Id == dto.PositionId))
            {
                PositionValidationError = "选择的哨位不在已选哨位列表中";
                isValid = false;
            }

            // 验证时段
            if (dto.TimeSlot < 0 || dto.TimeSlot > 11)
            {
                TimeSlotValidationError = "请选择时段";
                isValid = false;
            }

            error = isValid ? null : "请修正表单中的错误";
            return isValid;
        }

        /// <summary>
        /// 验证手动指定表单（UpdateDto版本）
        /// </summary>
        private bool ValidateManualAssignment(UpdateManualAssignmentDto dto, [NotNullWhen(false)] out string? error)
        {
            // 清空之前的错误
            ClearValidationErrors();

            bool isValid = true;

            // 验证日期范围
            if (dto.Date < StartDate.Date || dto.Date > EndDate.Date)
            {
                DateValidationError = "日期必须在排班开始日期和结束日期之间";
                isValid = false;
            }

            // 验证人员
            if (dto.PersonnelId <= 0)
            {
                PersonnelValidationError = "请选择人员";
                isValid = false;
            }
            else if (!SelectedPersonnels.Any(p => p.Id == dto.PersonnelId))
            {
                PersonnelValidationError = "选择的人员不在已选人员列表中";
                isValid = false;
            }

            // 验证哨位
            if (dto.PositionId <= 0)
            {
                PositionValidationError = "请选择哨位";
                isValid = false;
            }
            else if (!SelectedPositions.Any(p => p.Id == dto.PositionId))
            {
                PositionValidationError = "选择的哨位不在已选哨位列表中";
                isValid = false;
            }

            // 验证时段
            if (dto.TimeSlot < 0 || dto.TimeSlot > 11)
            {
                TimeSlotValidationError = "请选择时段";
                isValid = false;
            }

            error = isValid ? null : "请修正表单中的错误";
            return isValid;
        }

        /// <summary>
        /// 清空验证错误消息
        /// </summary>
        private void ClearValidationErrors()
        {
            DateValidationError = string.Empty;
            PersonnelValidationError = string.Empty;
            PositionValidationError = string.Empty;
            TimeSlotValidationError = string.Empty;
        }

        #endregion
    }
}
