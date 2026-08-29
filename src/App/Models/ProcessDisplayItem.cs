namespace Canopus.App.Models;

/// <summary>
/// Display-ready representation of a process for the "Top processus" mini-table.
/// </summary>
public sealed record ProcessDisplayItem(string Name, string CpuText, string MemoryText);
