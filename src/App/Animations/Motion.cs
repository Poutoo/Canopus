using System.Globalization;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;

namespace Canopus.App.Animations;

/// <summary>
/// Reads the motion tokens (Tokens.xaml) and runs Composition animations on XAML elements.
/// Every animation collapses to an instant change when Windows animations are turned off.
/// </summary>
public static class Motion
{
    private static readonly UISettings UiSettings = new();

    public static bool AnimationsEnabled => UiSettings.AnimationsEnabled;

    public static double Value(string key) => (double)Application.Current.Resources[key];

    public static TimeSpan Duration(string key) => TimeSpan.FromMilliseconds(Value(key));

    public static Visual VisualOf(UIElement element) => ElementCompositionPreview.GetElementVisual(element);

    public static CompositionEasingFunction Easing(Compositor compositor, string key)
    {
        float[] p = ((string)Application.Current.Resources[key])
            .Split(',')
            .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
            .ToArray();
        return compositor.CreateCubicBezierEasingFunction(new Vector2(p[0], p[1]), new Vector2(p[2], p[3]));
    }

    public static void EnableTranslation(UIElement element) =>
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

    public static void SetTranslationY(UIElement element, float y)
    {
        Visual visual = VisualOf(element);
        visual.StopAnimation("Translation.Y");
        visual.Properties.InsertVector3("Translation", new Vector3(0, y, 0));
    }

    /// <summary>
    /// Animates a scalar property of the element's visual ("Opacity", "Translation.Y",
    /// "Scale.X"...), or sets it immediately when animations are off.
    /// </summary>
    public static void AnimateScalar(UIElement element, string property, float? from, float to,
        TimeSpan duration, TimeSpan delay, string easingKey)
    {
        Visual visual = VisualOf(element);
        visual.StopAnimation(property);

        if (!AnimationsEnabled || duration <= TimeSpan.Zero)
        {
            SetScalar(visual, property, to);
            return;
        }

        Compositor compositor = visual.Compositor;
        ScalarKeyFrameAnimation animation = compositor.CreateScalarKeyFrameAnimation();
        if (from is float start)
            animation.InsertKeyFrame(0f, start);
        animation.InsertKeyFrame(1f, to, Easing(compositor, easingKey));
        animation.Duration = duration;
        animation.DelayTime = delay;
        animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        visual.StartAnimation(property, animation);
    }

    private static void SetScalar(Visual visual, string property, float value)
    {
        switch (property)
        {
            case "Opacity":
                visual.Opacity = value;
                break;
            case "Scale.X":
                visual.Scale = visual.Scale with { X = value };
                break;
            case "Translation.Y":
                visual.Properties.InsertVector3("Translation", new Vector3(0, value, 0));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }
    }
}
