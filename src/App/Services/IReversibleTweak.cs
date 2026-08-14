using Canopus.App.Models;

namespace Canopus.App.Services;

public interface IReversibleTweak
{
    string Name { get; }
    Task<TweakSnapshot> CaptureAsync();
    Task ApplyAsync();
    Task<bool> VerifyAsync();
    Task RevertAsync(TweakSnapshot snapshot);
}
