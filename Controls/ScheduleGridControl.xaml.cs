using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using AutoScheduling3.DTOs;
using System.Linq;
using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using AutoScheduling3.Models.Constraints;
using System.Collections.Generic;
using AutoScheduling3.Helpers;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AutoScheduling3.Controls
{
    public sealed partial class ScheduleGridControl : UserControl
    {
        public ScheduleGridControl() 
        { 
            this.InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            
            // 初始化虚拟化支持
            _virtualizedCells = new ObservableCollection<CellModel>();
            _virtualizedCells.CollectionChanged += OnVirtualizedCellsChanged;
        }

        public static readonly DependencyProperty ScheduleProperty = DependencyProperty.Register(nameof(Schedule), typeof(ScheduleDto), typeof(ScheduleGridControl), new PropertyMetadata(null, OnDataChanged));
        public ScheduleDto? Schedule { get => (ScheduleDto?)GetValue(ScheduleProperty); set => SetValue(ScheduleProperty, value); }
        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(nameof(Positions), typeof(ObservableCollection<PositionDto>), typeof(ScheduleGridControl), new PropertyMetadata(null, OnDataChanged));
        public ObservableCollection<PositionDto>? Positions { get => (ObservableCollection<PositionDto>?)GetValue(PositionsProperty); set => SetValue(PositionsProperty, value); }
        public static readonly DependencyProperty PersonnelsProperty = DependencyProperty.Register(nameof(Personnels), typeof(ObservableCollection<PersonnelDto>), typeof(ScheduleGridControl), new PropertyMetadata(null));
        public ObservableCollection<PersonnelDto>? Personnels { get => (ObservableCollection<PersonnelDto>?)GetValue(PersonnelsProperty); set => SetValue(PersonnelsProperty, value); }
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ScheduleGridControl), new PropertyMetadata(false));
        public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
        public static readonly DependencyProperty ShowConflictsProperty = DependencyProperty.Register(nameof(ShowConflicts), typeof(bool), typeof(ScheduleGridControl), new PropertyMetadata(true));
        public bool ShowConflicts { get => (bool)GetValue(ShowConflictsProperty); set => SetValue(ShowConflictsProperty, value); }
        
        public static readonly DependencyProperty EnableVirtualizationProperty = DependencyProperty.Register(nameof(EnableVirtualization), typeof(bool), typeof(ScheduleGridControl), new PropertyMetadata(true));
        public bool EnableVirtualization { get => (bool)GetValue(EnableVirtualizationProperty); set => SetValue(EnableVirtualizationProperty, value); }
        
        public static readonly DependencyProperty MaxVisibleRowsProperty = DependencyProperty.Register(nameof(MaxVisibleRows), typeof(int), typeof(ScheduleGridControl), new PropertyMetadata(20));
        public int MaxVisibleRows { get => (int)GetValue(MaxVisibleRowsProperty); set => SetValue(MaxVisibleRowsProperty, value); }
        
        public static readonly DependencyProperty MaxVisibleColumnsProperty = DependencyProperty.Register(nameof(MaxVisibleColumns), typeof(int), typeof(ScheduleGridControl), new PropertyMetadata(12));
        public int MaxVisibleColumns { get => (int)GetValue(MaxVisibleColumnsProperty); set => SetValue(MaxVisibleColumnsProperty, value); }

        // External events for data updates
        public event EventHandler<ShiftDto>? ShiftChanged; // Triggered after shift editing
        public event EventHandler<IReadOnlyList<ConflictDto>>? ConflictsRecomputed; // Triggered after conflict recomputation
        
        // Computed properties for UI binding
        public int TotalCells => _cells.Count;
        public int ConflictCount => Schedule?.Conflicts?.Count ?? 0;
        public bool HasConflicts => ConflictCount > 0;

        // ��ѡ���������õĶ��ڹ������ֶ�ָ�����ںϷ���У�飨�����ⲿ��)
        public List<FixedPositionRule>? ActiveFixedRules { get; set; }
        public List<ManualAssignment>? ActiveManualAssignments { get; set; }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScheduleGridControl c && c.IsLoaded)
                c.BuildGrid();
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => BuildGrid();

        // Enhanced Cell model with virtualization and drag-drop support
        public class CellModel : INotifyPropertyChanged
        {
            private ShiftDto? _shift;
            private bool _hasConflict;
            private ConflictDto? _conflict;
            private bool _isDragSource;
            private bool _isDragTarget;

            public ShiftDto? Shift 
            { 
                get => _shift; 
                set 
                { 
                    _shift = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(IsEmpty));
                } 
            }
            
            public DateTime Date { get; set; }
            public PositionDto? Position { get; set; }
            public int PeriodIndex { get; set; }
            
            public bool HasConflict 
            { 
                get => _hasConflict; 
                set 
                { 
                    _hasConflict = value; 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConflictBrush));
                    OnPropertyChanged(nameof(ConflictDisplayText));
                } 
            }
            
            public ConflictDto? Conflict 
            { 
                get => _conflict; 
                set 
                { 
                    _conflict = value; 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConflictBrush));
                    OnPropertyChanged(nameof(ConflictDisplayText));
                } 
            }

            public bool IsDragSource 
            { 
                get => _isDragSource; 
                set 
                { 
                    _isDragSource = value; 
                    OnPropertyChanged(); 
                } 
            }
            
            public bool IsDragTarget 
            { 
                get => _isDragTarget; 
                set 
                { 
                    _isDragTarget = value; 
                    OnPropertyChanged(); 
                } 
            }

            // Computed properties for UI binding
            public bool IsEmpty => Shift == null;
            public bool IsNightShift => PeriodIndex >= 22 || PeriodIndex <= 6; // 22:00-06:59 as night shift
            public string PeriodDisplayText => $"{PeriodIndex * 2:D2}:00-{(PeriodIndex * 2 + 2) % 24:D2}:00";
            
            public SolidColorBrush ConflictBrush
            {
                get
                {
                    if (!HasConflict || Conflict == null) return new SolidColorBrush(Colors.Transparent);
                    
                    return Conflict.Type switch
                    {
                        "hard" => new SolidColorBrush(Colors.Red),
                        "soft" => new SolidColorBrush(Colors.Orange),
                        "unassigned" => new SolidColorBrush(Colors.Gray),
                        _ => new SolidColorBrush(Colors.Yellow)
                    };
                }
            }
            
            public string ConflictDisplayText
            {
                get
                {
                    if (!HasConflict || Conflict == null) return string.Empty;
                    
                    return Conflict.Type switch
                    {
                        "hard" => "硬约束",
                        "soft" => "软约束", 
                        "unassigned" => "未分配",
                        _ => "冲突"
                    };
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Collections for virtualization
        private readonly ObservableCollection<CellModel> _cells = new();
        private readonly ObservableCollection<CellModel> _virtualizedCells;
        private readonly ObservableCollection<string> _timeHeaders = new();
        private readonly ObservableCollection<string> _positionHeaders = new();

        // Drag and drop state
        private CellModel? _dragSource;
        private readonly List<CellModel> _dragTargets = new();
        
        // Virtualization parameters
        private int _visibleRows = 20;
        private int _visibleColumns = 12;
        private int _scrollOffsetRow = 0;
        private int _scrollOffsetColumn = 0;

        private async void BuildGrid()
        {
            if (Schedule == null || Positions == null) return;

            // Show loading indicator
            if (LoadingRing != null) LoadingRing.IsActive = true;
            if (EmptyStatePanel != null) EmptyStatePanel.Visibility = Visibility.Collapsed;

            await Task.Run(() =>
            {
                _cells.Clear();
                
                var shiftsByKey = Schedule.Shifts.GroupBy(s => (s.StartTime.Date, s.PositionId, s.PeriodIndex))
                    .ToDictionary(g => g.Key, g => g.First());
                
                var totalDays = (Schedule.EndDate.Date - Schedule.StartDate.Date).Days + 1;
                var dates = Enumerable.Range(0, totalDays).Select(i => Schedule.StartDate.Date.AddDays(i)).ToList();
                
                // Build time headers
                DispatcherQueue.TryEnqueue(() =>
                {
                    _timeHeaders.Clear();
                    for (int period = 0; period < 12; period++)
                    {
                        _timeHeaders.Add($"{period * 2:D2}:00-{(period * 2 + 2) % 24:D2}:00");
                    }
                });

                // Build position headers
                DispatcherQueue.TryEnqueue(() =>
                {
                    _positionHeaders.Clear();
                    foreach (var pos in Positions)
                    {
                        _positionHeaders.Add(pos.Name);
                    }
                });

                // Build cells
                foreach (var pos in Positions)
                {
                    foreach (var date in dates)
                    {
                        for (int period = 0; period < 12; period++)
                        {
                            shiftsByKey.TryGetValue((date, pos.Id, period), out var shift);
                            var cell = new CellModel
                            {
                                Shift = shift,
                                Date = date,
                                Position = pos,
                                PeriodIndex = period,
                                HasConflict = false
                            };
                            
                            DispatcherQueue.TryEnqueue(() => _cells.Add(cell));
                        }
                    }
                }
            });

            // Update UI bindings
            UpdateVirtualizedView();
            
            // Bind headers
            if (TimeHeaderRepeater != null) TimeHeaderRepeater.ItemsSource = _timeHeaders;
            if (PositionHeaderRepeater != null) PositionHeaderRepeater.ItemsSource = _positionHeaders;
            
            // Configure grid layout
            if (GridLayout != null)
            {
                GridLayout.MaximumRowsOrColumns = _timeHeaders.Count;
            }

            // Hide loading indicator
            if (LoadingRing != null) LoadingRing.IsActive = false;
            
            // Show empty state if no data
            if (_cells.Count == 0 && EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
            }

            await RecomputeConflictsAsync();
        }
        // Virtualization support
        private void UpdateVirtualizedView()
        {
            if (_cells.Count == 0) return;

            _virtualizedCells.Clear();
            
            // Calculate visible range based on scroll position and viewport size
            var startRow = Math.Max(0, _scrollOffsetRow);
            var endRow = Math.Min(_positionHeaders.Count, startRow + _visibleRows);
            var startCol = Math.Max(0, _scrollOffsetColumn);
            var endCol = Math.Min(_timeHeaders.Count, startCol + _visibleColumns);

            // Add visible cells to virtualized collection
            for (int row = startRow; row < endRow; row++)
            {
                for (int col = startCol; col < endCol; col++)
                {
                    var cellIndex = row * _timeHeaders.Count + col;
                    if (cellIndex < _cells.Count)
                    {
                        _virtualizedCells.Add(_cells[cellIndex]);
                    }
                }
            }

            // Bind to main repeater
            if (MainGridRepeater != null)
            {
                MainGridRepeater.ItemsSource = _virtualizedCells;
            }
        }

        private void OnVirtualizedCellsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle virtualized collection changes if needed
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Recalculate visible items based on new size
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                _visibleColumns = Math.Max(1, (int)(e.NewSize.Width / 120)); // 120 is min cell width
                _visibleRows = Math.Max(1, (int)(e.NewSize.Height / 60));    // 60 is min cell height
                UpdateVirtualizedView();
            }
        }

        private async Task RecomputeConflictsAsync()
        {
            if (Schedule == null) return;
            var conflicts = new List<ConflictDto>();
            // ����: δ���䵥Ԫ��
            foreach (var cell in _cells)
            {
                if (cell.Shift == null)
                {
                    cell.HasConflict = true;
                    cell.Conflict = new ConflictDto
                    {
                        Type = "unassigned",
                        Message = "δ������Ա",
                        PositionId = cell.Position?.Id,
                        StartTime = cell.Date.AddHours(cell.PeriodIndex *2),
                        EndTime = cell.Date.AddHours(cell.PeriodIndex *2 +2),
                        PeriodIndex = cell.PeriodIndex
                    };
                    conflicts.Add(cell.Conflict);
                    continue;
                }
                cell.HasConflict = false;
                cell.Conflict = null;
            }
            // ����/�����ͻ������ʱ�μ��ж�
            if (Personnels != null && Positions != null)
            {
                var personnelDict = Personnels.ToDictionary(p => p.Id);
                var positionDict = Positions.ToDictionary(p => p.Id);
                foreach (var cell in _cells.Where(c => c.Shift != null))
                {
                    var shift = cell.Shift!;
                    if (!personnelDict.TryGetValue(shift.PersonnelId, out var person) || !positionDict.TryGetValue(shift.PositionId, out var pos)) continue;
                    // ���ܲ�ƥ��
                    bool skillOk = pos.RequiredSkillIds.All(id => person.SkillIds.Contains(id));
                    if (!skillOk)
                    {
                        cell.HasConflict = true;
                        cell.Conflict = new ConflictDto
                        {
                            Type = "hard",
                            Message = "���ܲ�ƥ��",
                            PositionId = pos.Id,
                            PersonnelId = person.Id,
                            StartTime = shift.StartTime,
                            EndTime = shift.EndTime,
                            PeriodIndex = shift.PeriodIndex
                        };
                        conflicts.Add(cell.Conflict);
                        continue; // ������ʾ���ܳ�ͻ
                    }
                    // ���ڹ�����(����л����)
                    if (ActiveFixedRules?.Any() == true)
                    {
                        var personRules = ActiveFixedRules.Where(r => r.PersonalId == person.Id && r.IsEnabled).ToList();
                        if (personRules.Any())
                        {
                            bool positionAllowed = personRules.All(r => r.AllowedPositionIds.Count ==0 || r.AllowedPositionIds.Contains(pos.Id));
                            bool periodAllowed = personRules.All(r => r.AllowedPeriods.Count ==0 || r.AllowedPeriods.Contains(shift.PeriodIndex));
                            if (!positionAllowed || !periodAllowed)
                            {
                                cell.HasConflict = true;
                                cell.Conflict = new ConflictDto
                                {
                                    Type = "hard",
                                    Message = "Υ�����ڹ���",
                                    PositionId = pos.Id,
                                    PersonnelId = person.Id,
                                    StartTime = shift.StartTime,
                                    EndTime = shift.EndTime,
                                    PeriodIndex = shift.PeriodIndex
                                };
                                conflicts.Add(cell.Conflict);
                                continue;
                            }
                        }
                    }
                }
            }
            // ��������ʱ�γ�ͻ��ͬһ��Ա����ʱ��ͬһ��)
            var groupedByPersonDate = _cells.Where(c => c.Shift != null).GroupBy(c => (c.Shift!.PersonnelId, c.Date.Date));
            foreach (var grp in groupedByPersonDate)
            {
                var ordered = grp.OrderBy(c => c.PeriodIndex).ToList();
                for (int i =1; i < ordered.Count; i++)
                {
                    if (ordered[i].PeriodIndex - ordered[i -1].PeriodIndex ==1)
                    {
                        // �������ͻ���ɵ���)
                        var cell = ordered[i];
                        if (!cell.HasConflict)
                        {
                            cell.HasConflict = true;
                            cell.Conflict = new ConflictDto
                            {
                                Type = "soft",
                                Message = "����ʱ���Ű�",
                                PositionId = cell.Position?.Id,
                                PersonnelId = cell.Shift!.PersonnelId,
                                StartTime = cell.Shift!.StartTime,
                                EndTime = cell.Shift!.EndTime,
                                PeriodIndex = cell.PeriodIndex
                            };
                            conflicts.Add(cell.Conflict);
                        }
                    }
                }
            }
            Schedule.Conflicts = conflicts; // ���� DTO ��ͻ����
            ConflictsRecomputed?.Invoke(this, conflicts);
        }

        // Enhanced drag-drop event handlers
        private void OnCellPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IsReadOnly) return;
            
            if (sender is FrameworkElement element && element.DataContext is CellModel cell)
            {
                if (e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
                {
                    _dragSource = cell;
                    cell.IsDragSource = true;
                    element.CapturePointer(e.Pointer);
                }
            }
        }

        private void OnCellPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (IsReadOnly || _dragSource == null) 
            {
                ClearDragState();
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is CellModel target && target != _dragSource)
            {
                _ = TrySwapCellsAsync(_dragSource, target);
            }
            
            ClearDragState();
            if (sender is FrameworkElement fe) fe.ReleasePointerCapture(e.Pointer);
        }

        private void OnCellPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_dragSource == null || IsReadOnly) return;
            
            if (sender is FrameworkElement element && element.DataContext is CellModel cell && cell != _dragSource)
            {
                cell.IsDragTarget = true;
                _dragTargets.Add(cell);
            }
        }

        private void OnCellPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is CellModel cell)
            {
                cell.IsDragTarget = false;
                _dragTargets.Remove(cell);
            }
        }

        private void OnCellRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (IsReadOnly) return;
            
            if (sender is FrameworkElement element && element.DataContext is CellModel cell)
            {
                ShowContextMenu(element, cell);
            }
        }

        private void ClearDragState()
        {
            if (_dragSource != null)
            {
                _dragSource.IsDragSource = false;
                _dragSource = null;
            }
            
            foreach (var target in _dragTargets)
            {
                target.IsDragTarget = false;
            }
            _dragTargets.Clear();
        }

        private async Task TrySwapCellsAsync(CellModel a, CellModel b)
        {
            if (Schedule == null || a.Shift == null || b.Shift == null) return;
            // �Ϸ���У�飺���� + ���ڹ��� + ͬ��Աһ�����ҹ�ڼ򵥼��
            if (!ValidateSwap(a, b, out var warn))
            {
                await new DialogService().ShowWarningAsync(warn);
                return;
            }
            //������Ա
            var tmpPersonId = a.Shift.PersonnelId;
            var tmpPersonName = a.Shift.PersonnelName;
            a.Shift.PersonnelId = b.Shift.PersonnelId;
            a.Shift.PersonnelName = b.Shift.PersonnelName;
            b.Shift.PersonnelId = tmpPersonId;
            b.Shift.PersonnelName = tmpPersonName;
            ShiftChanged?.Invoke(this, a.Shift);
            ShiftChanged?.Invoke(this, b.Shift);
            await RecomputeConflictsAsync();
            UpdateVirtualizedView(); // Update virtualized view instead of full rebuild
        }

        private bool ValidateSwap(CellModel a, CellModel b, out string warn)
        {
            warn = string.Empty;
            if (Personnels == null || Positions == null || a.Shift == null || b.Shift == null) return true;
            var pa = Personnels.FirstOrDefault(p => p.Id == a.Shift.PersonnelId);
            var pb = Personnels.FirstOrDefault(p => p.Id == b.Shift.PersonnelId);
            var posa = Positions.FirstOrDefault(p => p.Id == a.Shift.PositionId);
            var posb = Positions.FirstOrDefault(p => p.Id == b.Shift.PositionId);
            if (pa == null || pb == null || posa == null || posb == null) return true;
            // ����У��
            bool paFitsPosB = posb.RequiredSkillIds.All(id => pa.SkillIds.Contains(id));
            bool pbFitsPosA = posa.RequiredSkillIds.All(id => pb.SkillIds.Contains(id));
            if (!paFitsPosB || !pbFitsPosA)
            {
                warn = "���ܲ�ƥ�䣬�޷�����";
                return false;
            }
            // ���ڹ���У��
            if (ActiveFixedRules?.Any() == true)
            {
                bool ruleOk = CheckFixedRule(pa.Id, posb.Id, b.PeriodIndex) && CheckFixedRule(pb.Id, posa.Id, a.PeriodIndex);
                if (!ruleOk)
                {
                    warn = "���ڹ��������˽���";
                    return false;
                }
            }
            // ҹ��Ψһ(��: period11/0/1/2��Ϊҹ�ڣ�������ͬһ��Ա����Щʱ�γ���>1�򾯸�)
            int[] night = {11,0,1,2};
            if (night.Contains(a.PeriodIndex) || night.Contains(b.PeriodIndex))
            {
                //ͳ�� b ��Ա�ڵ���ҹ������ (�ų���ǰ������ģ�⽻����)
                int countPaNight = _cells.Count(c => c.Shift != null && c.Shift.PersonnelId == pa.Id && night.Contains(c.PeriodIndex) && c != a && c != b);
                int countPbNight = _cells.Count(c => c.Shift != null && c.Shift.PersonnelId == pb.Id && night.Contains(c.PeriodIndex) && c != a && c != b);
                if (countPaNight >=1 || countPbNight >=1)
                {
                    warn = "ҹ��Ψһ������ܱ��ƻ�";
                    //��������ʾ
                }
            }
            return true;
        }
        private bool CheckFixedRule(int personId, int positionId, int periodIdx)
        {
            var rules = ActiveFixedRules?.Where(r => r.PersonalId == personId && r.IsEnabled).ToList();
            if (rules == null || rules.Count ==0) return true;
            return rules.All(r => (r.AllowedPositionIds.Count ==0 || r.AllowedPositionIds.Contains(positionId)) && (r.AllowedPeriods.Count ==0 || r.AllowedPeriods.Contains(periodIdx)));
        }

        //�Ҽ��˵�
        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            base.OnRightTapped(e);
            if (IsReadOnly) return;
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is CellModel cell)
            {
                var menu = new MenuFlyout();
                if (cell.Shift != null)
                {
                    var clearItem = new MenuFlyoutItem { Text = "������" };
                    clearItem.Click += async (s, args) => { Schedule!.Shifts.Remove(cell.Shift!); cell.Shift = null; ShiftChanged?.Invoke(this, null); await RecomputeConflictsAsync(); UpdateVirtualizedView(); };
                    menu.Items.Add(clearItem);
                }
                var infoItem = new MenuFlyoutItem { Text = "����" };
                infoItem.Click += (s, args) => { new DialogService().ShowMessageAsync("�������", cell.Shift == null ? "δ����" : $"��Ա: {cell.Shift.PersonnelName}\n��λ: {cell.Position?.Name}\nʱ��: {cell.Shift.StartTime:yyyy-MM-dd HH:mm} - {cell.Shift.EndTime:HH:mm}"); };
                menu.Items.Add(infoItem);
                menu.ShowAt(this);
            }
        }

        // Context menu functionality
        private void ShowContextMenu(FrameworkElement element, CellModel cell)
        {
            var menu = new MenuFlyout();
            
            if (cell.Shift != null)
            {
                var clearItem = new MenuFlyoutItem { Text = "清除分配" };
                clearItem.Click += async (s, args) => 
                { 
                    if (Schedule != null)
                    {
                        Schedule.Shifts.Remove(cell.Shift); 
                        cell.Shift = null; 
                        ShiftChanged?.Invoke(this, null); 
                        await RecomputeConflictsAsync(); 
                        UpdateVirtualizedView(); 
                    }
                };
                menu.Items.Add(clearItem);
                
                var editItem = new MenuFlyoutItem { Text = "编辑分配" };
                editItem.Click += (s, args) => ShowEditDialog(cell);
                menu.Items.Add(editItem);
            }
            else
            {
                var assignItem = new MenuFlyoutItem { Text = "分配人员" };
                assignItem.Click += (s, args) => ShowAssignDialog(cell);
                menu.Items.Add(assignItem);
            }
            
            menu.Items.Add(new MenuFlyoutSeparator());
            
            var infoItem = new MenuFlyoutItem { Text = "详细信息" };
            infoItem.Click += (s, args) => ShowCellInfo(cell);
            menu.Items.Add(infoItem);
            
            if (cell.HasConflict)
            {
                var conflictItem = new MenuFlyoutItem { Text = "冲突详情" };
                conflictItem.Click += (s, args) => ShowConflictInfo(cell);
                menu.Items.Add(conflictItem);
            }
            
            menu.ShowAt(element);
        }

        private async void ShowCellInfo(CellModel cell)
        {
            var info = $"哨位: {cell.Position?.Name ?? "未知"}\n";
            info += $"日期: {cell.Date:yyyy-MM-dd}\n";
            info += $"时段: {cell.PeriodDisplayText}\n";
            
            if (cell.Shift != null)
            {
                info += $"人员: {cell.Shift.PersonnelName}\n";
                info += $"人员ID: {cell.Shift.PersonnelId}\n";
            }
            else
            {
                info += "状态: 未分配\n";
            }
            
            if (cell.HasConflict && cell.Conflict != null)
            {
                info += $"冲突: {cell.Conflict.Message}";
            }
            
            await new DialogService().ShowMessageAsync("单元格信息", info);
        }

        private async void ShowConflictInfo(CellModel cell)
        {
            if (cell.Conflict == null) return;
            
            var info = $"冲突类型: {cell.ConflictDisplayText}\n";
            info += $"描述: {cell.Conflict.Message}\n";
            
            if (cell.Conflict.PersonnelId.HasValue)
            {
                info += $"相关人员ID: {cell.Conflict.PersonnelId}\n";
            }
            
            if (cell.Conflict.PositionId.HasValue)
            {
                info += $"相关哨位ID: {cell.Conflict.PositionId}\n";
            }
            
            await new DialogService().ShowMessageAsync("冲突详情", info);
        }

        private void ShowEditDialog(CellModel cell)
        {
            // TODO: Implement edit dialog for personnel assignment
            // This would show a dialog to change the assigned personnel
        }

        private void ShowAssignDialog(CellModel cell)
        {
            // TODO: Implement assignment dialog for empty cells
            // This would show a dialog to assign personnel to empty cells
        }

        // Toolbar event handlers
        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            BuildGrid();
        }

        private async void OnExportClick(object sender, RoutedEventArgs e)
        {
            // TODO: Implement export functionality
            await new DialogService().ShowMessageAsync("导出", "导出功能正在开发中...");
        }

        // Performance optimization methods
        private void OptimizeForLargeDatasets()
        {
            // Enable virtualization for large datasets
            if (_cells.Count > 1000)
            {
                _visibleRows = Math.Min(_visibleRows, 50);
                _visibleColumns = Math.Min(_visibleColumns, 20);
            }
        }

        // Responsive layout support
        private void UpdateLayoutForScreenSize()
        {
            if (ActualWidth < 800)
            {
                // Compact layout for smaller screens
                if (GridLayout != null)
                {
                    GridLayout.MinItemWidth = 100;
                    GridLayout.MinItemHeight = 50;
                }
            }
            else
            {
                // Standard layout for larger screens
                if (GridLayout != null)
                {
                    GridLayout.MinItemWidth = 120;
                    GridLayout.MinItemHeight = 60;
                }
            }
        }

        // Accessibility support
        private void SetupAccessibility()
        {
            // Set automation properties for screen readers
            AutomationProperties.SetName(this, "排班网格控件");
            AutomationProperties.SetHelpText(this, "显示排班表数据，支持拖拽交换和右键菜单操作");
        }

        // Public methods for external control
        public async Task RefreshAsync()
        {
            BuildGrid();
        }

        public void ClearSelection()
        {
            ClearDragState();
        }

        public void ScrollToCell(int positionIndex, int periodIndex)
        {
            // TODO: Implement scrolling to specific cell
        }

        public void HighlightConflicts(bool highlight)
        {
            ShowConflicts = highlight;
        }
    }
}