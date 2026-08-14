using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Canopus.App.Models;

public sealed record AuditDisplayItem(
    string Title,
    string StatusLabel,
    Brush StatusTextBrush,
    Brush StatusBgBrush,
    string Description,
    string DetailNote,
    Visibility DetailNoteVisibility);
