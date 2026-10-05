using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Canopus.App.Animations;

/// <summary>Scales a button down slightly while it is pressed.</summary>
public static class PressFeedback
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(PressFeedback), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static readonly PointerEventHandler PressedHandler = (sender, _) => ScaleTo((UIElement)sender, (float)Motion.Value("MotionPressScale"));
    private static readonly PointerEventHandler ReleasedHandler = (sender, _) => ScaleTo((UIElement)sender, 1f);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || e.NewValue is not true)
            return;

        // Buttons mark pointer events as handled, hence handledEventsToo.
        element.AddHandler(UIElement.PointerPressedEvent, PressedHandler, true);
        element.AddHandler(UIElement.PointerReleasedEvent, ReleasedHandler, true);
        element.AddHandler(UIElement.PointerCaptureLostEvent, ReleasedHandler, true);
        element.AddHandler(UIElement.PointerCanceledEvent, ReleasedHandler, true);
        element.AddHandler(UIElement.PointerExitedEvent, ReleasedHandler, true);
    }

    private static void ScaleTo(UIElement element, float scale)
    {
        Motion.VisualOf(element).CenterPoint = new Vector3(element.ActualSize.X / 2, element.ActualSize.Y / 2, 0);
        TimeSpan duration = Motion.Duration("MotionDurationPress");
        Motion.AnimateScalar(element, "Scale.X", null, scale, duration, TimeSpan.Zero, "MotionEasingStandard");
        Motion.AnimateScalar(element, "Scale.Y", null, scale, duration, TimeSpan.Zero, "MotionEasingStandard");
    }
}
