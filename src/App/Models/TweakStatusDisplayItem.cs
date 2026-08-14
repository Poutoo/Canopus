using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Canopus.App.Models;

public record TweakStatusDisplayItem(
    string Name,
    string StatusLabel,
    Brush StatusTextBrush,
    Brush StatusBgBrush,
    string Note,
    Visibility NoteVisibility);
