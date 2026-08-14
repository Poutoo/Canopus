namespace Canopus.App.Models;

/// <summary>
/// Représentation prête à l'affichage d'un process pour le mini-tableau "Top processus".
/// </summary>
public sealed record ProcessDisplayItem(string Name, string CpuText, string MemoryText);
