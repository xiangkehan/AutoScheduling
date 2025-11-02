using AutoScheduling3.DTOs;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;

namespace AutoScheduling3.Controls
{
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
                OnPropertyChanged(nameof(PersonnelIdText));
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

        // Properties for x:Bind StringFormat replacement
        public string DateText => $"{Date:MM-dd}";
        public string PersonnelIdText => Shift != null ? $"ID: {Shift.PersonnelId}" : "";

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
                    "hard" => "Ó²Ô¼Êø",
                    "soft" => "ÈíÔ¼Êø",
                    "unassigned" => "Î´·ÖÅä",
                    _ => "³åÍ»"
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
