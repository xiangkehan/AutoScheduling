using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 排班向导视图模型（主文件）
    /// 负责管理排班创建的完整流程，包括基本信息配置、人员选择、哨位选择、约束配置等
    /// 
    /// 本类使用部分类（Partial Class）拆分为多个文件，按功能模块组织：
    /// - SchedulingViewModel.cs: 主文件，包含依赖注入、构造函数、初始化逻辑
    /// - SchedulingViewModel.Properties.cs: 属性定义和属性变更回调
    /// - SchedulingViewModel.Commands.cs: 命令定义
    /// - SchedulingViewModel.Wizard.cs: 向导流程管理
    /// - SchedulingViewModel.ManualAssignment.cs: 手动指定管理
    /// - SchedulingViewModel.PositionPersonnel.cs: 哨位人员管理
    /// - SchedulingViewModel.TemplateConstraints.cs: 模板和约束管理
    /// - SchedulingViewModel.StateManagement.cs: 状态管理和草稿
    /// - SchedulingViewModel.Helpers.cs: 辅助方法和静态数据
    /// </summary>
    public partial class SchedulingViewModel : ObservableObject
    {
        #region 依赖服务

        private readonly ISchedulingService _schedulingService;
        private readonly IPersonnelService _personnelService;
        private readonly IPositionService _positionService;
        private readonly ITemplateService _templateService;
        private readonly ISchedulingDraftService? _draftService;
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigation_service;
        private Microsoft.UI.Xaml.Controls.ContentDialog? _progressDialog;

        #endregion

        #region 管理器

        // 手动指定管理器
        private readonly ManualAssignmentManager _manualAssignmentManager;

        // 哨位人员管理器
        private readonly PositionPersonnelManager _positionPersonnelManager;

        #endregion

        #region 缓存

        // 缓存：人员ID到PersonnelDto的映射
        private readonly Dictionary<int, PersonnelDto> _personnelCache = new();
        
        // 缓存：哨位ID到PositionDto的映射
        private readonly Dictionary<int, PositionDto> _positionCache = new();

        #endregion


        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public SchedulingViewModel(
            ISchedulingService schedulingService,
            IPersonnelService personnelService,
            IPositionService positionService,
            ITemplateService templateService,
            ISchedulingDraftService? draftService,
            DialogService dialogService,
            NavigationService navigationService)
        {
            // 依赖注入
            _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
            _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _draftService = draftService;
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigation_service = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // 初始化管理器
            _manualAssignmentManager = new ManualAssignmentManager();
            _positionPersonnelManager = new PositionPersonnelManager();

            // 初始化命令
            InitializeCommands();

            // 注册事件处理
            RegisterEventHandlers();
        }

        /// <summary>
        /// 初始化所有命令
        /// </summary>
        private void InitializeCommands()
        {
            // 核心命令
            LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            NextStepCommand = new RelayCommand(NextStep, CanGoNext);
            PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep > 1);
            ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
            CancelCommand = new RelayCommand(CancelWizard);
            
            // 模板和约束命令
            LoadTemplateCommand = new AsyncRelayCommand<int>(LoadTemplateAsync);
            LoadConstraintsCommand = new AsyncRelayCommand(LoadConstraintsAsync);
            SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync, CanSaveTemplate);

            // 手动指定命令
            StartCreateManualAssignmentCommand = new RelayCommand(StartCreateManualAssignment);
            SubmitCreateManualAssignmentCommand = new AsyncRelayCommand(SubmitCreateManualAssignmentAsync);
            CancelCreateManualAssignmentCommand = new RelayCommand(CancelCreateManualAssignment);
            StartEditManualAssignmentCommand = new RelayCommand<ManualAssignmentViewModel?>(StartEditManualAssignment);
            SubmitEditManualAssignmentCommand = new AsyncRelayCommand(SubmitEditManualAssignmentAsync);
            CancelEditManualAssignmentCommand = new RelayCommand(CancelEditManualAssignment);
            DeleteManualAssignmentCommand = new AsyncRelayCommand<ManualAssignmentViewModel?>(DeleteManualAssignmentAsync);

            // 哨位人员命令
            StartAddPersonnelToPositionCommand = new RelayCommand<PositionDto>(StartAddPersonnelToPosition);
            SubmitAddPersonnelToPositionCommand = new AsyncRelayCommand(SubmitAddPersonnelToPositionAsync);
            CancelAddPersonnelToPositionCommand = new RelayCommand(CancelAddPersonnelToPosition);
            RemovePersonnelFromPositionCommand = new RelayCommand<(int positionId, int personnelId)>(RemovePersonnelFromPosition);
            RevertPositionChangesCommand = new RelayCommand<int>(RevertPositionChanges);
            SavePositionChangesCommand = new AsyncRelayCommand<int>(SavePositionChangesAsync);

            // 手动添加参与人员命令
            StartManualAddPersonnelCommand = new RelayCommand(StartManualAddPersonnel);
            SubmitManualAddPersonnelCommand = new AsyncRelayCommand(SubmitManualAddPersonnelAsync);
            CancelManualAddPersonnelCommand = new RelayCommand(CancelManualAddPersonnel);
            RemoveManualPersonnelCommand = new RelayCommand<int>(RemoveManualPersonnel);
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            // 监听属性变更以刷新命令状态
            PropertyChanged += (s, e) =>
            {
                if (e?.PropertyName is nameof(CurrentStep) or nameof(ScheduleTitle) or nameof(StartDate) or nameof(EndDate) or nameof(IsExecuting))
                {
                    RefreshCommandStates();
                }
                
                // 当进入步骤3时，更新哨位人员视图模型
                if (e?.PropertyName == nameof(CurrentStep) && CurrentStep == 3)
                {
                    UpdatePositionPersonnelViewModels();
                }
            };
            
            // 监听集合变更
            SelectedPersonnels.CollectionChanged += (s, e) => RefreshCommandStates();
            SelectedPositions.CollectionChanged += (s, e) => 
            {
                RefreshCommandStates();
                // 当选择的哨位发生变化时，更新哨位人员视图模型
                if (CurrentStep == 3)
                {
                    UpdatePositionPersonnelViewModels();
                }
            };
        }

        #endregion
        #region 初始化方法

        /// <summary>
        /// 加载初始数据（人员和哨位列表）
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            if (IsLoadingInitial) return;
            
            IsLoadingInitial = true;
            try
            {
                // 并行加载人员和哨位数据
                var personnelTask = _personnelService.GetAllAsync();
                var positionTask = _positionService.GetAllAsync();
                await Task.WhenAll(personnelTask, positionTask);
                
                // 设置可用列表
                AvailablePersonnels = new ObservableCollection<PersonnelDto>(personnelTask.Result);
                AvailablePositions = new ObservableCollection<PositionDto>(positionTask.Result);
                
                // 构建缓存
                BuildCaches();
                
                // 设置默认标题
                if (string.IsNullOrWhiteSpace(ScheduleTitle))
                {
                    ScheduleTitle = $"排班_{DateTime.Now:yyyyMMdd}";
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("加载人员/岗位失败", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        /// <summary>
        /// 构建人员和哨位查找缓存
        /// </summary>
        private void BuildCaches()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 构建人员缓存
            _personnelCache.Clear();
            foreach (var personnel in AvailablePersonnels)
            {
                _personnelCache[personnel.Id] = personnel;
            }
            
            // 构建哨位缓存
            _positionCache.Clear();
            foreach (var position in AvailablePositions)
            {
                _positionCache[position.Id] = position;
            }
            
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"缓存构建完成: {_personnelCache.Count}个人员, {_positionCache.Count}个哨位, 耗时{stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 缓存访问方法

        /// <summary>
        /// 从缓存中获取人员
        /// </summary>
        /// <param name="personnelId">人员ID</param>
        /// <returns>人员DTO，如果不存在则返回null</returns>
        private PersonnelDto? GetPersonnelFromCache(int personnelId)
        {
            return _personnelCache.TryGetValue(personnelId, out var personnel) ? personnel : null;
        }

        /// <summary>
        /// 从缓存中获取哨位
        /// </summary>
        /// <param name="positionId">哨位ID</param>
        /// <returns>哨位DTO，如果不存在则返回null</returns>
        private PositionDto? GetPositionFromCache(int positionId)
        {
            return _positionCache.TryGetValue(positionId, out var position) ? position : null;
        }

        #endregion
    }
}