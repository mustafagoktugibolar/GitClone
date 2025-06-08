
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GitClone.Interfaces;

namespace GitClone.Services
{
    public class CloneService(IRepositoryService repositoryService) : ICloneService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _baseDirectory = Environment.CurrentDirectory;

        public async Task CloneAsync(string url, string projectName, string branch, string location)
        {
            var baseUrl = url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
                ? url[..^4]
                : url;
            
            if (string.IsNullOrWhiteSpace(branch))
            {
                var html = await _httpClient.GetStringAsync(baseUrl);
                var json = ExtractDefaultBranchJson(html);
                using var doc = JsonDocument.Parse(json);
                branch = doc.RootElement
                            .GetProperty("repo")
                            .GetProperty("defaultBranch")
                            .GetString()!;
            }
            
            var zipUrl = $"{baseUrl}/archive/refs/heads/{branch}.zip";
            
            var response = await _httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var tempFolder = Path.GetTempPath();

            var zipFileName = !string.IsNullOrWhiteSpace(projectName) ? Path.Combine(tempFolder, projectName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? projectName : $"{projectName}.zip") : Path.ChangeExtension(Path.GetTempFileName(), ".zip");

            await using (var fs = new FileStream(
                zipFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
            {
                await response.Content.CopyToAsync(fs);
            }
            
            var repoName = Path.GetFileName(new Uri(baseUrl).AbsolutePath);
            
            var targetDir = !string.IsNullOrWhiteSpace(location)
                ? location
                : Path.Combine(_baseDirectory,
                    !string.IsNullOrWhiteSpace(projectName) ? projectName : repoName);

            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, recursive: true);
            }
            Directory.CreateDirectory(targetDir);
            
            Directory.SetCurrentDirectory(targetDir);
            repositoryService.InitRepository(targetDir);
            
            using (var zip = ZipFile.OpenRead(zipFileName))
            {
                zip.ExtractToDirectory(targetDir);
            }
            
            var childPath = Path.Combine(targetDir, $"{repoName}-{branch}");
            if (Directory.Exists(childPath))
            {
                var directory = new DirectoryInfo(childPath);
                var directories = directory.GetDirectories();
                var files = directory.GetFiles();
                foreach (var file in files)
                {
                    file.MoveTo(Path.Combine(targetDir, file.Name));
                }
                foreach (var dir in directories)
                {
                    dir.MoveTo(Path.Combine(targetDir, dir.Name));
                }
                Directory.Delete(childPath, recursive: true);
            }
            
            File.Delete(zipFileName);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Repository successfully cloned!");
            Console.ResetColor();
        }
        
        private static string ExtractDefaultBranchJson(string html)
        {
            const string marker = "<script type=\"application/json\" data-target=\"react-partial.embeddedData\">{\"props\":{\"initialPayload\":";
            int markerIdx = html.IndexOf(marker, StringComparison.Ordinal);
            if (markerIdx < 0)
                throw new InvalidOperationException("EmbeddedData script marker not found.");
        
            int pos = markerIdx + marker.Length;
        
            int start = html.IndexOf('{', pos);
            if (start < 0)
                throw new InvalidOperationException("JSON object start '{' not found.");
        
            int depth = 0;
            int end = start;
            for (; end < html.Length; end++)
            {
                if (html[end] == '{') depth++;
                else if (html[end] == '}')
                {
                    depth--;
                    if (depth == 0)
                        break;
                }
            }

            if (depth != 0)
                throw new InvalidOperationException("JSON braces are unbalanced.");
        
            return html.Substring(start, end - start + 1);
        }
    }
}
