using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using AutoScheduling3.DTOs;
using System.Linq;
using System;

namespace AutoScheduling3.Controls
{
    public sealed partial class ScheduleGridControl : UserControl
    {
        // Remove InitializeComponent (not needed when no codegen partial produced by XAML compiler yet)
        public ScheduleGridControl()
        {
            Loaded += OnLoaded;
        }
        // Dependency Properties
        public static readonly DependencyProperty ScheduleProperty =
        DependencyProperty.Register(nameof(Schedule), typeof(ScheduleDto), typeof(ScheduleGridControl), new PropertyMetadata(null, OnDataChanged));
        public ScheduleDto? Schedule
        {
            get => (ScheduleDto?)GetValue(ScheduleProperty);
            set => SetValue(ScheduleProperty, value);
        }
        public static readonly DependencyProperty PositionsProperty =
        DependencyProperty.Register(nameof(Positions), typeof(ObservableCollection<PositionDto>), typeof(ScheduleGridControl), new PropertyMetadata(null, OnDataChanged));
        public ObservableCollection<PositionDto>? Positions
        {
            get => (ObservableCollection<PositionDto>?)GetValue(PositionsProperty);
            set => SetValue(PositionsProperty, value);
        }
        public static readonly DependencyProperty PersonnelsProperty =
        DependencyProperty.Register(nameof(Personnels), typeof(ObservableCollection<PersonnelDto>), typeof(ScheduleGridControl), new PropertyMetadata(null));
        public ObservableCollection<PersonnelDto>? Personnels
        {
            get => (ObservableCollection<PersonnelDto>?)GetValue(PersonnelsProperty);
            set => SetValue(PersonnelsProperty, value);
        }
        public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ScheduleGridControl), new PropertyMetadata(false));
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }
        public static readonly DependencyProperty ShowConflictsProperty =
        DependencyProperty.Register(nameof(ShowConflicts), typeof(bool), typeof(ScheduleGridControl), new PropertyMetadata(true));
        public bool ShowConflicts
        {
            get => (bool)GetValue(ShowConflictsProperty);
            set => SetValue(ShowConflictsProperty, value);
        }
        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScheduleGridControl c && c.IsLoaded)
            {
                c.BuildGrid();
            }
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
        }
        private DataTemplate BuildTemplate()
        {
            var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>\n <Border Padding='4' Margin='2' CornerRadius='4' BorderThickness='1' BorderBrush='{ThemeResource CardStrokeColorDefaultBrush}' Background='{ThemeResource CardBackgroundFillColorDefaultBrush}'>\n <StackPanel Spacing='2'>\n <TextBlock Text='{Binding Position.Name}' FontSize='10'/>\n <TextBlock Text='{Binding Shift.PersonnelName}' FontSize='12' FontWeight='SemiBold'/>\n <TextBlock Text='{Binding Date, StringFormat=MM-dd}' FontSize='10'/>\n </StackPanel>\n </Border>\n </DataTemplate>";
            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }
    }
}
