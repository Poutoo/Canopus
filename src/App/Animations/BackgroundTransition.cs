using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Canopus.App.Animations;

/// <summary>Fades background changes (hover, pressed) over the fast motion token.</summary>
public static class BackgroundTransition
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(BackgroundTransition), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;

        TimeSpan duration = Motion.Duration("MotionDurationFast");
        panel.BackgroundTransition = e.NewValue is true && duration > TimeSpan.Zero
            ? new BrushTransition { Duration = duration }
            : null;
    }
}
