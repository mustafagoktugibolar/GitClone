namespace GitClone.Infrastructure.Helpers;

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
            if (Directory.Exists(markerPath) && IsRepositoryMarker(markerPath))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"No '{RepoMarkerDir}' directory found. Run 'ilos init' in a folder to initialize a repository.");
    }

    private static bool IsRepositoryMarker(string markerPath)
    {
        var indexPath = Path.Combine(markerPath, "index");
        var refsPath = Path.Combine(markerPath, "refs");
        var headPath = Path.Combine(markerPath, "HEAD");
        return File.Exists(indexPath) || Directory.Exists(refsPath) || File.Exists(headPath);
    }
    public static string GetInitRoot(string startDirectory)  => Path.GetFullPath(startDirectory);
}
