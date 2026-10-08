namespace Canopus.App.Tests;

internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string AppSource => Path.Combine(Root, "src", "App");

    private static string FindRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Canopus.sln")))
                return dir.FullName;
        }
        throw new InvalidOperationException("Canopus.sln not found above the test output folder.");
    }
}
