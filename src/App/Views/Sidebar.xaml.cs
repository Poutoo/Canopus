using System.Numerics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Canopus.App.Views;

/// <summary>
/// État actif = élévation uniquement (scale ~1.1 + <see cref="ThemeShadow"/> via
/// une translation Z), jamais de cercle/badge/fond contrasté.
/// </summary>
public sealed partial class Sidebar : UserControl
{
    private static readonly Vector3 ActiveElevation = new(0, 0, 32);
    private static readonly Vector3 InactiveElevation = Vector3.Zero;

    private readonly (Grid Host, ScaleTransform Scale)[] _navItems;

    public event EventHandler<string>? NavigationRequested;

    public Sidebar()
    {
        InitializeComponent();

        _navItems =
        [
            (DashboardIconHost, DashboardScale),
            (AuditIconHost, AuditScale),
            (HistoriqueIconHost, HistoriqueScale),
            (DocumentationIconHost, DocumentationScale),
            (SettingsIconHost, SettingsScale)
        ];

        SetActive(DashboardIconHost);
    }

    private void OnNavItemTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Grid host)
            return;

        SetActive(host);

        if (host.Tag is string destination)
            NavigationRequested?.Invoke(this, destination);
    }

    private void SetActive(Grid activeHost)
    {
        foreach (var (host, scale) in _navItems)
        {
            bool isActive = ReferenceEquals(host, activeHost);
            scale.ScaleX = isActive ? 1.1 : 1.0;
            scale.ScaleY = isActive ? 1.1 : 1.0;
            host.Translation = isActive ? ActiveElevation : InactiveElevation;
        }
    }
}
