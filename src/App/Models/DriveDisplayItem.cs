using Microsoft.UI.Xaml;

namespace Canopus.App.Models;

/// <summary>
/// Représentation prête à l'affichage d'un disque/partition, avec la
/// répartition de la barre de remplissage déjà calculée (deux colonnes
/// <see cref="GridLength"/> en étoile : plus besoin de convertisseur en XAML).
/// </summary>
public sealed record DriveDisplayItem(
    string Name,
    string UsageText,
    GridLength FilledColumnWidth,
    GridLength EmptyColumnWidth);
