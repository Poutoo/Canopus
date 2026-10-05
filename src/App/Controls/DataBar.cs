using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Animations;

namespace Canopus.App.Controls;

/// <summary>
/// Horizontal data bar: a track and a fill scaled on X from the left edge, so the value
/// can be animated on the compositor without relayout. Only allowed on levels 1 and 2.
/// </summary>
public sealed class DataBar : Grid
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(DataBar),
        new PropertyMetadata(0d, (d, e) => ((DataBar)d).OnValueChanged((double)e.OldValue, (double)e.NewValue)));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(DataBar),
        new PropertyMetadata(null, (d, e) => ((DataBar)d)._fill.Background = (Brush?)e.NewValue));

    private readonly Border _fill = new();

    public DataBar()
    {
        Background = (Brush)Application.Current.Resources["DataTrackBrush"];
        Children.Add(_fill);
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => _fill.CornerRadius = CornerRadius);
        SetFillScale(0);
    }

    /// <summary>Filled fraction, from 0 to 1.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Fills the bar from zero, as part of the page entrance.</summary>
    public void PlayEntrance(TimeSpan delay) =>
        Motion.AnimateScalar(_fill, "Scale.X", 0f, Target(Value), Motion.Duration("MotionDurationBar"), delay, "MotionEasingEnter");

    // The 1.5 s refresh moves bars only for a visible change, on a window that is on
    // screen and a page that is displayed; otherwise the value is set without animation.
    private void OnValueChanged(double oldValue, double newValue)
    {
        double minDelta = Motion.Value("MotionBarMinDelta") / 100;
        bool animate = Math.Abs(newValue - oldValue) >= minDelta && Motion.IsWindowPresented && IsDisplayed();
        TimeSpan duration = animate ? Motion.Duration("MotionDurationBar") : TimeSpan.Zero;
        Motion.AnimateScalar(_fill, "Scale.X", null, Target(newValue), duration, TimeSpan.Zero, "MotionEasingEnter");
    }

    private bool IsDisplayed()
    {
        for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is UIElement { Visibility: Visibility.Collapsed })
                return false;
        }
        return XamlRoot is not null;
    }

    private static float Target(double fraction) => (float)Math.Clamp(fraction, 0, 1);

    private void SetFillScale(double fraction) =>
        Motion.VisualOf(_fill).Scale = new Vector3(Target(fraction), 1, 1);
}
