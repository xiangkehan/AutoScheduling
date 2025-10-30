using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Hosting;
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

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(PositionCard), new PropertyMetadata(false, OnSelectionChanged));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static readonly DependencyProperty ShowActionsProperty =
            DependencyProperty.Register(nameof(ShowActions), typeof(bool), typeof(PositionCard), new PropertyMetadata(false));

        public bool ShowActions
        {
            get => (bool)GetValue(ShowActionsProperty);
            set => SetValue(ShowActionsProperty, value);
        }

        private Visual? _rootVisual;
        private bool _isPointerOver;

        public PositionCard()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _rootVisual = ElementCompositionPreview.GetElementVisual(this);
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOver = true;
            UpdateHoverAnimation();
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOver = false;
            UpdateHoverAnimation();
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PositionCard card)
            {
                card.UpdateHoverAnimation();
            }
        }

        private void UpdateHoverAnimation()
        {
            if (_rootVisual is null) return;
            var compositor = _rootVisual.Compositor;
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Duration = System.TimeSpan.FromMilliseconds(150);
            offsetAnimation.InsertKeyFrame(1.0f, new System.Numerics.Vector3(0, _isPointerOver || IsSelected ? -4f :0f,0));
            _rootVisual.StartAnimation("Offset", offsetAnimation);
        }
    }
}
