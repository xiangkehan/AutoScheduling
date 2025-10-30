using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Controls
{
    public sealed partial class LoadingIndicator : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingIndicator), new PropertyMetadata(false, OnIsLoadingChanged));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(LoadingIndicator), new PropertyMetadata("正在加载中..."));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public LoadingIndicator()
        {
            this.InitializeComponent();
        }

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LoadingIndicator)d;
            control.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
