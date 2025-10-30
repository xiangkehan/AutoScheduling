using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using System.Collections.ObjectModel;
using AutoScheduling3.DTOs;
using System.Linq;
using System;
using Microsoft.UI.Composition;
using System.Numerics;

namespace AutoScheduling3.Controls
{
 public sealed partial class ScheduleGridControl : UserControl
 {
 public ScheduleGridControl()
 {
 InitializeComponent();
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
 if (d is ScheduleGridControl c)
 {
 c.BuildGrid();
 }
 }

 private void OnLoaded(object sender, RoutedEventArgs e)
 {
 BuildGrid();
 }

 // Basic cell model
 private class CellModel
 {
 public ShiftDto? Shift { get; set; }
 public DateTime Date { get; set; }
 public PositionDto? Position { get; set; }
 public int PeriodIndex { get; set; }
 public bool HasConflict { get; set; }
 }

 private readonly ObservableCollection<object> _cells = new();

 private void BuildGrid()
 {
 _cells.Clear();
 if (Schedule == null || Positions == null) { GridRepeater.ItemsSource = _cells; return; }
 var shiftsByKey = Schedule.Shifts.GroupBy(s => (s.startTime.Date, s.positionId, s.periodIndex)).ToDictionary(g => g.Key, g => g.First());
 var dates = Enumerable.Range(0, (Schedule.endDate - Schedule.startDate).Days +1).Select(i => Schedule.startDate.AddDays(i)).ToList();
 foreach (var pos in Positions)
 {
 foreach (var date in dates)
 {
 for (int period =0; period <12; period++)
 {
 shiftsByKey.TryGetValue((date.Date, pos.id, period), out var shift);
 _cells.Add(new CellModel
 {
 Shift = shift,
 Date = date,
 Position = pos,
 PeriodIndex = period,
 HasConflict = false // placeholder
 });
 }
 }
 }
 GridRepeater.ItemsSource = _cells;
 if (GridRepeater.Layout == null)
 {
 GridRepeater.Layout = new UniformGridLayout { Orientation = Orientation.Horizontal, ItemsPerRow =12 }; // simplistic; real impl should custom layout
 }
 GridRepeater.ItemTemplate = BuildTemplate();
 }

 private DataTemplate BuildTemplate()
 {
 var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
 <Border Padding='4' Margin='2' CornerRadius='4' BorderThickness='1'>
 <StackPanel Spacing='2'>
 <TextBlock Text='{Binding Position.name}' FontSize='10'/> 
 <TextBlock Text='{Binding Shift.personnelName}' FontSize='12' FontWeight='SemiBold'/>
 <TextBlock Text='{Binding Date, StringFormat=MM-dd}' FontSize='10'/>
 </StackPanel>
 </Border>
 </DataTemplate>";
 return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
 }
 }
}
