using System.IO.Compression;
using System.Text.Json;
using GitClone.Core.Abstractions;

namespace GitClone.Infrastructure.Runtime;

public sealed class HttpZipCloneExecutor : ICloneExecutor
{
    private const string EmbeddedDataMarker =
        "<script type=\"application/json\" data-target=\"react-partial.embeddedData\">{\"props\":{\"initialPayload\":";

    public async Task<CloneExecutionResult> ExecuteAsync(CloneExecutionRequest request)
    {
        using var httpClient = new HttpClient();
        var baseUrl = request.Url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? request.Url[..^4]
            : request.Url;

        var branchName = request.Branch;
        if (string.IsNullOrWhiteSpace(branchName))
        {
            var html = await httpClient.GetStringAsync(baseUrl);
            branchName = ExtractDefaultBranch(html);
        }

        var zipUrl = $"{baseUrl}/archive/refs/heads/{branchName}.zip";
        using var response = await httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        try
        {
            await using (var fs = new FileStream(
                             tempZipPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             8192,
                             useAsync: true))
            {
                await response.Content.CopyToAsync(fs);
            }

            var repositoryName = Path.GetFileName(new Uri(baseUrl).AbsolutePath);
            var targetDirectory = ResolveTargetDirectory(request, repositoryName);

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, recursive: true);
            }

            Directory.CreateDirectory(targetDirectory);
            await using (var archive = await ZipFile.OpenReadAsync(tempZipPath))
            {
                await archive.ExtractToDirectoryAsync(targetDirectory);
            }

            FlattenExtractedFolder(targetDirectory, repositoryName, branchName!);
            return new CloneExecutionResult(repositoryName, branchName!, targetDirectory);
        }
        finally
        {
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
        }
    }

    private static string ResolveTargetDirectory(CloneExecutionRequest request, string repositoryName)
    {
        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            return Path.GetFullPath(request.Location);
        }

        var folderName = string.IsNullOrWhiteSpace(request.ProjectName) ? repositoryName : request.ProjectName;
        return Path.GetFullPath(Path.Combine(request.WorkingDirectory, folderName!));
    }

    private static void FlattenExtractedFolder(string targetDirectory, string repositoryName, string branchName)
    {
        var extractedChild = Path.Combine(targetDirectory, $"{repositoryName}-{branchName}");
        if (!Directory.Exists(extractedChild))
        {
            return;
        }

        var childDirectory = new DirectoryInfo(extractedChild);
        foreach (var file in childDirectory.GetFiles())
        {
            file.MoveTo(Path.Combine(targetDirectory, file.Name), overwrite: true);
        }

        foreach (var directory in childDirectory.GetDirectories())
        {
            directory.MoveTo(Path.Combine(targetDirectory, directory.Name));
        }

        Directory.Delete(extractedChild, recursive: true);
    }

    private static string ExtractDefaultBranch(string html)
    {
        var markerIndex = html.IndexOf(EmbeddedDataMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException("Could not detect the remote default branch.");
        }

        var searchPosition = markerIndex + EmbeddedDataMarker.Length;
        var jsonStart = html.IndexOf('{', searchPosition);
        if (jsonStart < 0)
        {
            throw new InvalidOperationException("Could not parse default branch payload.");
        }

        var depth = 0;
        var end = jsonStart;
        for (; end < html.Length; end++)
        {
            if (html[end] == '{')
            {
                depth++;
            }
            else if (html[end] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    break;
                }
            }
        }

        if (depth != 0)
        {
            throw new InvalidOperationException("Default branch payload is malformed.");
        }

        var payload = html.Substring(jsonStart, end - jsonStart + 1);
        using var document = JsonDocument.Parse(payload);
        var branch = document.RootElement
            .GetProperty("repo")
            .GetProperty("defaultBranch")
            .GetString();

        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new InvalidOperationException("Could not resolve default branch.");
        }

        return branch;
    }
}
