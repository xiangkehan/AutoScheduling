using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Controls
{
    public sealed partial class PersonnelCard : UserControl
    {
        public static readonly DependencyProperty PersonnelProperty =
            DependencyProperty.Register(nameof(Personnel), typeof(PersonnelDto), typeof(PersonnelCard), new PropertyMetadata(null));

        public PersonnelDto Personnel
        {
            get => (PersonnelDto)GetValue(PersonnelProperty);
            set => SetValue(PersonnelProperty, value);
        }

        public PersonnelCard()
        {
            this.InitializeComponent();
        }
    }
}
