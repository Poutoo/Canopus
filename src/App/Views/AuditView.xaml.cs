using Microsoft.UI.Xaml.Controls;
using Canopus.App.ViewModels;

namespace Canopus.App.Views;

public sealed partial class AuditView : UserControl
{
    public AuditViewModel ViewModel { get; }

    public AuditView()
    {
        InitializeComponent();
        ViewModel = new AuditViewModel(App.AuditService);
    }
}
