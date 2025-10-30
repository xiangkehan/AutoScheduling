using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Controls
{
    public sealed partial class PositionCard : UserControl
    {
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(nameof(Position), typeof(PositionDto), typeof(PositionCard), new PropertyMetadata(null));

        public PositionDto Position
        {
            get => (PositionDto)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public PositionCard()
        {
            this.InitializeComponent();
        }
    }
}
