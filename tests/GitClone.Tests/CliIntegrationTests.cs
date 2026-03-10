using GitClone.Cli;
using GitClone.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GitClone.Tests;

public class CliIntegrationTests
{
    [Fact]
    public async Task HelpCommand_PrintsCommandList()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();

        var exitCode = await RunCommand(["help"], sandbox.Path, console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage: ilos <command>", console.Lines);
        Assert.Contains(console.Lines, line => line.Contains("config: Manage local/global configuration", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VersionCommand_PrintsVersion()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();

        var exitCode = await RunCommand(["--version"], sandbox.Path, console);

        Assert.Equal(0, exitCode);
        Assert.Contains(console.Lines, line => line.StartsWith("ilos ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitCommand_CreatesRepository()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();

        var exitCode = await Program.RunAsync(
            ["init"],
            services =>
            {
                services.AddSingleton<IConsole>(console);
                services.AddSingleton<IWorkingDirectoryProvider>(new StaticWorkingDirectoryProvider(sandbox.Path));
            });

        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(Path.Combine(sandbox.Path, ".ilos")));
        Assert.True(File.Exists(Path.Combine(sandbox.Path, ".ilos", "index")));
        Assert.Contains(console.Lines, line => line.Contains("Repository ready at", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StatusCommand_ShowsUntrackedFiles()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand("init", sandbox.Path, console);

        var untrackedPath = Path.Combine(sandbox.Path, "untracked.txt");
        await File.WriteAllTextAsync(untrackedPath, "new");

        console.Clear();
        var exitCode = await RunCommand("status", sandbox.Path, console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Untracked files:", console.Lines);
        Assert.Contains(console.Lines, line => line.Contains("untracked.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StatusCommand_ShowsTrackedCleanAndModifiedFiles()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand("init", sandbox.Path, console);

        var trackedPath = Path.Combine(sandbox.Path, "tracked.txt");
        await File.WriteAllTextAsync(trackedPath, "v1");
        await RunCommand("add", trackedPath, sandbox.Path, console);

        console.Clear();
        var cleanExitCode = await RunCommand("status", sandbox.Path, console);

        Assert.Equal(0, cleanExitCode);
        Assert.Contains("Tracked clean files:", console.Lines);
        Assert.Contains(console.Lines, line => line.Contains("tracked.txt", StringComparison.Ordinal));

        await File.WriteAllTextAsync(trackedPath, "v2");
        console.Clear();
        var modifiedExitCode = await RunCommand("status", sandbox.Path, console);

        Assert.Equal(0, modifiedExitCode);
        Assert.Contains("Modified files:", console.Lines);
        Assert.Contains(console.Lines, line => line.Contains("tracked.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddCommand_StagesRelativePathFromNestedDirectory()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        var nestedDir = Path.Combine(sandbox.Path, "src");
        Directory.CreateDirectory(nestedDir);
        await File.WriteAllTextAsync(Path.Combine(nestedDir, "main.txt"), "content");

        console.Clear();
        var addExitCode = await RunCommand(["add", "main.txt"], nestedDir, console);
        Assert.Equal(0, addExitCode);

        console.Clear();
        var statusExitCode = await RunCommand(["status"], sandbox.Path, console);
        Assert.Equal(0, statusExitCode);
        Assert.Contains(console.Lines, line => line.Contains("src/main.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BranchCommand_CreateRenameDeleteAndList()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        console.Clear();
        var createExitCode = await RunCommand(["branch", "feature"], sandbox.Path, console);
        Assert.Equal(0, createExitCode);
        Assert.Contains(console.Lines, line => line.Contains("Created branch 'feature'", StringComparison.Ordinal));

        console.Clear();
        var listAfterCreate = await RunCommand(["branch"], sandbox.Path, console);
        Assert.Equal(0, listAfterCreate);
        Assert.Contains(console.Lines, line => line.Contains("* master", StringComparison.Ordinal));
        Assert.Contains(console.Lines, line => line.Contains("feature", StringComparison.Ordinal));

        console.Clear();
        var renameExitCode = await RunCommand(["branch", "-m", "feature", "release"], sandbox.Path, console);
        Assert.Equal(0, renameExitCode);
        Assert.Contains(console.Lines, line => line.Contains("Renamed branch 'feature' to 'release'", StringComparison.Ordinal));

        console.Clear();
        var deleteExitCode = await RunCommand(["branch", "-d", "release"], sandbox.Path, console);
        Assert.Equal(0, deleteExitCode);
        Assert.Contains(console.Lines, line => line.Contains("Deleted branch 'release'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CommitAndLogCommands_CreateAndDisplayHistory()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        await File.WriteAllTextAsync(Path.Combine(sandbox.Path, "notes.txt"), "v1");
        await RunCommand(["add", "notes.txt"], sandbox.Path, console);

        console.Clear();
        var commitExitCode = await RunCommand(["commit", "-m", "initial commit"], sandbox.Path, console);
        Assert.Equal(0, commitExitCode);
        Assert.Contains(console.Lines, line => line.Contains("initial commit", StringComparison.Ordinal));

        var headRefPath = Path.Combine(sandbox.Path, ".ilos", "refs", "heads", "master");
        var commitId = (await File.ReadAllTextAsync(headRefPath)).Trim();
        Assert.False(string.IsNullOrWhiteSpace(commitId));
        Assert.True(File.Exists(Path.Combine(sandbox.Path, ".ilos", "commits", $"{commitId}.json")));

        console.Clear();
        var logExitCode = await RunCommand(["log"], sandbox.Path, console);
        Assert.Equal(0, logExitCode);
        Assert.Contains(console.Lines, line => line.Equals($"commit {commitId}", StringComparison.Ordinal));
        Assert.Contains(console.Lines, line => line.Contains("initial commit", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CommitCommand_WithoutStagedChanges_ShowsNothingToCommit()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        console.Clear();
        var commitExitCode = await RunCommand(["commit", "-m", "empty"], sandbox.Path, console);

        Assert.Equal(0, commitExitCode);
        Assert.Contains(console.Lines, line => line.Contains("Nothing to commit.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BranchCommand_NewBranchStartsFromHeadCommit()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        await File.WriteAllTextAsync(Path.Combine(sandbox.Path, "tracked.txt"), "v1");
        await RunCommand(["add", "tracked.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "base"], sandbox.Path, console);

        console.Clear();
        var branchExitCode = await RunCommand(["branch", "feature"], sandbox.Path, console);
        Assert.Equal(0, branchExitCode);

        var masterRef = (await File.ReadAllTextAsync(Path.Combine(sandbox.Path, ".ilos", "refs", "heads", "master"))).Trim();
        var featureRef = (await File.ReadAllTextAsync(Path.Combine(sandbox.Path, ".ilos", "refs", "heads", "feature"))).Trim();

        Assert.False(string.IsNullOrWhiteSpace(masterRef));
        Assert.Equal(masterRef, featureRef);
    }

    [Fact]
    public async Task SwitchCommand_UpdatesWorkingTreeForTargetBranch()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        var filePath = Path.Combine(sandbox.Path, "app.txt");
        await File.WriteAllTextAsync(filePath, "master");
        await RunCommand(["add", "app.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "master commit"], sandbox.Path, console);

        await RunCommand(["branch", "feature"], sandbox.Path, console);
        await RunCommand(["switch", "feature"], sandbox.Path, console);

        await File.WriteAllTextAsync(filePath, "feature");
        await RunCommand(["add", "app.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "feature commit"], sandbox.Path, console);

        console.Clear();
        var switchBackExitCode = await RunCommand(["switch", "master"], sandbox.Path, console);
        Assert.Equal(0, switchBackExitCode);

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("master", content);
        Assert.Contains(console.Lines, line => line.Contains("Switched to branch 'master'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiffCommand_ShowsWorkingTreeAndCachedChanges()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        var filePath = Path.Combine(sandbox.Path, "diff.txt");
        await File.WriteAllTextAsync(filePath, "v1");
        await RunCommand(["add", "diff.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "base"], sandbox.Path, console);

        await File.WriteAllTextAsync(filePath, "v2");

        console.Clear();
        var diffExitCode = await RunCommand(["diff"], sandbox.Path, console);
        Assert.Equal(0, diffExitCode);
        Assert.Contains(console.Lines, line => line.Equals("M diff.txt", StringComparison.Ordinal));

        console.Clear();
        var cachedNoChangeExitCode = await RunCommand(["diff", "--cached"], sandbox.Path, console);
        Assert.Equal(0, cachedNoChangeExitCode);
        Assert.Contains("No differences.", console.Lines);

        await RunCommand(["add", "diff.txt"], sandbox.Path, console);
        console.Clear();
        var cachedExitCode = await RunCommand(["diff", "--cached"], sandbox.Path, console);
        Assert.Equal(0, cachedExitCode);
        Assert.Contains(console.Lines, line => line.Equals("M diff.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RestoreCommand_RestoresWorkingTreeAndStagedState()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        var filePath = Path.Combine(sandbox.Path, "restore.txt");
        await File.WriteAllTextAsync(filePath, "v1");
        await RunCommand(["add", "restore.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "base"], sandbox.Path, console);

        await File.WriteAllTextAsync(filePath, "v2");
        await RunCommand(["restore", "restore.txt"], sandbox.Path, console);
        var restoredContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal("v1", restoredContent);

        await File.WriteAllTextAsync(filePath, "v3");
        await RunCommand(["add", "restore.txt"], sandbox.Path, console);
        await RunCommand(["restore", "--staged", "restore.txt"], sandbox.Path, console);

        console.Clear();
        var commitExitCode = await RunCommand(["commit", "-m", "should not commit"], sandbox.Path, console);
        Assert.Equal(0, commitExitCode);
        Assert.Contains(console.Lines, line => line.Contains("No staged changes to commit", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResetCommand_MixedAndHardModesWorkAsExpected()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        var filePath = Path.Combine(sandbox.Path, "reset.txt");
        await File.WriteAllTextAsync(filePath, "v1");
        await RunCommand(["add", "reset.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "c1"], sandbox.Path, console);

        var firstCommit = (await File.ReadAllTextAsync(Path.Combine(sandbox.Path, ".ilos", "refs", "heads", "master"))).Trim();
        Assert.False(string.IsNullOrWhiteSpace(firstCommit));

        await File.WriteAllTextAsync(filePath, "v2");
        await RunCommand(["add", "reset.txt"], sandbox.Path, console);
        await RunCommand(["commit", "-m", "c2"], sandbox.Path, console);

        console.Clear();
        var mixedExitCode = await RunCommand(["reset", "--mixed", "HEAD~1"], sandbox.Path, console);
        Assert.Equal(0, mixedExitCode);

        var headAfterMixed = (await File.ReadAllTextAsync(Path.Combine(sandbox.Path, ".ilos", "refs", "heads", "master"))).Trim();
        Assert.Equal(firstCommit, headAfterMixed);

        console.Clear();
        var statusAfterMixed = await RunCommand(["status"], sandbox.Path, console);
        Assert.Equal(0, statusAfterMixed);
        Assert.Contains(console.Lines, line => line.Contains("Modified files:", StringComparison.Ordinal));
        Assert.Contains(console.Lines, line => line.Contains("reset.txt", StringComparison.Ordinal));

        console.Clear();
        var hardExitCode = await RunCommand(["reset", "--hard", "HEAD"], sandbox.Path, console);
        Assert.Equal(0, hardExitCode);

        var contentAfterHard = await File.ReadAllTextAsync(filePath);
        Assert.Equal("v1", contentAfterHard);

        console.Clear();
        var cleanStatusExitCode = await RunCommand(["status"], sandbox.Path, console);
        Assert.Equal(0, cleanStatusExitCode);
        Assert.Contains("Working tree clean.", console.Lines);
    }

    [Fact]
    public async Task ConfigCommand_ListLocalConfigs()
    {
        using var sandbox = SandboxDirectory.Create();
        var console = new TestConsole();
        await RunCommand(["init"], sandbox.Path, console);

        console.Clear();
        var exitCode = await RunCommand(["config", "list"], sandbox.Path, console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Local Configs:", console.Lines);
    }

    private static Task<int> RunCommand(string command, string workingDirectory, TestConsole console)
    {
        return RunCommand([command], workingDirectory, console);
    }

    private static Task<int> RunCommand(string command, string value, string workingDirectory, TestConsole console)
    {
        string[] args = string.IsNullOrWhiteSpace(value) ? [command] : [command, value];
        return RunCommand(args, workingDirectory, console);
    }

    private static Task<int> RunCommand(string[] args, string workingDirectory, TestConsole console)
    {
        return Program.RunAsync(
            args,
            services =>
            {
                services.AddSingleton<IConsole>(console);
                services.AddSingleton<IWorkingDirectoryProvider>(new StaticWorkingDirectoryProvider(workingDirectory));
            });
    }

    private sealed class StaticWorkingDirectoryProvider(string currentDirectory) : IWorkingDirectoryProvider
    {
        public string GetCurrentDirectory()
        {
            return currentDirectory;
        }
    }

    private sealed class SandboxDirectory : IDisposable
    {
        private SandboxDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static SandboxDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "gitclone-tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(path);
            return new SandboxDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class TestConsole : IConsole
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines => _lines;

        public void Write(string value)
        {
            _lines.Add(value);
        }

        public void WriteLine(string value)
        {
            _lines.Add(value);
        }

        public void WriteErrorLine(string value)
        {
            _lines.Add(value);
        }

        public string? ReadLine()
        {
            return null;
        }

        public void SetForegroundColor(ConsoleColor color)
        {
        }

        public void ResetColor()
        {
        }

        public void Clear()
        {
            _lines.Clear();
        }
    }
}
