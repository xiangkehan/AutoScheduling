using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Microsoft.UI.Xaml.Markup; // for XamlReader

namespace AutoScheduling3.Controls
{
    public sealed partial class EmptyState : UserControl
    {
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(EmptyState), new PropertyMetadata(false, OnIsVisibleChanged));
        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public static readonly DependencyProperty IconGlyphProperty =
            DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(EmptyState), new PropertyMetadata("\uE825"));
        public string IconGlyph
        {
            get => (string)GetValue(IconGlyphProperty);
            set => SetValue(IconGlyphProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata("暂无数据"));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(EmptyState), new PropertyMetadata("您还没有添加任何内容。"));
        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(EmptyState), new PropertyMetadata(null));
        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        public static readonly DependencyProperty ButtonCommandProperty =
            DependencyProperty.Register(nameof(ButtonCommand), typeof(ICommand), typeof(EmptyState), new PropertyMetadata(null));
        public ICommand ButtonCommand
        {
            get => (ICommand)GetValue(ButtonCommandProperty);
            set => SetValue(ButtonCommandProperty, value);
        }

        public EmptyState()
        {
            // Manual XAML load fallback: ensure component tree if InitializeComponent not generated
            if (Content == null)
            {
                var xaml = @"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Spacing='16' Padding='24'>
                    <FontIcon Glyph='{x:Bind IconGlyph, Mode=OneWay}' FontSize='64' HorizontalAlignment='Center' />
                    <TextBlock Text='{x:Bind Title, Mode=OneWay}' HorizontalAlignment='Center' FontSize='16' FontWeight='SemiBold' />
                    <TextBlock Text='{x:Bind Subtitle, Mode=OneWay}' HorizontalAlignment='Center' FontSize='13' TextWrapping='Wrap' TextAlignment='Center' />
                </StackPanel>";
                Content = (UIElement)XamlReader.Load(xaml);
            }
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (EmptyState)d;
            control.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
