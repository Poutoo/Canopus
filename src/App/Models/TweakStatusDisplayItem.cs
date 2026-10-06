using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Canopus.App.Models;

public record TweakStatusDisplayItem(
    string Name,
    string Description,
    string StatusLabel,
    Brush StatusTextBrush,
    Brush StatusBgBrush,
    string FailureReason,
    Visibility FailureVisibility,
    Thickness DividerThickness,
    Visibility ExcludeVisibility,
    bool IsExcluded,
    bool IsExcludeEnabled);
