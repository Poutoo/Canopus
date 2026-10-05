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

    private void OnValueChanged(double oldValue, double newValue) => SetFillScale(newValue);

    private void SetFillScale(double fraction) =>
        Motion.VisualOf(_fill).Scale = new Vector3((float)Math.Clamp(fraction, 0, 1), 1, 1);
}
