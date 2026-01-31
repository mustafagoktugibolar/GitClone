using GitClone.Interfaces;

namespace GitClone.Services;

public class IgnoreService(IRepositoryContext repositoryContext) : IIgnoreService
{
    private string IgnorePath => repositoryContext.IgnorePath;

    private readonly HashSet<string> _ignoredDirectories = new(StringComparer.OrdinalIgnoreCase);    // bin/ obj/ .vscode/
    private readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase);         // .env secrets.json
    private readonly HashSet<string> _ignoredExtensions = new(StringComparer.OrdinalIgnoreCase);    // *.log", "*.pdb", "*.tmp

    private DateTime _lastLoadedWriteTimeUtc = DateTime.MinValue;
    private readonly Lock _fileReloadLock = new();
    
    private const string IgnoreContent = """
                                         # ========================
                                         # OS / Editor Artifacts
                                         # ========================
                                         .DS_Store
                                         Thumbs.db
                                         *.swp
                                         *.swo

                                         # ========================
                                         # IDE / Editor
                                         # ========================
                                         .vscode/
                                         .idea/
                                         *.user
                                         *.userosscache
                                         *.sln.docstates

                                         # ========================
                                         # Build outputs
                                         # ========================
                                         bin/
                                         obj/
                                         out/
                                         Debug/
                                         Release/
                                         x64/
                                         x86/

                                         # ========================
                                         # .NET / Visual Studio
                                         # ========================
                                         .vs/
                                         *.pdb
                                         *.cache
                                         *.log

                                         # ========================
                                         # Test / Coverage
                                         # ========================
                                         TestResults/
                                         coverage/
                                         *.coverage
                                         *.coveragexml

                                         # ========================
                                         # NuGet
                                         # ========================
                                         packages/
                                         *.nupkg
                                         *.snupkg

                                         # ========================
                                         # Publish / Artifacts
                                         # ========================
                                         publish/
                                         artifacts/

                                         # ========================
                                         # Environment / Secrets
                                         # ========================
                                         .env
                                         .env.*
                                         secrets.json
                                         appsettings.Development.json
                                         appsettings.Local.json

                                         # ========================
                                         # Logs
                                         # ========================
                                         logs/
                                         *.log

                                         # ========================
                                         # Temporary files
                                         # ========================
                                         tmp/
                                         temp/
                                         """;

    private void ReloadIfChanged()
    { 
        EnsureCreated();
        

        var writeTimeUtc = File.GetLastWriteTimeUtc(IgnorePath);

        if (writeTimeUtc == _lastLoadedWriteTimeUtc)
        {
            return;
        }
        
        lock (_fileReloadLock)
        {
            writeTimeUtc = File.GetLastWriteTimeUtc(IgnorePath);
            if (writeTimeUtc == _lastLoadedWriteTimeUtc)
            {
                return;
            }
            
            _ignoredDirectories.Clear();
            _ignoredFiles.Clear();
            _ignoredExtensions.Clear();

            ParseIgnoreRules();
            _lastLoadedWriteTimeUtc = writeTimeUtc;
        }
    }

    private void ParseIgnoreRules()
    {
        try
        {
            var lines = File.ReadLines(IgnorePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                { 
                    continue;
                }

                if (trimmed.EndsWith('/'))
                {
                    _ignoredDirectories.Add(trimmed.TrimEnd('/'));
                }
                else if (trimmed.StartsWith("*."))
                {
                    _ignoredExtensions.Add(trimmed[1..]);
                }
                else
                {
                    _ignoredFiles.Add(trimmed);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error parsing ignore file {IgnorePath}: {e.Message}");
            throw;
        }
    }

    public void EnsureCreated()
    {
        if (File.Exists(IgnorePath))
        {
            return;
        }
        File.WriteAllText(IgnorePath, IgnoreContent);
        Console.WriteLine($".ilosignore created at {IgnorePath}");
    }

    public bool IsIgnored(string filePath)
    {
        ReloadIfChanged();
        
        var fullPath = Path.GetFullPath(filePath);

        var fileName = Path.GetFileName(fullPath);
        if (_ignoredFiles.Contains(fileName))
        {
            return true;
        }
        var extension = Path.GetExtension(fullPath);
        if (!string.IsNullOrEmpty(extension) && _ignoredExtensions.Contains(extension))
        {
            return true;
        }
        var relative = Path.GetRelativePath(repositoryContext.RootPath, fullPath).Replace('\\', '/');
        if (relative.StartsWith(".."))
        {
            return false;   
        }
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => _ignoredDirectories.Contains(part));
    }
}