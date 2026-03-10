using GitClone.Cli.Commands;
using GitClone.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using GitClone.Core.Abstractions;
using GitClone.Application.Add;
using GitClone.Application.Branch;
using GitClone.Application.Commit;
using GitClone.Application.Clone;
using GitClone.Application.Config;
using GitClone.Application.Diff;
using GitClone.Application.Help;
using GitClone.Application.Init;
using GitClone.Application.Log;
using GitClone.Application.Reset;
using GitClone.Application.Restore;
using GitClone.Application.Status;
using GitClone.Application.Switch;
using GitClone.Application.Version;
using GitClone.Cli;
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

            if (args.Length == 0)
            {
                configureServices?.Invoke(serviceCollection);
                using var provider = serviceCollection.BuildServiceProvider();
                var console = provider.GetRequiredService<IConsole>();
                console.WriteLine("Please enter a command. Ex: ilos --help");
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            AddApplication(serviceCollection);
            AddCommands(serviceCollection);

            configureServices?.Invoke(serviceCollection);
            return await RunCommand(command, args, serviceCollection);
        }

        private static async Task<int> RunCommand(string command, string[] args, IServiceCollection serviceCollection)
        {
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var console = serviceProvider.GetRequiredService<IConsole>();
            
            try
            {
                var commandServices = serviceProvider.GetServices<ICommandHandler>();
                var handler = commandServices.FirstOrDefault(cs => cs.CanHandle(command));

                if (handler != null)
                {
                    await handler.Handle(args);
                    return 0;
                }

                console.WriteLine("Invalid command! Use: ilos --help");
                return 1;
            }
            catch (Exception ex)
            {
                console.SetForegroundColor(ConsoleColor.Red);
                console.WriteLine($"Command failed: {ex.Message}");
                console.ResetColor();
                return CommandErrorMapper.MapExitCode(ex);
            }
        }
        
        private static void AddCommands(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped <ICommandHandler, InitCommand>();
            serviceCollection.AddScoped <ICommandHandler, StatusCommand>();
            serviceCollection.AddScoped<ICommandHandler, HelpCommand>();
            serviceCollection.AddScoped<ICommandHandler, AddCommand>();
            serviceCollection.AddScoped<ICommandHandler, BranchCommand>();
            serviceCollection.AddScoped<ICommandHandler, VersionCommand>();
            serviceCollection.AddScoped<ICommandHandler, ConfigCommand>();
            serviceCollection.AddScoped<ICommandHandler, CloneCommand>();
            serviceCollection.AddScoped<ICommandHandler, CommitCommand>();
            serviceCollection.AddScoped<ICommandHandler, DiffCommand>();
            serviceCollection.AddScoped<ICommandHandler, LogCommand>();
            serviceCollection.AddScoped<ICommandHandler, ResetCommand>();
            serviceCollection.AddScoped<ICommandHandler, RestoreCommand>();
            serviceCollection.AddScoped<ICommandHandler, SwitchCommand>();
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
            serviceCollection.AddScoped<BranchUseCase>();
            serviceCollection.AddScoped<CloneUseCase>();
            serviceCollection.AddScoped<CommitUseCase>();
            serviceCollection.AddScoped<ConfigUseCase>();
            serviceCollection.AddScoped<DiffUseCase>();
            serviceCollection.AddScoped<HelpUseCase>();
            serviceCollection.AddScoped<InitUseCase>();
            serviceCollection.AddScoped<LogUseCase>();
            serviceCollection.AddScoped<ResetUseCase>();
            serviceCollection.AddScoped<RestoreUseCase>();
            serviceCollection.AddScoped<StatusUseCase>();
            serviceCollection.AddScoped<SwitchUseCase>();
            serviceCollection.AddScoped<VersionUseCase>();
            
            serviceCollection.AddScoped<AddRenderer>();
            serviceCollection.AddScoped<BranchRenderer>();
            serviceCollection.AddScoped<CloneRenderer>();
            serviceCollection.AddScoped<CommitRenderer>();
            serviceCollection.AddScoped<ConfigRenderer>();
            serviceCollection.AddScoped<DiffRenderer>();
            serviceCollection.AddScoped<HelpRenderer>();
            serviceCollection.AddScoped<InitRenderer>();
            serviceCollection.AddScoped<LogRenderer>();
            serviceCollection.AddScoped<ResetRenderer>();
            serviceCollection.AddScoped<RestoreRenderer>();
            serviceCollection.AddScoped<StatusRenderer>();
            serviceCollection.AddScoped<SwitchRenderer>();
            serviceCollection.AddScoped<VersionRenderer>();
        }
    }
}
