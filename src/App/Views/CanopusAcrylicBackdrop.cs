using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Canopus.App.Views;

/// <summary>
/// Desktop Acrylic driven by a <see cref="DesktopAcrylicController"/>: the built-in
/// DesktopAcrylicBackdrop does not expose LuminosityOpacity nor FallbackColor, and the
/// readability of the whole window rests on the luminosity layer.
/// </summary>
public sealed class CanopusAcrylicBackdrop(Window window) : SystemBackdrop
{
    private DesktopAcrylicController? _controller;
    private SystemBackdropConfiguration? _configuration;

    public static bool IsSupported => DesktopAcrylicController.IsSupported();

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        // Theme forced to Light: without it the material follows Windows' dark theme.
        _configuration = new SystemBackdropConfiguration
        {
            Theme = SystemBackdropTheme.Light,
            IsInputActive = true
        };

        _controller = new DesktopAcrylicController
        {
            Kind = DesktopAcrylicKind.Base,
            TintColor = Resource<Color>("BackdropTintColor"),
            TintOpacity = (float)Resource<double>("BackdropTintOpacity"),
            LuminosityOpacity = (float)Resource<double>("BackdropLuminosityOpacity"),
            FallbackColor = Resource<Color>("BackdropFallbackColor")
        };
        _controller.AddSystemBackdropTarget(connectedTarget);
        _controller.SetSystemBackdropConfiguration(_configuration);

        window.Activated += OnWindowActivated;
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);

        window.Activated -= OnWindowActivated;
        _controller?.RemoveSystemBackdropTarget(disconnectedTarget);
        _controller?.Dispose();
        _controller = null;
        _configuration = null;
    }

    // A hand-made configuration is not tracked by the system: focus has to be relayed
    // here, otherwise the material never switches to FallbackColor when unfocused.
    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_configuration is not null)
            _configuration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private static T Resource<T>(string key) => (T)Application.Current.Resources[key];
}
