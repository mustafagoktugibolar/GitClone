using GitClone.Core.Abstractions;
using GitClone.Application.Init;
using GitClone.Application.Status;
using GitClone.Cli.Rendering;
using GitClone.Infrastructure.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace GitClone.DebugHarness;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var scenario = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
        if (!IsValidScenario(scenario))
        {
            ShowUsage();
            return 1;
        }

        var scenarios = scenario == "all"
            ? new[] { "untracked", "tracked_clean", "modified" }
            : new[] { scenario };

        foreach (var scenarioName in scenarios)
        {
            await RunScenario(scenarioName);
        }

        return 0;
    }

    private static bool IsValidScenario(string scenario)
    {
        return scenario is "all" or "untracked" or "tracked_clean" or "modified";
    }

    private static async Task RunScenario(string scenario)
    {
        var repoPath = Path.Combine(
            Path.GetTempPath(),
            "gitclone-debugharness",
            $"{scenario}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        Directory.CreateDirectory(repoPath);

        await using var services = BuildServices(repoPath);
        var initUseCase = services.GetRequiredService<InitUseCase>();
        var statusUseCase = services.GetRequiredService<StatusUseCase>();
        var statusRenderer = services.GetRequiredService<StatusRenderer>();
        var repositorySessionFactory = services.GetRequiredService<IRepositorySessionFactory>();

        await initUseCase.ExecuteAsync(new InitRequest(repoPath));
        await SetupScenario(scenario, repoPath, repositorySessionFactory);

        var result = await statusUseCase.ExecuteAsync(new StatusRequest(repoPath));

        Console.WriteLine($"Scenario: {scenario}");
        Console.WriteLine($"Repository: {repoPath}");
        statusRenderer.Render(result);
        Console.WriteLine();
    }

    private static ServiceProvider BuildServices(string workingDirectory)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConsole, SystemConsole>();
        services.AddSingleton<IPasswordPrompter, SystemPasswordPrompter>();
        services.AddSingleton<IWorkingDirectoryProvider>(new StaticWorkingDirectoryProvider(workingDirectory));
        services.AddSingleton<IRepositorySessionFactory, RepositorySessionFactory>();
        services.AddScoped<InitUseCase>();
        services.AddScoped<StatusUseCase>();
        services.AddScoped<StatusRenderer>();
        return services.BuildServiceProvider();
    }

    private static async Task SetupScenario(string scenario, string repoPath, IRepositorySessionFactory repositorySessionFactory)
    {
        switch (scenario)
        {
            case "untracked":
                await File.WriteAllTextAsync(Path.Combine(repoPath, "new-file.txt"), "untracked");
                break;

            case "tracked_clean":
                await CreateAndStageFile(repositorySessionFactory, repoPath, "tracked.txt", "tracked clean");
                break;

            case "modified":
                var targetPath = await CreateAndStageFile(repositorySessionFactory, repoPath, "modified.txt", "before");
                await File.WriteAllTextAsync(targetPath, "after");
                break;
        }
    }

    private static async Task<string> CreateAndStageFile(
        IRepositorySessionFactory repositorySessionFactory,
        string repoPath,
        string relativePath,
        string content)
    {
        var fullPath = Path.Combine(repoPath, relativePath);
        await File.WriteAllTextAsync(fullPath, content);

        var session = repositorySessionFactory.CreateForPath(repoPath);
        var hash = session.HashService.ComputeSha1(content);

        await session.BlobStore.EnsureDirectory();
        if (!session.BlobStore.Exists(hash))
        {
            await session.BlobStore.Save(hash, content);
        }

        await session.IndexManager.EnsureCreated();
        await session.IndexManager.StageFile(relativePath, hash);
        return fullPath;
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage: dotnet run --project tools/GitClone.DebugHarness -- [all|untracked|tracked_clean|modified]");
    }

    private sealed class StaticWorkingDirectoryProvider(string workingDirectory) : IWorkingDirectoryProvider
    {
        public string GetCurrentDirectory()
        {
            return workingDirectory;
        }
    }
}
