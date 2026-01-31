namespace GitClone.Helpers;

public static class RepoLocator
{
    private const string RepoMarkerDir = ".ilos";

    public static string FindRepoRoot(string startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            throw new ArgumentException("startDirectory is empty.");

        var dir = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (dir != null)
        {
            var markerPath = Path.Combine(dir.FullName, RepoMarkerDir);
            if (Directory.Exists(markerPath))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"No '{RepoMarkerDir}' directory found. Run 'ilos init' in a folder to initialize a repository.");
    }
    public static string GetInitRoot(string startDirectory)  => Path.GetFullPath(startDirectory);
}