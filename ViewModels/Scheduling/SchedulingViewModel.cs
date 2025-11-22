using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Collections.Generic;
using AutoScheduling3.Models.Constraints; // Լģ
using CommunityToolkit.Mvvm.Input.Internals;
using System.Diagnostics.CodeAnalysis;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel : ObservableObject
    {
        private readonly ISchedulingService _schedulingService;
        private readonly IPersonnelService _personnelService;
        private readonly IPositionService _positionService;
        private readonly ITemplateService _templateService; // ģ
        private readonly ISchedulingDraftService? _draftService; // 草稿服务（可选）
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigation_service; // ԭֶ
        private Microsoft.UI.Xaml.Controls.ContentDialog? _progressDialog; //ȶԻ



        // 手动指定管理器
        private readonly ManualAssignmentManager _manualAssignmentManager;

        // 哨位人员管理器
        private readonly PositionPersonnelManager _positionPersonnelManager;

        // 缓存：人员ID到PersonnelDto的映射
        private readonly Dictionary<int, PersonnelDto> _personnelCache = new();
        
        // 缓存：哨位ID到PositionDto的映射
        private readonly Dictionary<int, PositionDto> _positionCache = new();





        public SchedulingViewModel(
        ISchedulingService schedulingService,
        IPersonnelService personnelService,
        IPositionService positionService,
        ITemplateService templateService,
        ISchedulingDraftService? draftService,
        DialogService dialogService,
        NavigationService navigationService)
        {
            _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
            _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _draftService = draftService; // 草稿服务可以为null
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigation_service = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // 初始化手动指定管理器
            _manualAssignmentManager = new ManualAssignmentManager();
            
            // 初始化哨位人员管理器
            _positionPersonnelManager = new PositionPersonnelManager();

            LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            NextStepCommand = new RelayCommand(NextStep, CanGoNext);
            PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep > 1);
            ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
            CancelCommand = new RelayCommand(CancelWizard);
            LoadTemplateCommand = new AsyncRelayCommand<int>(LoadTemplateAsync);
            LoadConstraintsCommand = new AsyncRelayCommand(LoadConstraintsAsync);
            SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync, CanSaveTemplate);

            // 初始化手动指定命令
            StartCreateManualAssignmentCommand = new RelayCommand(StartCreateManualAssignment);
            SubmitCreateManualAssignmentCommand = new AsyncRelayCommand(SubmitCreateManualAssignmentAsync);
            CancelCreateManualAssignmentCommand = new RelayCommand(CancelCreateManualAssignment);
            StartEditManualAssignmentCommand = new RelayCommand<ManualAssignmentViewModel?>(StartEditManualAssignment);
            SubmitEditManualAssignmentCommand = new AsyncRelayCommand(SubmitEditManualAssignmentAsync);
            CancelEditManualAssignmentCommand = new RelayCommand(CancelEditManualAssignment);
            DeleteManualAssignmentCommand = new AsyncRelayCommand<ManualAssignmentViewModel?>(DeleteManualAssignmentAsync);

            // 初始化为哨位添加人员命令
            StartAddPersonnelToPositionCommand = new RelayCommand<PositionDto>(StartAddPersonnelToPosition);
            SubmitAddPersonnelToPositionCommand = new AsyncRelayCommand(SubmitAddPersonnelToPositionAsync);
            CancelAddPersonnelToPositionCommand = new RelayCommand(CancelAddPersonnelToPosition);

            // 初始化移除和撤销人员命令
            RemovePersonnelFromPositionCommand = new RelayCommand<(int positionId, int personnelId)>(RemovePersonnelFromPosition);
            RevertPositionChangesCommand = new RelayCommand<int>(RevertPositionChanges);

            // 初始化保存为永久命令
            SavePositionChangesCommand = new AsyncRelayCommand<int>(SavePositionChangesAsync);

            // 初始化手动添加参与人员命令
            StartManualAddPersonnelCommand = new RelayCommand(StartManualAddPersonnel);
            SubmitManualAddPersonnelCommand = new AsyncRelayCommand(SubmitManualAddPersonnelAsync);
            CancelManualAddPersonnelCommand = new RelayCommand(CancelManualAddPersonnel);
            RemoveManualPersonnelCommand = new RelayCommand<int>(RemoveManualPersonnel);

            // Listen to property changes to refresh command states
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



        private async Task LoadInitialDataAsync()
        {
            if (IsLoadingInitial) return;
            IsLoadingInitial = true;
            try
            {
                var personnelTask = _personnelService.GetAllAsync();
                var positionTask = _positionService.GetAllAsync();
                await Task.WhenAll(personnelTask, positionTask);
                AvailablePersonnels = new ObservableCollection<PersonnelDto>(personnelTask.Result);
                AvailablePositions = new ObservableCollection<PositionDto>(positionTask.Result);
                
                // 构建缓存
                BuildCaches();
                
                // 默认标题
                if (string.IsNullOrWhiteSpace(ScheduleTitle))
                    ScheduleTitle = $"排班_{DateTime.Now:yyyyMMdd}";
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

        /// <summary>
        /// 从缓存中获取人员
        /// </summary>
        private PersonnelDto? GetPersonnelFromCache(int personnelId)
        {
            return _personnelCache.TryGetValue(personnelId, out var personnel) ? personnel : null;
        }

        /// <summary>
        /// 从缓存中获取哨位
        /// </summary>
        private PositionDto? GetPositionFromCache(int positionId)
        {
            return _positionCache.TryGetValue(positionId, out var position) ? position : null;
        }

        private async Task LoadConstraintsAsync()
        {
            // 重复加载检查
            if (IsLoadingConstraints)
            {
                System.Diagnostics.Debug.WriteLine("约束数据正在加载中，跳过重复请求");
                return;
            }
            
            IsLoadingConstraints = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("=== 开始加载约束数据 ===");
            System.Diagnostics.Debug.WriteLine($"日期范围: {StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd}");
            
            try
            {
                // 并行加载三种约束数据
                var configsTask = _schedulingService.GetHolidayConfigsAsync();
                var rulesTask = _schedulingService.GetFixedPositionRulesAsync(true);
                var manualTask = _schedulingService.GetManualAssignmentsAsync(StartDate.Date, EndDate.Date, true);
                
                await Task.WhenAll(configsTask, rulesTask, manualTask);
                
                var configs = configsTask.Result;
                var rules = rulesTask.Result;
                var manuals = manualTask.Result;
                
                System.Diagnostics.Debug.WriteLine($"加载完成 - 休息日配置: {configs.Count}, 定岗规则: {rules.Count}, 手动指定: {manuals.Count}");
                
                // 更新 ObservableCollection
                HolidayConfigs = new ObservableCollection<HolidayConfig>(configs);
                FixedPositionRules = new ObservableCollection<FixedPositionRule>(rules);
                ManualAssignments = new ObservableCollection<ManualAssignment>(manuals);
                
                // 转换为DTO并加载手动指定到ManualAssignmentManager（使用缓存提高性能）
                var manualDtos = manuals.Select(m =>
                {
                    // 从缓存中查找人员和岗位名称
                    var personnel = GetPersonnelFromCache(m.PersonalId);
                    var position = GetPositionFromCache(m.PositionId);
                    
                    return new ManualAssignmentDto
                    {
                        Id = m.Id,
                        Date = m.Date,
                        PersonnelId = m.PersonalId,
                        PersonnelName = personnel?.Name ?? "未知人员",
                        PositionId = m.PositionId,
                        PositionName = position?.Name ?? "未知哨位",
                        TimeSlot = m.PeriodIndex,
                        Remarks = m.Remarks,
                        IsEnabled = m.IsEnabled,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }).ToList();
                _manualAssignmentManager.LoadSaved(manualDtos);
                System.Diagnostics.Debug.WriteLine($"已加载 {manuals.Count} 条手动指定到ManualAssignmentManager");
                
                // 空数据警告日志
                if (configs.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到休息日配置");
                if (rules.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到定岗规则");
                if (manuals.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到手动指定");
                
                // 应用模板约束（如果有）
                if (TemplateApplied)
                {
                    System.Diagnostics.Debug.WriteLine("应用模板约束设置");
                    ApplyTemplateConstraints();
                }
            }
            catch (Microsoft.Data.Sqlite.SqliteException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (数据库错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {sqlEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误代码: {sqlEx.SqliteErrorCode}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {sqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {sqlEx.StackTrace}");
                
                await _dialogService.ShowErrorAsync(
                    "无法连接到数据库",
                    $"请检查数据库文件是否存在且未被占用。\n\n错误详情: {sqlEx.Message}");
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (数据格式错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {jsonEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"路径: {jsonEx.Path}");
                System.Diagnostics.Debug.WriteLine($"行号: {jsonEx.LineNumber}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {jsonEx.StackTrace}");
                
                await _dialogService.ShowErrorAsync(
                    "约束数据格式错误",
                    $"数据库中的约束数据格式不正确，可能需要重新创建。\n\n错误详情: {jsonEx.Message}");
            }
            catch (TaskCanceledException taskEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (超时) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {taskEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {taskEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {taskEx.StackTrace}");
                
                await _dialogService.ShowWarningAsync("加载约束数据超时，请重试");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (未知错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"内部异常消息: {ex.InnerException.Message}");
                }
                
                await _dialogService.ShowErrorAsync(
                    "加载约束数据失败",
                    $"发生了意外错误。\n\n错误类型: {ex.GetType().Name}\n错误消息: {ex.Message}");
            }
            finally
            {
                IsLoadingConstraints = false;
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"约束数据加载耗时: {stopwatch.ElapsedMilliseconds}ms");
                System.Diagnostics.Debug.WriteLine("=== 约束数据加载流程结束 ===");
            }
        }



        private async Task LoadTemplateAsync(int templateId)
        {
            IsLoadingInitial = true;
            System.Diagnostics.Debug.WriteLine($"=== 开始加载模板 (ID: {templateId}) ===");
            
            try
            {
                var template = await _templateService.GetByIdAsync(templateId);
                if (template == null)
                {
                    System.Diagnostics.Debug.WriteLine($"模板不存在 (ID: {templateId})");
                    await _dialogService.ShowWarningAsync("模板不存在");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"模板加载成功: {template.Name}");
                System.Diagnostics.Debug.WriteLine($"模板包含 - 人员: {template.PersonnelIds.Count}, 岗位: {template.PositionIds.Count}");
                
                LoadedTemplateId = template.Id;

                // Load base data first
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("加载基础数据（人员和岗位）");
                    await LoadInitialDataAsync();
                }

                // Pre-fill selections
                var selectedPers = AvailablePersonnels.Where(p => template.PersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => template.PositionIds.Contains(p.Id)).ToList();
                
                // Validate and identify missing resources
                var missingPersonnelIds = template.PersonnelIds
                    .Except(selectedPers.Select(p => p.Id))
                    .ToList();
                var missingPositionIds = template.PositionIds
                    .Except(selectedPos.Select(p => p.Id))
                    .ToList();
                
                // Display warning if any resources are missing
                if (missingPersonnelIds.Any() || missingPositionIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine("警告: 模板中的部分资源已不存在");
                    if (missingPersonnelIds.Any())
                        System.Diagnostics.Debug.WriteLine($"- 缺失人员ID: {string.Join(", ", missingPersonnelIds)}");
                    if (missingPositionIds.Any())
                        System.Diagnostics.Debug.WriteLine($"- 缺失岗位ID: {string.Join(", ", missingPositionIds)}");
                    
                    var warningMsg = "模板中的部分资源已不存在：\n";
                    if (missingPersonnelIds.Any())
                        warningMsg += $"- 缺失人员ID: {string.Join(", ", missingPersonnelIds)}\n";
                    if (missingPositionIds.Any())
                        warningMsg += $"- 缺失岗位ID: {string.Join(", ", missingPositionIds)}\n";
                    warningMsg += "\n将仅加载可用的资源。";
                    await _dialogService.ShowWarningAsync(warningMsg);
                }
                
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                // Store constraint IDs to be applied after they are loaded
                UseActiveHolidayConfig = template.UseActiveHolidayConfig;
                SelectedHolidayConfigId = template.HolidayConfigId;
                _enabledFixedRules = template.EnabledFixedRuleIds?.ToList() ?? new();
                _enabledManualAssignments = template.EnabledManualAssignmentIds?.ToList() ?? new();
                
                System.Diagnostics.Debug.WriteLine($"模板约束配置 - 固定规则: {_enabledFixedRules.Count}, 手动指定: {_enabledManualAssignments.Count}");
                System.Diagnostics.Debug.WriteLine($"节假日配置 - 使用活动配置: {UseActiveHolidayConfig}, 配置ID: {SelectedHolidayConfigId}");

                // Immediately load constraint data
                System.Diagnostics.Debug.WriteLine("开始加载约束数据以应用模板设置");
                await LoadConstraintsAsync();
                
                // Apply template constraints after loading
                System.Diagnostics.Debug.WriteLine("约束数据加载完成，开始应用模板约束设置");
                ApplyTemplateConstraints();

                TemplateApplied = true;
                CurrentStep = 1; // Stay on step 1
                RefreshCommandStates();
                
                // Calculate constraint counts
                var enabledFixedRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
                var enabledManualAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
                var totalConstraints = enabledFixedRulesCount + enabledManualAssignmentsCount;
                
                System.Diagnostics.Debug.WriteLine($"模板应用完成 - 已启用约束: {totalConstraints} (固定规则: {enabledFixedRulesCount}, 手动指定: {enabledManualAssignmentsCount})");
                System.Diagnostics.Debug.WriteLine("=== 模板加载流程结束 ===");
                
                // Display success message with statistics
                var successMsg = $"模板已加载\n" +
                                $"人员: {selectedPers.Count}\n" +
                                $"岗位: {selectedPos.Count}\n" +
                                $"约束: {totalConstraints}";
                await _dialogService.ShowSuccessAsync(successMsg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 加载模板失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("加载模板失败", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        private void ApplyTemplateConstraints()
        {
            if (!TemplateApplied)
            {
                System.Diagnostics.Debug.WriteLine("ApplyTemplateConstraints: 模板未应用，跳过");
                return;
            }

            System.Diagnostics.Debug.WriteLine("=== 开始应用模板约束 ===");
            System.Diagnostics.Debug.WriteLine($"模板中的固定规则ID: [{string.Join(", ", _enabledFixedRules)}]");
            System.Diagnostics.Debug.WriteLine($"模板中的手动指定ID: [{string.Join(", ", _enabledManualAssignments)}]");
            System.Diagnostics.Debug.WriteLine($"数据库中的固定规则数量: {FixedPositionRules.Count}");
            System.Diagnostics.Debug.WriteLine($"数据库中的手动指定数量: {ManualAssignments.Count}");

            // 验证并应用固定规则
            var appliedFixedRules = 0;
            var missingFixedRuleIds = new List<int>();
            
            foreach (var rule in FixedPositionRules)
            {
                if (_enabledFixedRules.Contains(rule.Id))
                {
                    rule.IsEnabled = true;
                    appliedFixedRules++;
                    System.Diagnostics.Debug.WriteLine($"启用固定规则: ID={rule.Id}, 描述={rule.Description}");
                }
                else
                {
                    rule.IsEnabled = false;
                }
            }
            
            // 检查模板中的规则是否在数据库中存在
            var existingFixedRuleIds = FixedPositionRules.Select(r => r.Id).ToHashSet();
            foreach (var templateRuleId in _enabledFixedRules)
            {
                if (!existingFixedRuleIds.Contains(templateRuleId))
                {
                    missingFixedRuleIds.Add(templateRuleId);
                }
            }

            // 验证并应用手动指定
            var appliedManualAssignments = 0;
            var missingManualAssignmentIds = new List<int>();
            
            foreach (var assignment in ManualAssignments)
            {
                if (_enabledManualAssignments.Contains(assignment.Id))
                {
                    assignment.IsEnabled = true;
                    appliedManualAssignments++;
                    System.Diagnostics.Debug.WriteLine($"启用手动指定: ID={assignment.Id}, 日期={assignment.Date:yyyy-MM-dd}, 人员={assignment.PersonalId}, 岗位={assignment.PositionId}");
                }
                else
                {
                    assignment.IsEnabled = false;
                }
            }
            
            // 同步手动指定启用状态到ManualAssignmentManager
            foreach (var savedAssignment in _manualAssignmentManager.SavedAssignments)
            {
                if (savedAssignment.Id.HasValue)
                {
                    savedAssignment.IsEnabled = _enabledManualAssignments.Contains(savedAssignment.Id.Value);
                }
            }
            System.Diagnostics.Debug.WriteLine($"已同步 {_manualAssignmentManager.SavedAssignments.Count} 条手动指定的启用状态到ManualAssignmentManager");
            
            // 检查模板中的手动指定是否在数据库中存在
            var existingManualAssignmentIds = ManualAssignments.Select(a => a.Id).ToHashSet();
            foreach (var templateAssignmentId in _enabledManualAssignments)
            {
                if (!existingManualAssignmentIds.Contains(templateAssignmentId))
                {
                    missingManualAssignmentIds.Add(templateAssignmentId);
                }
            }

            System.Diagnostics.Debug.WriteLine($"应用结果 - 固定规则: {appliedFixedRules}/{_enabledFixedRules.Count}, 手动指定: {appliedManualAssignments}/{_enabledManualAssignments.Count}");

            // 记录缺失的约束
            if (missingFixedRuleIds.Any() || missingManualAssignmentIds.Any())
            {
                System.Diagnostics.Debug.WriteLine("警告: 模板中的部分约束在数据库中不存在");
                
                if (missingFixedRuleIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"- 缺失的固定规则ID: {string.Join(", ", missingFixedRuleIds)}");
                }
                
                if (missingManualAssignmentIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"- 缺失的手动指定ID: {string.Join(", ", missingManualAssignmentIds)}");
                }
                
                // 构建警告消息并显示给用户
                var warningMsg = "模板中的部分约束已不存在：\n";
                if (missingFixedRuleIds.Any())
                {
                    warningMsg += $"- 缺失的固定规则: {missingFixedRuleIds.Count} 条 (ID: {string.Join(", ", missingFixedRuleIds)})\n";
                }
                if (missingManualAssignmentIds.Any())
                {
                    warningMsg += $"- 缺失的手动指定: {missingManualAssignmentIds.Count} 条 (ID: {string.Join(", ", missingManualAssignmentIds)})\n";
                }
                warningMsg += "\n将仅应用存在的约束。";
                
                // 异步显示警告（不阻塞当前流程）
                _ = _dialogService.ShowWarningAsync(warningMsg);
            }
            
            System.Diagnostics.Debug.WriteLine("=== 模板约束应用完成 ===");
        }

        private bool CanSaveTemplate() => SelectedPersonnels.Count > 0 && SelectedPositions.Count > 0;
        private async Task SaveAsTemplateAsync()
        {
            if (!CanSaveTemplate()) { await _dialogService.ShowWarningAsync("当前配置不完整，无法保存为模板"); return; }
            // 创建自定义对话框 (名称、类型、描述、是否默认)
            var nameBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "模板名称", Text = $"模板_{DateTime.Now:yyyyMMdd}" };
            var typeBox = new Microsoft.UI.Xaml.Controls.ComboBox { ItemsSource = new string[] { "regular", "holiday", "special" }, SelectedIndex = 0, MinWidth = 160 };
            var descBox = new Microsoft.UI.Xaml.Controls.TextBox { AcceptsReturn = true, Height = 80, PlaceholderText = "描述(可选)" };
            var defaultSwitch = new Microsoft.UI.Xaml.Controls.ToggleSwitch { Header = "设为默认", IsOn = false };
            var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8 };
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "名称:" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "类型:" });
            panel.Children.Add(typeBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "描述:" });
            panel.Children.Add(descBox);
            panel.Children.Add(defaultSwitch);
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "另存为模板",
                Content = panel,
                PrimaryButtonText = "保存",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return;

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary) return;
            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { await _dialogService.ShowWarningAsync("名称不能为空"); return; }
            var type = typeBox.SelectedItem?.ToString() ?? "regular";
            if (name.Length > 100) { await _dialogService.ShowWarningAsync("名称不能超过100字符"); return; }
            
            // 保存临时手动指定到数据库
            var tempIdToSavedIdMap = new Dictionary<Guid, int>();
            var savedManualAssignmentIds = new List<int>();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始保存模板 '{name}' ===");
                System.Diagnostics.Debug.WriteLine($"临时手动指定数量: {_manualAssignmentManager.TemporaryAssignments.Count}");
                
                // 保存所有临时手动指定
                foreach (var tempAssignment in _manualAssignmentManager.TemporaryAssignments)
                {
                    var createDto = new CreateManualAssignmentDto
                    {
                        Date = tempAssignment.Date,
                        PersonnelId = tempAssignment.PersonnelId,
                        PositionId = tempAssignment.PositionId,
                        TimeSlot = tempAssignment.TimeSlot,
                        Remarks = tempAssignment.Remarks,
                        IsEnabled = tempAssignment.IsEnabled
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"保存临时手动指定: 日期={tempAssignment.Date:yyyy-MM-dd}, 人员={tempAssignment.PersonnelName}, 哨位={tempAssignment.PositionName}, 时段={tempAssignment.TimeSlot}");
                    
                    var savedDto = await _schedulingService.CreateManualAssignmentAsync(createDto);
                    tempIdToSavedIdMap[tempAssignment.TempId] = savedDto.Id;
                    savedManualAssignmentIds.Add(savedDto.Id);
                    
                    System.Diagnostics.Debug.WriteLine($"手动指定已保存，ID={savedDto.Id}");
                }
                
                // 将临时手动指定标记为已保存
                if (tempIdToSavedIdMap.Count > 0)
                {
                    _manualAssignmentManager.MarkAsSaved(tempIdToSavedIdMap);
                    System.Diagnostics.Debug.WriteLine($"已将 {tempIdToSavedIdMap.Count} 条临时手动指定标记为已保存");
                }
                
                // 收集所有启用的手动指定ID（包括已保存的和新保存的）
                var allEnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList();
                allEnabledManualAssignmentIds.AddRange(savedManualAssignmentIds);
                
                System.Diagnostics.Debug.WriteLine($"模板将包含 {allEnabledManualAssignmentIds.Count} 条启用的手动指定");
                
                var templateDto = new CreateTemplateDto
                {
                    Name = name,
                    TemplateType = type,
                    Description = string.IsNullOrWhiteSpace(descBox.Text) ? null : descBox.Text.Trim(),
                    IsDefault = defaultSwitch.IsOn,
                    PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                    PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                    HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
                    UseActiveHolidayConfig = UseActiveHolidayConfig,
                    EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                    EnabledManualAssignmentIds = allEnabledManualAssignmentIds
                };
                
                var tpl = await _templateService.CreateAsync(templateDto);
                System.Diagnostics.Debug.WriteLine($"模板 '{tpl.Name}' 保存成功，ID={tpl.Id}");
                System.Diagnostics.Debug.WriteLine("=== 模板保存完成 ===");
                
                await _dialogService.ShowSuccessAsync($"模板 '{tpl.Name}' 已保存");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 保存模板失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 如果保存失败，需要回滚已保存的手动指定
                if (tempIdToSavedIdMap.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("尝试回滚已保存的手动指定...");
                    // 注意：这里简化处理，实际应该删除已保存的手动指定
                    // 但由于这是一个复杂的回滚操作，暂时只记录日志
                    System.Diagnostics.Debug.WriteLine($"警告: {tempIdToSavedIdMap.Count} 条手动指定已保存但模板创建失败，可能需要手动清理");
                }
                
                await _dialogService.ShowErrorAsync("保存模板失败", ex);
            }
        }



        #region 草稿保存和恢复

        /// <summary>
        /// 创建草稿（保存当前状态）
        /// </summary>
        public async Task CreateDraftAsync()
        {
            if (_draftService == null)
            {
                System.Diagnostics.Debug.WriteLine("草稿服务未初始化，跳过保存");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始保存草稿 ===");

                // 收集所有哨位的临时更改
                var positionPersonnelChanges = new Dictionary<int, PositionPersonnelChangeDto>();
                var positionsWithChanges = _positionPersonnelManager.GetPositionsWithChanges();
                
                foreach (var positionId in positionsWithChanges)
                {
                    var changes = _positionPersonnelManager.GetChanges(positionId);
                    positionPersonnelChanges[positionId] = new PositionPersonnelChangeDto
                    {
                        PositionId = changes.PositionId,
                        AddedPersonnelIds = new List<int>(changes.AddedPersonnelIds),
                        RemovedPersonnelIds = new List<int>(changes.RemovedPersonnelIds)
                    };
                }

                System.Diagnostics.Debug.WriteLine($"保存 {positionPersonnelChanges.Count} 个哨位的临时更改");
                System.Diagnostics.Debug.WriteLine($"保存 {ManuallyAddedPersonnelIds.Count} 个手动添加的人员");

                // 获取所有启用的手动指定
                var allEnabledAssignments = _manualAssignmentManager.AllAssignments
                    .Where(a => a.IsEnabled)
                    .ToList();
                
                var enabledManualAssignmentIds = allEnabledAssignments
                    .Where(a => a.Id.HasValue)
                    .Select(a => a.Id.Value)
                    .ToList();
                
                var temporaryManualAssignments = allEnabledAssignments
                    .Where(a => !a.Id.HasValue)
                    .ToList();

                // 创建草稿DTO
                var draft = new SchedulingDraftDto
                {
                    ScheduleTitle = ScheduleTitle,
                    StartDate = StartDate.DateTime,
                    EndDate = EndDate.DateTime,
                    CurrentStep = CurrentStep,
                    TemplateApplied = TemplateApplied,
                    LoadedTemplateId = LoadedTemplateId,
                    SelectedPersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                    SelectedPositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                    UseActiveHolidayConfig = UseActiveHolidayConfig,
                    SelectedHolidayConfigId = SelectedHolidayConfigId,
                    EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                    EnabledManualAssignmentIds = enabledManualAssignmentIds,
                    TemporaryManualAssignments = temporaryManualAssignments
                         .Select(vm => new ManualAssignmentDraftDto
                         {
                             Date = vm.Date,
                             PersonnelId = vm.PersonnelId,
                             PositionId = vm.PositionId,
                             TimeSlot = vm.TimeSlot,
                             Remarks = vm.Remarks,
                             IsEnabled = vm.IsEnabled,
                             TempId = vm.TempId
                         })
                         .ToList(),
                    PositionPersonnelChanges = positionPersonnelChanges,
                    ManuallyAddedPersonnelIds = new List<int>(ManuallyAddedPersonnelIds),
                    SavedAt = DateTime.Now,
                    Version = "1.0"
                };

                await _draftService.SaveDraftAsync(draft);
                System.Diagnostics.Debug.WriteLine("草稿保存成功");
                System.Diagnostics.Debug.WriteLine("=== 草稿保存完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存草稿失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                // 不抛出异常，避免影响主流程
            }
        }

        /// <summary>
        /// 从草稿恢复状态
        /// </summary>
        public async Task<bool> RestoreFromDraftAsync()
        {
            if (_draftService == null)
            {
                System.Diagnostics.Debug.WriteLine("草稿服务未初始化，跳过恢复");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始恢复草稿 ===");

                var draft = await _draftService.LoadDraftAsync();
                if (draft == null)
                {
                    System.Diagnostics.Debug.WriteLine("没有找到草稿");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"找到草稿: {draft.ScheduleTitle}");
                System.Diagnostics.Debug.WriteLine($"草稿保存时间: {draft.SavedAt}");
                System.Diagnostics.Debug.WriteLine($"草稿步骤: {draft.CurrentStep}");

                // 加载基础数据（如果还没有加载）
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("加载基础数据");
                    await LoadInitialDataAsync();
                }

                // 恢复基本信息
                ScheduleTitle = draft.ScheduleTitle;
                StartDate = new DateTimeOffset(draft.StartDate);
                EndDate = new DateTimeOffset(draft.EndDate);
                TemplateApplied = draft.TemplateApplied;
                LoadedTemplateId = draft.LoadedTemplateId;
                UseActiveHolidayConfig = draft.UseActiveHolidayConfig;
                SelectedHolidayConfigId = draft.SelectedHolidayConfigId;

                // 恢复选中的人员和哨位
                var selectedPers = AvailablePersonnels.Where(p => draft.SelectedPersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => draft.SelectedPositionIds.Contains(p.Id)).ToList();
                
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                System.Diagnostics.Debug.WriteLine($"恢复人员: {SelectedPersonnels.Count}, 哨位: {SelectedPositions.Count}");

                // 恢复约束数据（如果需要）
                if (draft.CurrentStep >= 4)
                {
                    System.Diagnostics.Debug.WriteLine("加载约束数据");
                    await LoadConstraintsAsync();

                    // 恢复约束启用状态
                    foreach (var rule in FixedPositionRules)
                    {
                        rule.IsEnabled = draft.EnabledFixedRuleIds.Contains(rule.Id);
                    }

                    // 恢复手动指定
                    _manualAssignmentManager.Clear();
                    
                    // 加载已保存的手动指定（使用缓存提高性能）
                    var savedManualAssignments = ManualAssignments
                        .Where(m => draft.EnabledManualAssignmentIds.Contains(m.Id))
                        .Select(m =>
                        {
                            var personnel = GetPersonnelFromCache(m.PersonalId);
                            var position = GetPositionFromCache(m.PositionId);
                            
                            return new ManualAssignmentDto
                            {
                                Id = m.Id,
                                Date = m.Date,
                                PersonnelId = m.PersonalId,
                                PersonnelName = personnel?.Name ?? "未知人员",
                                PositionId = m.PositionId,
                                PositionName = position?.Name ?? "未知哨位",
                                TimeSlot = m.PeriodIndex,
                                Remarks = m.Remarks,
                                IsEnabled = true,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                        })
                        .ToList();
                    
                    _manualAssignmentManager.LoadSaved(savedManualAssignments);
                    
                    // 恢复临时手动指定
                    foreach (var tempAssignment in draft.TemporaryManualAssignments)
                    {
                        var personnel = GetPersonnelFromCache(tempAssignment.PersonnelId);
                        var position = GetPositionFromCache(tempAssignment.PositionId);
                        
                        var dto = new CreateManualAssignmentDto
                        {
                            Date = tempAssignment.Date,
                            PersonnelId = tempAssignment.PersonnelId,
                            PositionId = tempAssignment.PositionId,
                            TimeSlot = tempAssignment.TimeSlot,
                            Remarks = tempAssignment.Remarks,
                            IsEnabled = tempAssignment.IsEnabled
                        };
                        
                        _manualAssignmentManager.AddTemporary(dto, personnel?.Name ?? "未知人员", position?.Name ?? "未知哨位", tempAssignment.TempId);
                    }

                    System.Diagnostics.Debug.WriteLine($"恢复手动指定: 已保存 {savedManualAssignments.Count}, 临时 {draft.TemporaryManualAssignments.Count}");
                }

                // 恢复PositionPersonnelManager状态
                if (draft.PositionPersonnelChanges.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"恢复 {draft.PositionPersonnelChanges.Count} 个哨位的临时更改");
                    
                    // 初始化PositionPersonnelManager
                    _positionPersonnelManager.Initialize(SelectedPositions);
                    
                    // 恢复每个哨位的临时更改
                    foreach (var kvp in draft.PositionPersonnelChanges)
                    {
                        var positionId = kvp.Key;
                        var changes = kvp.Value;
                        
                        // 添加临时添加的人员
                        foreach (var personnelId in changes.AddedPersonnelIds)
                        {
                            _positionPersonnelManager.AddPersonnelTemporarily(positionId, personnelId);
                        }
                        
                        // 移除临时移除的人员
                        foreach (var personnelId in changes.RemovedPersonnelIds)
                        {
                            _positionPersonnelManager.RemovePersonnelTemporarily(positionId, personnelId);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"哨位 {positionId}: 添加 {changes.AddedPersonnelIds.Count}, 移除 {changes.RemovedPersonnelIds.Count}");
                    }
                }

                // 恢复手动添加的人员列表
                ManuallyAddedPersonnelIds.Clear();
                foreach (var personnelId in draft.ManuallyAddedPersonnelIds)
                {
                    ManuallyAddedPersonnelIds.Add(personnelId);
                }
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                System.Diagnostics.Debug.WriteLine($"恢复手动添加的人员: {ManuallyAddedPersonnelCount}");

                // 恢复步骤（最后设置，因为会触发OnCurrentStepChanged）
                CurrentStep = draft.CurrentStep;

                System.Diagnostics.Debug.WriteLine("草稿恢复成功");
                System.Diagnostics.Debug.WriteLine("=== 草稿恢复完成 ===");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复草稿失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                await _dialogService.ShowErrorAsync("恢复草稿失败", ex);
                return false;
            }
        }

        #endregion

    }
}