using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace AutoScheduling3.Controls
{
    public sealed partial class ErrorState : UserControl
    {
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(ErrorState), new PropertyMetadata(false, OnIsVisibleChanged));

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ErrorState), new PropertyMetadata("加载失败"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(ErrorState), new PropertyMetadata("发生了一个错误，请重试。"));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty RetryButtonTextProperty =
            DependencyProperty.Register(nameof(RetryButtonText), typeof(string), typeof(ErrorState), new PropertyMetadata("重试"));

        public string RetryButtonText
        {
            get => (string)GetValue(RetryButtonTextProperty);
            set => SetValue(RetryButtonTextProperty, value);
        }

        public static readonly DependencyProperty RetryCommandProperty =
            DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(ErrorState), new PropertyMetadata(null));

        public ICommand RetryCommand
        {
            get => (ICommand)GetValue(RetryCommandProperty);
            set => SetValue(RetryCommandProperty, value);
        }

        public static readonly DependencyProperty DetailsButtonTextProperty =
            DependencyProperty.Register(nameof(DetailsButtonText), typeof(string), typeof(ErrorState), new PropertyMetadata(null));

        public string DetailsButtonText
        {
            get => (string)GetValue(DetailsButtonTextProperty);
            set => SetValue(DetailsButtonTextProperty, value);
        }

        public static readonly DependencyProperty DetailsCommandProperty =
            DependencyProperty.Register(nameof(DetailsCommand), typeof(ICommand), typeof(ErrorState), new PropertyMetadata(null));

        public ICommand DetailsCommand
        {
            get => (ICommand)GetValue(DetailsCommandProperty);
            set => SetValue(DetailsCommandProperty, value);
        }

        public ErrorState()
        {
            this.InitializeComponent();
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ErrorState)d;
            control.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
