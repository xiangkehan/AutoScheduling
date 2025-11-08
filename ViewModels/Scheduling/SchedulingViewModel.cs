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
using AutoScheduling3.Models.Constraints; // ����Լ��ģ������
using CommunityToolkit.Mvvm.Input.Internals;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel : ObservableObject
    {
        private readonly ISchedulingService _schedulingService;
        private readonly IPersonnelService _personnelService;
        private readonly IPositionService _positionService;
        private readonly ITemplateService _templateService; // ����ģ�����
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigation_service; // ����ԭ���������ֶ���
        private Microsoft.UI.Xaml.Controls.ContentDialog? _progressDialog; //���ȶԻ�������

        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private string _scheduleTitle = string.Empty;

        [ObservableProperty]
        private DateTimeOffset _startDate = DateTimeOffset.Now;

        [ObservableProperty]
        private DateTimeOffset _endDate = DateTimeOffset.Now.AddDays(7);

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnels = new();
        [ObservableProperty]
        private ObservableCollection<PositionDto> _availablePositions = new();

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _selectedPersonnels = new();
        [ObservableProperty]
        private ObservableCollection<PositionDto> _selectedPositions = new();

        [ObservableProperty] private List<int> _enabledFixedRules = new();
        [ObservableProperty] private List<int> _enabledManualAssignments = new();

        [ObservableProperty] private bool _useActiveHolidayConfig = true;
        [ObservableProperty] private int? _selectedHolidayConfigId;

        //Լ������Դ��ʹ�������Ա� UI��)
        [ObservableProperty] private ObservableCollection<HolidayConfig> _holidayConfigs = new();
        [ObservableProperty] private ObservableCollection<FixedPositionRule> _fixedPositionRules = new();
        [ObservableProperty] private ObservableCollection<ManualAssignment> _manualAssignments = new();

        [ObservableProperty]
        private ScheduleDto _resultSchedule;

        [ObservableProperty]
        private bool _isExecuting;
        [ObservableProperty]
        private bool _isLoadingInitial;
        [ObservableProperty]
        private bool _isLoadingConstraints; // ʹ�����Թ� UI ��ʾ����״̬

        // ģ����أ�ʹ�����Զ����ֶΣ����ڰ����߼�ͳһ��
        [ObservableProperty] private int? _loadedTemplateId;
        [ObservableProperty] private bool _templateApplied;

        // ��5������չʾ����
        [ObservableProperty] private ObservableCollection<SummarySection> _summarySections = new();

        // ����
        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand NextStepCommand { get; }
        public IRelayCommand PreviousStepCommand { get; }
        public IAsyncRelayCommand ExecuteSchedulingCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IAsyncRelayCommand<int> LoadTemplateCommand { get; }
        public IAsyncRelayCommand LoadConstraintsCommand { get; }
        public IAsyncRelayCommand SaveAsTemplateCommand { get; }

        public SchedulingViewModel(
        ISchedulingService schedulingService,
        IPersonnelService personnelService,
        IPositionService positionService,
        ITemplateService templateService,
        DialogService dialogService,
        NavigationService navigationService)
        {
            _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
            _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigation_service = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            NextStepCommand = new RelayCommand(NextStep, CanGoNext);
            PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep > 1);
            ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
            CancelCommand = new RelayCommand(CancelWizard);
            LoadTemplateCommand = new AsyncRelayCommand<int>(LoadTemplateAsync);
            LoadConstraintsCommand = new AsyncRelayCommand(LoadConstraintsAsync);
            SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync, CanSaveTemplate);

            // Listen to property changes to refresh command states
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(CurrentStep) or nameof(ScheduleTitle) or nameof(StartDate) or nameof(EndDate) or nameof(IsExecuting))
                {
                    RefreshCommandStates();
                }
            };
            SelectedPersonnels.CollectionChanged += (s, e) => RefreshCommandStates();
            SelectedPositions.CollectionChanged += (s, e) => RefreshCommandStates();
        }

        private void NextStep()
        {
            if (CurrentStep < 5 && CanGoNext())
            {
                // ���ģ����Ӧ�ò����ڵ�1����ֱ��������5��
                if (TemplateApplied && CurrentStep == 1)
                {
                    CurrentStep = 5;
                    BuildSummarySections(); // Build summary when jumping to step 5
                }
                else
                {
                    CurrentStep++;
                }

                if (CurrentStep == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
                {
                    _ = LoadConstraintsAsync();
                }
                if (CurrentStep == 5)
                {
                    BuildSummarySections();
                }
                RefreshCommandStates();
            }
        }
        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                // ���ģ����Ӧ�ã���ֹ�ص���Ա/��λ/Լ�����裬ֻ�ܻص���1��
                if (TemplateApplied && CurrentStep > 1 && CurrentStep <= 5)
                {
                    CurrentStep = 1;
                }
                else
                {
                    CurrentStep--;
                }
                RefreshCommandStates();
            }
        }
        private bool CanGoNext() => CurrentStep switch
        {
            1 => ValidateStep1(out _),
            2 => ValidateStep2(out _),
            3 => ValidateStep3(out _),
            4 => true, // Constraints step is always navigable if reached
            5 => false, // Cannot go next from summary
            _ => false
        };
        private bool CanExecuteScheduling() => CurrentStep == 5 && !IsExecuting && ValidateStep1(out _) && ValidateStep2(out _) && ValidateStep3(out _);
        private void RefreshCommandStates()
        {
            NextStepCommand.NotifyCanExecuteChanged();
            PreviousStepCommand.NotifyCanExecuteChanged();
            ExecuteSchedulingCommand.NotifyCanExecuteChanged();
            SaveAsTemplateCommand.NotifyCanExecuteChanged();
        }

        private bool ValidateStep1(out string error)
        {
            if (string.IsNullOrWhiteSpace(ScheduleTitle)) { error = "�Ű����Ʋ���Ϊ��"; return false; }
            if (ScheduleTitle.Length > 100) { error = "�Ű����Ƴ��Ȳ��ܳ���100�ַ�"; return false; }
            if (StartDate.Date < DateTimeOffset.Now.Date) { error = "��ʼ���ڲ������ڽ���"; return false; }
            if (EndDate.Date < StartDate.Date) { error = "�������ڲ������ڿ�ʼ����"; return false; }
            if ((EndDate.Date - StartDate.Date).TotalDays + 1 > 365) { error = "�Ű����ڲ��ܳ���365��"; return false; }
            error = string.Empty; return true;
        }
        private bool ValidateStep2(out string error)
        {
            if (SelectedPersonnels == null || SelectedPersonnels.Count == 0) { error = "������ѡ��һ����Ա"; return false; }
            if (SelectedPersonnels.Any(p => !p.IsAvailable || p.IsRetired)) { error = "���������û�������Ա�����Ƴ�"; return false; }
            error = string.Empty; return true;
        }
        private bool ValidateStep3(out string error)
        {
            if (SelectedPositions == null || SelectedPositions.Count == 0) { error = "������ѡ��һ����λ"; return false; }
            error = string.Empty; return true;
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
                // Ĭ�ϱ���
                if (string.IsNullOrWhiteSpace(ScheduleTitle))
                    ScheduleTitle = $"�Ű��_{DateTime.Now:yyyyMMdd}";
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("������Ա/��λʧ��", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        private async Task LoadConstraintsAsync()
        {
            // ʹ�����Զ����ֶΣ�ȷ�� UI�󶨸���
            if (IsLoadingConstraints) return;
            IsLoadingConstraints = true;
            try
            {
                var configsTask = _schedulingService.GetHolidayConfigsAsync();
                var rulesTask = _schedulingService.GetFixedPositionRulesAsync(true);
                var manualTask = _schedulingService.GetManualAssignmentsAsync(StartDate.Date, EndDate.Date, true);
                await Task.WhenAll(configsTask, rulesTask, manualTask);
                HolidayConfigs = new ObservableCollection<HolidayConfig>(configsTask.Result);
                FixedPositionRules = new ObservableCollection<FixedPositionRule>(rulesTask.Result);
                ManualAssignments = new ObservableCollection<ManualAssignment>(manualTask.Result);

                // After loading, re-evaluate which ones are enabled based on the template or previous state
                if (TemplateApplied)
                {
                    ApplyTemplateConstraints();
                }
            }
            catch (Exception ex)
            {
                // �����ֶ�����_dialogService
                await _dialogService.ShowErrorAsync("����Լ������ʧ��", ex);
            }
            finally
            {
                IsLoadingConstraints = false;
            }
        }

        private async Task ExecuteSchedulingAsync()
        {
            if (!CanExecuteScheduling())
            {
                if (!ValidateStep1(out var e1)) await _dialogService.ShowWarningAsync(e1);
                else if (!ValidateStep2(out var e2)) await _dialogService.ShowWarningAsync(e2);
                else if (!ValidateStep3(out var e3)) await _dialogService.ShowWarningAsync(e3);
                return;
            }
            
            var request = BuildSchedulingRequest();
            
            // 显示进度对话框
            _progressDialog = _dialogService.ShowLoadingDialog("正在生成排班，请稍候...");
            IsExecuting = true;
            RefreshCommandStates();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始执行排班: {request.Title}");
                System.Diagnostics.Debug.WriteLine($"人员数: {request.PersonnelIds.Count}, 哨位数: {request.PositionIds.Count}");
                
                var schedule = await _schedulingService.ExecuteSchedulingAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"排班执行成功，生成 {schedule.Shifts.Count} 个班次");
                
                ResultSchedule = schedule;
                await _dialogService.ShowSuccessAsync("排班生成成功");
                
                try 
                { 
                    _navigation_service.NavigateTo("ScheduleResult", schedule.Id); 
                } 
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"导航失败: {navEx.Message}");
                }
            }
            catch (ArgumentException argEx)
            {
                // 业务规则验证失败
                System.Diagnostics.Debug.WriteLine($"业务规则验证失败: {argEx.Message}");
                await _dialogService.ShowErrorAsync($"排班参数验证失败：{argEx.Message}");
            }
            catch (InvalidOperationException invEx)
            {
                // 操作无效（如数据不存在）
                System.Diagnostics.Debug.WriteLine($"操作无效: {invEx.Message}");
                await _dialogService.ShowErrorAsync($"排班操作失败：{invEx.Message}");
            }
            catch (Exception ex)
            {
                // 其他未知异常
                System.Diagnostics.Debug.WriteLine($"排班执行异常: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync($"执行排班失败：{ex.Message}\n\n详细信息：{ex.GetType().Name}");
            }
            finally
            {
                IsExecuting = false;
                if (_progressDialog != null)
                {
                    try { _progressDialog.Hide(); } catch { }
                    _progressDialog = null;
                }
                RefreshCommandStates();
            }
        }
        private SchedulingRequestDto BuildSchedulingRequest() => new()
        {
            Title = ScheduleTitle.Trim(),
            StartDate = StartDate.DateTime.Date,
            EndDate = EndDate.DateTime.Date,
            PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
            PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
            UseActiveHolidayConfig = UseActiveHolidayConfig,
            HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
            EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
            EnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList()
        };

        private void CancelWizard()
        {
            CurrentStep = 1;
            ScheduleTitle = string.Empty;
            SelectedPersonnels.Clear();
            SelectedPositions.Clear();
            EnabledFixedRules.Clear();
            EnabledManualAssignments.Clear();
            FixedPositionRules.Clear();
            ManualAssignments.Clear();
            HolidayConfigs.Clear();
            ResultSchedule = null;
            SelectedHolidayConfigId = null;
            UseActiveHolidayConfig = true;
            LoadedTemplateId = null;
            TemplateApplied = false;
            SummarySections.Clear();
            RefreshCommandStates();
        }

        private async Task LoadTemplateAsync(int templateId)
        {
            IsLoadingInitial = true;
            try
            {
                var template = await _templateService.GetByIdAsync(templateId);
                if (template == null)
                {
                    await _dialogService.ShowWarningAsync("模板不存在");
                    return;
                }
                LoadedTemplateId = template.Id;

                // Load base data first
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                    await LoadInitialDataAsync();

                // Pre-fill selections
                var selectedPers = AvailablePersonnels.Where(p => template.PersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => template.PositionIds.Contains(p.Id)).ToList();
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                // Store constraint IDs to be applied after they are loaded
                UseActiveHolidayConfig = template.UseActiveHolidayConfig;
                SelectedHolidayConfigId = template.HolidayConfigId;
                _enabledFixedRules = template.EnabledFixedRuleIds?.ToList() ?? new();
                _enabledManualAssignments = template.EnabledManualAssignmentIds?.ToList() ?? new();

                TemplateApplied = true;
                CurrentStep = 1; // Stay on step 1
                RefreshCommandStates();
                await _dialogService.ShowSuccessAsync("ģ���Ѽ��أ���ѡ�����ڷ�Χ��ֱ��ִ���Ű�");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("����ģ��ʧ��", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        private void ApplyTemplateConstraints()
        {
            if (!TemplateApplied) return;

            foreach (var rule in FixedPositionRules)
            {
                rule.IsEnabled = _enabledFixedRules.Contains(rule.Id);
            }
            foreach (var assignment in ManualAssignments)
            {
                assignment.IsEnabled = _enabledManualAssignments.Contains(assignment.Id);
            }
        }

        private bool CanSaveTemplate() => SelectedPersonnels.Count > 0 && SelectedPositions.Count > 0;
        private async Task SaveAsTemplateAsync()
        {
            if (!CanSaveTemplate()) { await _dialogService.ShowWarningAsync("��ǰ���ò��������޷�����Ϊģ��"); return; }
            // �����Զ�������Ի��� (���ơ����͡�������Ĭ�Ͽ���)
            var nameBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "ģ������", Text = $"ģ��_{DateTime.Now:yyyyMMdd}" };
            var typeBox = new Microsoft.UI.Xaml.Controls.ComboBox { ItemsSource = new string[] { "regular", "holiday", "special" }, SelectedIndex = 0, MinWidth = 160 };
            var descBox = new Microsoft.UI.Xaml.Controls.TextBox { AcceptsReturn = true, Height = 80, PlaceholderText = "����(��ѡ)" };
            var defaultSwitch = new Microsoft.UI.Xaml.Controls.ToggleSwitch { Header = "��ΪĬ��", IsOn = false };
            var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8 };
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "����:" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "����:" });
            panel.Children.Add(typeBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "����:" });
            panel.Children.Add(descBox);
            panel.Children.Add(defaultSwitch);
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "����Ϊģ��",
                Content = panel,
                PrimaryButtonText = "����",
                SecondaryButtonText = "ȡ��",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary) return;
            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { await _dialogService.ShowWarningAsync("���Ʋ���Ϊ��"); return; }
            var type = typeBox.SelectedItem?.ToString() ?? "regular";
            if (name.Length > 100) { await _dialogService.ShowWarningAsync("���Ʋ��ܳ���100�ַ�"); return; }
            var createDto = new CreateTemplateDto
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
                EnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList()
            };
            try
            {
                var tpl = await _templateService.CreateAsync(createDto);
                await _dialogService.ShowSuccessAsync($"ģ�� '{tpl.Name}' �ѱ���");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("����ģ��ʧ��", ex);
            }
        }

        // ������5������
        private void BuildSummarySections()
        {
            var sections = new List<SummarySection>();
            // ������Ϣ
            var basic = new SummarySection { Header = "������Ϣ" };
            basic.Lines.Add($"�Ű�����: {ScheduleTitle}");
            basic.Lines.Add($"���ڷ�Χ: {StartDate:yyyy-MM-dd} �� {EndDate:yyyy-MM-dd} (�ϼ� {(EndDate.Date - StartDate.Date).TotalDays + 1} ��) ");
            sections.Add(basic);
            // ��Ա
            var per = new SummarySection { Header = $"������Ա ({SelectedPersonnels.Count})" };
            foreach (var p in SelectedPersonnels.Take(20)) // �������
                per.Lines.Add($"{p.Name} (ID:{p.Id})");
            if (SelectedPersonnels.Count > 20) per.Lines.Add($"... �� {SelectedPersonnels.Count} ��");
            sections.Add(per);
            // ��λ
            var pos = new SummarySection { Header = $"������λ ({SelectedPositions.Count})" };
            foreach (var p in SelectedPositions.Take(20))
                pos.Lines.Add($"{p.Name} (ID:{p.Id})");
            if (SelectedPositions.Count > 20) pos.Lines.Add($"... �� {SelectedPositions.Count} ����λ");
            sections.Add(pos);
            //Լ��
            var cons = new SummarySection { Header = "Լ������" };
            cons.Lines.Add(UseActiveHolidayConfig ? "��Ϣ������: ʹ�õ�ǰ�����" : $"��Ϣ������: �Զ�������ID={SelectedHolidayConfigId}");
            var enabledRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
            var enabledAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
            cons.Lines.Add($"���ö��ڹ���: {enabledRulesCount} ��");
            cons.Lines.Add($"�����ֶ�ָ��: {enabledAssignmentsCount} ��");
            sections.Add(cons);
            // ģ����Ϣ
            if (TemplateApplied && LoadedTemplateId.HasValue)
            {
                sections.Add(new SummarySection { Header = "ģ����Դ", Lines = { $"��Դģ��ID: {LoadedTemplateId}" } });
            }
            // �� BuildSummarySections ������
            SummarySections = new ObservableCollection<SummarySection>(sections);
        }

        // ���Ա���ص�
        partial void OnCurrentStepChanged(int value)
        {
            RefreshCommandStates();
            if (value == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
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
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }
        partial void OnSelectedPositionsChanged(ObservableCollection<PositionDto> value)
        {
            RefreshCommandStates();
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
    }
}
