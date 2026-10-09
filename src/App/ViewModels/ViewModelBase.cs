using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Canopus.App.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // A list rebuilt with identical content must not reach the bindings: the ItemsControl
    // would recreate every row, and the rows already on screen would blink.
    protected void SetItems<T>(ref IReadOnlyList<T> field, IReadOnlyList<T> value, [CallerMemberName] string? propertyName = null)
    {
        if (field.SequenceEqual(value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
