using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using AutoScheduling3.DTOs;
using System.Linq;
using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using AutoScheduling3.Models.Constraints;
using System.Collections.Generic; // 新增
using AutoScheduling3.Helpers; // 新增 DialogService

namespace AutoScheduling3.Controls
{
    public sealed partial class ScheduleGridControl : UserControl
    {
        public ScheduleGridControl() { Loaded += OnLoaded; }

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

        //供外部监听的事件
        public event EventHandler<ShiftDto>? ShiftChanged; // 任意班次编辑后触发
        public event EventHandler<IReadOnlyList<ConflictDto>>? ConflictsRecomputed; // 冲突重算后触发

        // 可选：传入启用的定岗规则与手动指定用于合法性校验（依赖外部绑定)
        public List<FixedPositionRule>? ActiveFixedRules { get; set; }
        public List<ManualAssignment>? ActiveManualAssignments { get; set; }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScheduleGridControl c && c.IsLoaded)
                c.BuildGrid();
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => BuildGrid();

        // Cell model (public for XAML binding if needed later)
        public class CellModel
        {
            public ShiftDto? Shift { get; set; }
            public DateTime Date { get; set; }
            public PositionDto? Position { get; set; }
            public int PeriodIndex { get; set; }
            public bool HasConflict { get; set; }
            public ConflictDto? Conflict { get; set; }
        }
        private readonly ObservableCollection<CellModel> _cells = new();

        private void BuildGrid()
        {
            _cells.Clear();
            if (Schedule == null || Positions == null) return; // postpone until data ready
            var shiftsByKey = Schedule.Shifts.GroupBy(s => (s.StartTime.Date, s.PositionId, s.PeriodIndex))
            .ToDictionary(g => g.Key, g => g.First());
            var totalDays = (Schedule.EndDate.Date - Schedule.StartDate.Date).Days +1;
            var dates = Enumerable.Range(0, totalDays).Select(i => Schedule.StartDate.Date.AddDays(i)).ToList();
            foreach (var pos in Positions)
            {
                foreach (var date in dates)
                {
                    for (int period =0; period <12; period++)
                    {
                        shiftsByKey.TryGetValue((date, pos.Id, period), out var shift);
                        _cells.Add(new CellModel
                        {
                            Shift = shift,
                            Date = date,
                            Position = pos,
                            PeriodIndex = period,
                            HasConflict = false
                        });
                    }
                }
            }
            // Bind repeater if present
            if (this.FindName("GridRepeater") is ItemsRepeater repeater)
            {
                repeater.ItemsSource = _cells;
                if (repeater.Layout == null)
                {
                    repeater.Layout = new UniformGridLayout { Orientation = Orientation.Vertical }; // ItemsPerRow removed for compatibility
                }
                if (repeater.ItemTemplate == null)
                {
                    repeater.ItemTemplate = BuildTemplate();
                }
            }
            RecomputeConflicts();
        }
        private DataTemplate BuildTemplate()
        {
            var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>\n <Border Padding='4' Margin='2' CornerRadius='4' BorderThickness='1' x:Name='CellBorder'>\n <StackPanel Spacing='2'>\n <TextBlock Text='{Binding Position.Name}' FontSize='10'/>\n <TextBlock Text='{Binding Shift.PersonnelName}' FontSize='12' FontWeight='SemiBold'/>\n <TextBlock Text='{Binding Date, StringFormat=MM-dd}' FontSize='10'/>\n </StackPanel>\n </Border>\n </DataTemplate>";
            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }

