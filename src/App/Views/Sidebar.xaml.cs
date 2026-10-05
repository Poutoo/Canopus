using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Windows.Foundation;
using Canopus.App.Animations;
using Canopus.App.Localization;

namespace Canopus.App.Views;

public sealed partial class Sidebar : UserControl
{
    private readonly Dictionary<AppPage, (Button Item, Path Icon, TextBlock Label)> _items;
    private AppPage _activePage = AppPage.Dashboard;
    private float? _indicatorY;

    public event EventHandler<AppPage>? NavigationRequested;

    public Sidebar()
    {
        InitializeComponent();

        _items = new()
        {
            [AppPage.Dashboard] = (DashboardItem, DashboardIcon, DashboardLabel),
            [AppPage.Audit] = (AuditItem, AuditIcon, AuditLabel),
            [AppPage.GameSession] = (GameSessionItem, GameSessionIcon, GameSessionLabel),
            [AppPage.Parametres] = (ParametresItem, ParametresIcon, ParametresLabel)
        };

        Motion.EnableTranslation(ActiveIndicator);
        ApplyActiveStyles();
    }

    /// <summary>
    /// Reflects the displayed page, whatever triggered the navigation (sidebar or a
    /// button inside a page).
    /// </summary>
    public void SetActivePage(AppPage page)
    {
        if (page == _activePage)
            return;

        _activePage = page;
        ApplyActiveStyles();
        MoveIndicator(animate: true);
    }

    private void OnNavItemClick(object sender, RoutedEventArgs e)
    {
        AppPage page = _items.First(entry => ReferenceEquals(entry.Value.Item, sender)).Key;
        NavigationRequested?.Invoke(this, page);
    }

    // Parametres sits at the bottom, so its position changes with the window height.
    private void OnItemsHostSizeChanged(object sender, SizeChangedEventArgs e) => MoveIndicator(animate: false);

    private void ApplyActiveStyles()
    {
        var activeLabel = (Style)Application.Current.Resources["NavLabelActiveStyle"];
        var idleLabel = (Style)Application.Current.Resources["NavLabelStyle"];
        var activeBrush = (Brush)Application.Current.Resources["TextPrimaryBrush"];
        var idleBrush = (Brush)Application.Current.Resources["TextSecondaryBrush"];

        foreach (var (page, (item, icon, label)) in _items)
        {
            bool isActive = page == _activePage;
            label.Style = isActive ? activeLabel : idleLabel;
            icon.Stroke = isActive ? activeBrush : idleBrush;
            AutomationProperties.SetItemStatus(item, isActive ? Strings.Get("Nav.CurrentPage") : string.Empty);
        }
    }

    private void MoveIndicator(bool animate)
    {
        Button item = _items[_activePage].Item;
        if (item.ActualHeight == 0)
            return;

        float y = (float)item.TransformToVisual(ItemsHost).TransformPoint(new Point(0, 0)).Y;
        float distance = _indicatorY is float previous ? Math.Abs(y - previous) : 0;
        _indicatorY = y;

        if (!animate)
        {
            Motion.SetTranslationY(ActiveIndicator, y);
            return;
        }

        string durationKey = distance > Motion.Value("MotionNavLongDistance") ? "MotionDurationNavLong" : "MotionDurationNav";
        Motion.AnimateScalar(ActiveIndicator, "Translation.Y", null, y,
            Motion.Duration(durationKey), TimeSpan.Zero, "MotionEasingStandard");
    }
}
