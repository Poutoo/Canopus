using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Controls;

namespace Canopus.App.Animations;

/// <summary>
/// Page entrance: elements carrying an <c>Order</c> fade and rise in a cascade, then data
/// bars fill from zero. Only played on navigation, never by the periodic refresh.
/// </summary>
public static class Entrance
{
    public static readonly DependencyProperty OrderProperty = DependencyProperty.RegisterAttached(
        "Order", typeof(int), typeof(Entrance), new PropertyMetadata(-1));

    public static int GetOrder(DependencyObject element) => (int)element.GetValue(OrderProperty);

    public static void SetOrder(DependencyObject element, int value) => element.SetValue(OrderProperty, value);

    public static void Play(FrameworkElement page)
    {
        TimeSpan duration = Motion.Duration("MotionDurationPageEnter");
        double step = Motion.Value("MotionStaggerStep");
        float offset = (float)Motion.Value("MotionEnterOffset");
        var bars = new List<DataBar>();

        foreach (DependencyObject node in Descendants(page))
        {
            if (node is DataBar bar)
                bars.Add(bar);

            if (node is not UIElement element)
                continue;

            int order = GetOrder(element);
            if (order < 0)
                continue;

            TimeSpan delay = TimeSpan.FromMilliseconds(order * step);
            Motion.EnableTranslation(element);
            Motion.AnimateScalar(element, "Opacity", 0f, 1f, duration, delay, "MotionEasingEnter");
            Motion.AnimateScalar(element, "Translation.Y", offset, 0f, duration, delay, "MotionEasingEnter");
        }

        double barDelay = Motion.Value("MotionBarDelay");
        for (int i = 0; i < bars.Count; i++)
            bars[i].PlayEntrance(TimeSpan.FromMilliseconds(barDelay + i * step));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child))
                yield return descendant;
        }
    }
}