        private void RecomputeConflicts()
        {
            if (Schedule == null) return;
            var conflicts = new List<ConflictDto>();
            // 基础: 未分配单元格
            foreach (var cell in _cells)
            {
                if (cell.Shift == null)
                {
                    cell.HasConflict = true;
                    cell.Conflict = new ConflictDto
                    {
                        Type = "unassigned",
                        Message = "未分配人员",
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
            // 技能/规则冲突与连续时段简单判定
            if (Personnels != null && Positions != null)
            {
                var personnelDict = Personnels.ToDictionary(p => p.Id);
                var positionDict = Positions.ToDictionary(p => p.Id);
                foreach (var cell in _cells.Where(c => c.Shift != null))
                {
                    var shift = cell.Shift!;
                    if (!personnelDict.TryGetValue(shift.PersonnelId, out var person) || !positionDict.TryGetValue(shift.PositionId, out var pos)) continue;
                    // 技能不匹配
                    bool skillOk = pos.RequiredSkillIds.All(id => person.SkillIds.Contains(id));
                    if (!skillOk)
                    {
                        cell.HasConflict = true;
                        cell.Conflict = new ConflictDto
                        {
                            Type = "hard",
                            Message = "技能不匹配",
                            PositionId = pos.Id,
                            PersonnelId = person.Id,
                            StartTime = shift.StartTime,
                            EndTime = shift.EndTime,
                            PeriodIndex = shift.PeriodIndex
                        };
                        conflicts.Add(cell.Conflict);
                        continue; // 优先显示技能冲突
                    }
                    // 定岗规则检查(如果有活动规则)
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
                                    Message = "违反定岗规则",
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
            // 简易连续时段冲突（同一人员相邻时段同一天)
            var groupedByPersonDate = _cells.Where(c => c.Shift != null).GroupBy(c => (c.Shift!.PersonnelId, c.Date.Date));
            foreach (var grp in groupedByPersonDate)
            {
                var ordered = grp.OrderBy(c => c.PeriodIndex).ToList();
                for (int i =1; i < ordered.Count; i++)
                {
                    if (ordered[i].PeriodIndex - ordered[i -1].PeriodIndex ==1)
                    {
                        // 标记软冲突（可调整)
                        var cell = ordered[i];
                        if (!cell.HasConflict)
                        {
                            cell.HasConflict = true;
                            cell.Conflict = new ConflictDto
                            {
                                Type = "soft",
                                Message = "连续时段排班",
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
            Schedule.Conflicts = conflicts; // 更新 DTO 冲突集合
            ConflictsRecomputed?.Invoke(this, conflicts);
        }

        private CellModel? _dragSource;
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (IsReadOnly) return;
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.OriginalSource is FrameworkElement fe && fe.DataContext is CellModel cell)
            {
                _dragSource = cell;
            }
        }
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (IsReadOnly) { _dragSource = null; return; }
            if (_dragSource != null && e.OriginalSource is FrameworkElement fe && fe.DataContext is CellModel target && target != _dragSource)
            {
                TrySwapCells(_dragSource, target);
            }
            _dragSource = null;
        }

        private async void TrySwapCells(CellModel a, CellModel b)
        {
            if (Schedule == null || a.Shift == null || b.Shift == null) return;
            // 合法性校验：技能 + 定岗规则 + 同人员一晚多个夜哨简单检查
            if (!ValidateSwap(a, b, out var warn))
            {
                await new DialogService().ShowWarningAsync(warn);
                return;
            }
            //交换人员
            var tmpPersonId = a.Shift.PersonnelId;
            var tmpPersonName = a.Shift.PersonnelName;
            a.Shift.PersonnelId = b.Shift.PersonnelId;
            a.Shift.PersonnelName = b.Shift.PersonnelName;
            b.Shift.PersonnelId = tmpPersonId;
            b.Shift.PersonnelName = tmpPersonName;
            ShiftChanged?.Invoke(this, a.Shift);
            ShiftChanged?.Invoke(this, b.Shift);
            RecomputeConflicts();
            BuildGrid(); // 重绘
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
            // 技能校验
            bool paFitsPosB = posb.RequiredSkillIds.All(id => pa.SkillIds.Contains(id));
            bool pbFitsPosA = posa.RequiredSkillIds.All(id => pb.SkillIds.Contains(id));
            if (!paFitsPosB || !pbFitsPosA)
            {
                warn = "技能不匹配，无法交换";
                return false;
            }
            // 定岗规则校验
            if (ActiveFixedRules?.Any() == true)
            {
                bool ruleOk = CheckFixedRule(pa.Id, posb.Id, b.PeriodIndex) && CheckFixedRule(pb.Id, posa.Id, a.PeriodIndex);
                if (!ruleOk)
                {
                    warn = "定岗规则不允许此交换";
                    return false;
                }
            }
            // 夜哨唯一(简单: period11/0/1/2视为夜哨，交换后同一人员在这些时段出现>1则警告)
            int[] night = {11,0,1,2};
            if (night.Contains(a.PeriodIndex) || night.Contains(b.PeriodIndex))
            {
                //统计 b 人员在当天夜哨数量 (排除当前两个，模拟交换后)
                int countPaNight = _cells.Count(c => c.Shift != null && c.Shift.PersonnelId == pa.Id && night.Contains(c.PeriodIndex) && c != a && c != b);
                int countPbNight = _cells.Count(c => c.Shift != null && c.Shift.PersonnelId == pb.Id && night.Contains(c.PeriodIndex) && c != a && c != b);
                if (countPaNight >=1 || countPbNight >=1)
                {
                    warn = "夜哨唯一规则可能被破坏";
                    //允许但提示
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

        //右键菜单
        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            base.OnRightTapped(e);
            if (IsReadOnly) return;
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is CellModel cell)
            {
                var menu = new MenuFlyout();
                if (cell.Shift != null)
                {
                    var clearItem = new MenuFlyoutItem { Text = "清除班次" };
                    clearItem.Click += (s, args) => { Schedule!.Shifts.Remove(cell.Shift!); cell.Shift = null; ShiftChanged?.Invoke(this, null); RecomputeConflicts(); BuildGrid(); };
                    menu.Items.Add(clearItem);
                }
                var infoItem = new MenuFlyoutItem { Text = "详情" };
                infoItem.Click += (s, args) => { new DialogService().ShowMessageAsync("班次详情", cell.Shift == null ? "未分配" : $"人员: {cell.Shift.PersonnelName}\n哨位: {cell.Position?.Name}\n时间: {cell.Shift.StartTime:yyyy-MM-dd HH:mm} - {cell.Shift.EndTime:HH:mm}"); };
                menu.Items.Add(infoItem);
                menu.ShowAt(this);
            }
        }
    }
}
