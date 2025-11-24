using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models.Constraints;
using System.Collections.Generic;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的属性定义部分
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 步骤和基本信息属性

        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private string _scheduleTitle = string.Empty;

        [ObservableProperty]
        private DateTimeOffset _startDate = DateTimeOffset.Now;

        [ObservableProperty]
        private DateTimeOffset _endDate = DateTimeOffset.Now.AddDays(6);

        #endregion

        #region 数据集合属性

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnels = new();

        [ObservableProperty]
        private ObservableCollection<PositionDto> _availablePositions = new();

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _selectedPersonnels = new();

        [ObservableProperty]
        private ObservableCollection<PositionDto> _selectedPositions = new();

        #endregion

        #region 约束相关属性

        [ObservableProperty] 
        private List<int> _enabledFixedRules = new();

        [ObservableProperty] 
        private List<int> _enabledManualAssignments = new();

        [ObservableProperty] 
        private bool _useActiveHolidayConfig = true;

        [ObservableProperty] 
        private int? _selectedHolidayConfigId;

        // 约束数据（用于显示和使用，绑定到 UI）
        [ObservableProperty] 
        private ObservableCollection<HolidayConfig> _holidayConfigs = new();

        [ObservableProperty] 
        private ObservableCollection<FixedPositionRule> _fixedPositionRules = new();

        [ObservableProperty] 
        private ObservableCollection<ManualAssignment> _manualAssignments = new();

        #endregion

        #region 状态属性

        [ObservableProperty]
        private bool _isExecuting;

        [ObservableProperty]
        private bool _isLoadingInitial;

        [ObservableProperty]
        private bool _isLoadingConstraints; // 用于显示约束加载 UI 状态

        #endregion

        #region 手动指定表单属性

        // 表单状态属性
        [ObservableProperty]
        private bool _isCreatingManualAssignment;

        [ObservableProperty]
        private bool _isEditingManualAssignment;

        // 表单数据属性
        [ObservableProperty]
        private CreateManualAssignmentDto? _newManualAssignment;

        [ObservableProperty]
        private ManualAssignmentViewModel? _editingManualAssignment;

        [ObservableProperty]
        private UpdateManualAssignmentDto? _editingManualAssignmentDto;

        // 包装属性（用于表单绑定，处理 null 情况）
        public DateTimeOffset? NewManualAssignmentDate
        {
            get => NewManualAssignment != null ? new DateTimeOffset(NewManualAssignment.Date) : null;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.Date != value.Value.DateTime)
                {
                    NewManualAssignment.Date = value.Value.DateTime;
                    OnPropertyChanged();
                }
            }
        }

        public int? NewManualAssignmentPersonnelId
        {
            get => NewManualAssignment?.PersonnelId;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.PersonnelId != value.Value)
                {
                    NewManualAssignment.PersonnelId = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public int? NewManualAssignmentPositionId
        {
            get => NewManualAssignment?.PositionId;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.PositionId != value.Value)
                {
                    NewManualAssignment.PositionId = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public int? NewManualAssignmentTimeSlot
        {
            get => NewManualAssignment?.TimeSlot;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.TimeSlot != value.Value)
                {
                    NewManualAssignment.TimeSlot = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewManualAssignmentRemarks
        {
            get => NewManualAssignment?.Remarks ?? string.Empty;
            set
            {
                if (NewManualAssignment != null && NewManualAssignment.Remarks != value)
                {
                    NewManualAssignment.Remarks = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTimeOffset? EditingManualAssignmentDate
        {
            get => EditingManualAssignmentDto != null ? new DateTimeOffset(EditingManualAssignmentDto.Date) : null;
            set
            {
                if (EditingManualAssignmentDto != null && value.HasValue && EditingManualAssignmentDto.Date != value.Value.DateTime)
                {
                    EditingManualAssignmentDto.Date = value.Value.DateTime;
                    OnPropertyChanged();
                }
            }
        }

        public int EditingManualAssignmentPersonnelId
        {
            get => EditingManualAssignmentDto?.PersonnelId ?? 0;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.PersonnelId != value)
                {
                    EditingManualAssignmentDto.PersonnelId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EditingManualAssignmentPositionId
        {
            get => EditingManualAssignmentDto?.PositionId ?? 0;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.PositionId != value)
                {
                    EditingManualAssignmentDto.PositionId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EditingManualAssignmentTimeSlot
        {
            get => EditingManualAssignmentDto?.TimeSlot ?? 0;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.TimeSlot != value)
                {
                    EditingManualAssignmentDto.TimeSlot = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EditingManualAssignmentRemarks
        {
            get => EditingManualAssignmentDto?.Remarks ?? string.Empty;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.Remarks != value)
                {
                    EditingManualAssignmentDto.Remarks = value;
                    OnPropertyChanged();
                }
            }
        }

        // 验证错误属性
        [ObservableProperty]
        private string _dateValidationError = string.Empty;

        [ObservableProperty]
        private string _personnelValidationError = string.Empty;

        [ObservableProperty]
        private string _positionValidationError = string.Empty;

        [ObservableProperty]
        private string _timeSlotValidationError = string.Empty;

        #endregion

        #region 哨位人员管理属性

        // 哨位人员相关属性
        [ObservableProperty]
        private bool _isAddingPersonnelToPosition;

        [ObservableProperty]
        private PositionDto? _currentEditingPosition;

        // 可添加到当前哨位的人员列表
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnelForPosition = new();

        // 选中的要添加到哨位的人员ID列表
        [ObservableProperty]
        private ObservableCollection<int> _selectedPersonnelIdsForPosition = new();

        // 手动添加参与人员相关属性
        [ObservableProperty]
        private bool _isManualAddingPersonnel;

        // 可手动添加的人员列表（不在任何哨位可用人员列表中的人员）
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnelForManualAdd = new();

        // 选中的要手动添加的人员ID列表
        [ObservableProperty]
        private ObservableCollection<int> _selectedPersonnelIdsForManualAdd = new();

        // 手动添加的人员ID列表（不在任何哨位的可用人员列表中）
        [ObservableProperty]
        private ObservableCollection<int> _manuallyAddedPersonnelIds = new();

        // 手动添加的人员详细信息（用于UI显示）
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _manuallyAddedPersonnelDetails = new();

        // 自动提取的人员数量
        [ObservableProperty]
        private int _autoExtractedPersonnelCount;

        // 手动添加的人员数量
        [ObservableProperty]
        private int _manuallyAddedPersonnelCount;

        // 哨位人员视图模型集合（用于步骤3的UI展示）
        [ObservableProperty]
        private ObservableCollection<PositionPersonnelViewModel> _positionPersonnelViewModels = new();

        #endregion

        #region 模板属性

        // 模板相关属性（用于标识是否加载了模板，用于逻辑判断和统一处理）
        [ObservableProperty] 
        private int? _loadedTemplateId;

        [ObservableProperty] 
        private bool _templateApplied;

        #endregion

        #region 结果属性

        [ObservableProperty]
        private ScheduleDto? _resultSchedule;

        // 步骤5摘要展示
        [ObservableProperty] 
        private ObservableCollection<SummarySection> _summarySections = new();

        #endregion

        #region 属性变更回调方法

        // 属性变化回调
        partial void OnCurrentStepChanged(int value)
        {
            RefreshCommandStates();
            
            // 步骤3：自动提取人员
            if (value == 3)
            {
                ExtractPersonnelFromPositions();
            }
            else if (value == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
            {
                _ = LoadConstraintsAsync();
            }
            else if (value == 5)
            {
                BuildSummarySections();
            }
        }

        partial void OnScheduleTitleChanged(string value) => RefreshCommandStates();

        partial void OnStartDateChanged(DateTimeOffset value)
        {
            if (EndDate < value) EndDate = value;
            RefreshCommandStates();
            if (CurrentStep >= 4)
                _ = LoadConstraintsAsync();
            if (CurrentStep == 5) BuildSummarySections();
        }

        partial void OnEndDateChanged(DateTimeOffset value)
        {
            if (value < StartDate) value = StartDate;
            RefreshCommandStates();
            if (CurrentStep >= 4)
                _ = LoadConstraintsAsync();
            if (CurrentStep == 5) BuildSummarySections();
        }

        partial void OnSelectedPersonnelsChanged(ObservableCollection<PersonnelDto> value)
        {
            if (value != null)
            {
                value.CollectionChanged += (s, e) => RefreshCommandStates();
            }
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }

        partial void OnSelectedPositionsChanged(ObservableCollection<PositionDto> value)
        {
            if (value != null)
            {
                value.CollectionChanged += (s, e) =>
                {
                    RefreshCommandStates();
                    
                    // 如果在步骤3，重新提取人员
                    if (CurrentStep == 3)
                    {
                        ExtractPersonnelFromPositions();
                    }
                };
            }
            RefreshCommandStates();
            
            // 如果在步骤3，重新提取人员
            if (CurrentStep == 3)
            {
                ExtractPersonnelFromPositions();
            }
            
            if (CurrentStep == 5) BuildSummarySections();
        }

        partial void OnUseActiveHolidayConfigChanged(bool value)
        {
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }

        partial void OnSelectedHolidayConfigIdChanged(int? value)
        {
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }

        #endregion
    }
}
