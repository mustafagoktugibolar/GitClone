using System.CommandLine;
using GitClone.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using GitClone.Core.Abstractions;
using GitClone.Application.Add;
using GitClone.Application.Branch;
using GitClone.Application.CherryPick;
using GitClone.Application.Commit;
using GitClone.Application.Clone;
using GitClone.Application.Config;
using GitClone.Application.Diff;
using GitClone.Application.Fetch;
using GitClone.Application.Init;
using GitClone.Application.Log;
using GitClone.Application.Merge;
using GitClone.Application.Mv;
using GitClone.Application.Pull;
using GitClone.Application.Push;
using GitClone.Application.Rebase;
using GitClone.Application.Remote;
using GitClone.Application.Reset;
using GitClone.Application.Restore;
using GitClone.Application.Rm;
using GitClone.Application.Stash;
using GitClone.Application.Status;
using GitClone.Application.Switch;
using GitClone.Application.Tag;
using GitClone.Cli.Rendering;
using GitClone.Infrastructure.Runtime;

namespace GitClone.Cli
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            return RunAsync(args);
        }

        public static async Task<int> RunAsync(string[] args, Action<IServiceCollection>? configureServices = null)
        {
            var serviceCollection = new ServiceCollection();
            AddInfrastructure(serviceCollection);
            AddApplication(serviceCollection);
            AddCommands(serviceCollection);
            configureServices?.Invoke(serviceCollection);

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var console = serviceProvider.GetRequiredService<IConsole>();
            var rootCommand = BuildRootCommand(serviceProvider);

            // Route System.CommandLine's own output (help, --version, parse errors) through the
            // same IConsole abstraction the rest of the app uses, instead of straight to
            // System.Console - keeps it testable and consistent (e.g. colorable) with everything else.
            var invocationConfiguration = new InvocationConfiguration
            {
                Output = new ConsoleTextWriter(console.WriteLine),
                Error = new ConsoleTextWriter(console.WriteErrorLine),
                // Domain exceptions are mapped to specific exit codes below via CommandErrorMapper;
                // don't let System.CommandLine's own handler swallow them first.
                EnableDefaultExceptionHandler = false
            };

            try
            {
                var parseResult = rootCommand.Parse(args);
                return await parseResult.InvokeAsync(invocationConfiguration);
            }
            catch (Exception ex)
            {
                console.SetForegroundColor(ConsoleColor.Red);
                console.WriteLine($"Command failed: {ex.Message}");
                console.ResetColor();
                return CommandErrorMapper.MapExitCode(ex);
            }
        }

        private static RootCommand BuildRootCommand(IServiceProvider provider)
        {
            var root = new RootCommand("Lightweight Git-like CLI tool written in .NET.");

            root.Subcommands.Add(provider.GetRequiredService<InitCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<StatusCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<AddCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<RmCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<MvCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<CommitCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<CherryPickCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<LogCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<DiffCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<MergeCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<RebaseCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<ResetCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<RestoreCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<StashCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<SwitchCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<BranchCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<TagCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<CloneCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<ConfigCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<RemoteCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<FetchCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<PushCommand>().Build());
            root.Subcommands.Add(provider.GetRequiredService<PullCommand>().Build());

            // RootCommand adds a --version option automatically; give it the conventional -v alias.
            var versionOption = root.Options.OfType<VersionOption>().Single();
            versionOption.Aliases.Add("-v");

            return root;
        }

        private static void AddCommands(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<InitCommand>();
            serviceCollection.AddScoped<StatusCommand>();
            serviceCollection.AddScoped<AddCommand>();
            serviceCollection.AddScoped<RmCommand>();
            serviceCollection.AddScoped<MvCommand>();
            serviceCollection.AddScoped<BranchCommand>();
            serviceCollection.AddScoped<TagCommand>();
            serviceCollection.AddScoped<CloneCommand>();
            serviceCollection.AddScoped<CommitCommand>();
            serviceCollection.AddScoped<CherryPickCommand>();
            serviceCollection.AddScoped<ConfigCommand>();
            serviceCollection.AddScoped<DiffCommand>();
            serviceCollection.AddScoped<LogCommand>();
            serviceCollection.AddScoped<MergeCommand>();
            serviceCollection.AddScoped<RebaseCommand>();
            serviceCollection.AddScoped<ResetCommand>();
            serviceCollection.AddScoped<RestoreCommand>();
            serviceCollection.AddScoped<StashCommand>();
            serviceCollection.AddScoped<SwitchCommand>();
            serviceCollection.AddScoped<RemoteCommand>();
            serviceCollection.AddScoped<FetchCommand>();
            serviceCollection.AddScoped<PushCommand>();
            serviceCollection.AddScoped<PullCommand>();
        }

        private static void AddInfrastructure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IConsole, SystemConsole>();
            serviceCollection.AddSingleton<IPasswordPrompter, SystemPasswordPrompter>();
            serviceCollection.AddSingleton<ICloneExecutor, HttpZipCloneExecutor>();
            serviceCollection.AddSingleton<IWorkingDirectoryProvider, WorkingDirectoryProvider>();
            serviceCollection.AddSingleton<IRepositorySessionFactory, RepositorySessionFactory>();
        }

        private static void AddApplication(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<AddUseCase>();
            serviceCollection.AddScoped<RmUseCase>();
            serviceCollection.AddScoped<MvUseCase>();
            serviceCollection.AddScoped<BranchUseCase>();
            serviceCollection.AddScoped<TagUseCase>();
            serviceCollection.AddScoped<CloneUseCase>();
            serviceCollection.AddScoped<CommitUseCase>();
            serviceCollection.AddScoped<CherryPickUseCase>();
            serviceCollection.AddScoped<ConfigUseCase>();
            serviceCollection.AddScoped<DiffUseCase>();
            serviceCollection.AddScoped<InitUseCase>();
            serviceCollection.AddScoped<LogUseCase>();
            serviceCollection.AddScoped<MergeUseCase>();
            serviceCollection.AddScoped<RebaseUseCase>();
            serviceCollection.AddScoped<ResetUseCase>();
            serviceCollection.AddScoped<RestoreUseCase>();
            serviceCollection.AddScoped<StashUseCase>();
            serviceCollection.AddScoped<StatusUseCase>();
            serviceCollection.AddScoped<SwitchUseCase>();
            serviceCollection.AddScoped<RemoteUseCase>();
            serviceCollection.AddScoped<FetchUseCase>();
            serviceCollection.AddScoped<PushUseCase>();
            serviceCollection.AddScoped<PullUseCase>();

            serviceCollection.AddScoped<AddRenderer>();
            serviceCollection.AddScoped<RmRenderer>();
            serviceCollection.AddScoped<MvRenderer>();
            serviceCollection.AddScoped<BranchRenderer>();
            serviceCollection.AddScoped<TagRenderer>();
            serviceCollection.AddScoped<CloneRenderer>();
            serviceCollection.AddScoped<CommitRenderer>();
            serviceCollection.AddScoped<CherryPickRenderer>();
            serviceCollection.AddScoped<ConfigRenderer>();
            serviceCollection.AddScoped<DiffRenderer>();
            serviceCollection.AddScoped<InitRenderer>();
            serviceCollection.AddScoped<LogRenderer>();
            serviceCollection.AddScoped<MergeRenderer>();
            serviceCollection.AddScoped<RebaseRenderer>();
            serviceCollection.AddScoped<ResetRenderer>();
            serviceCollection.AddScoped<RestoreRenderer>();
            serviceCollection.AddScoped<StashRenderer>();
            serviceCollection.AddScoped<StatusRenderer>();
            serviceCollection.AddScoped<SwitchRenderer>();
            serviceCollection.AddScoped<RemoteRenderer>();
            serviceCollection.AddScoped<FetchRenderer>();
            serviceCollection.AddScoped<PushRenderer>();
            serviceCollection.AddScoped<PullRenderer>();
        }
    }
}
