using System.Text.Json;
using GitClone.Services;
using GitClone.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using GitClone.Commands;
using GitClone.Commands.ConfigStrategies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GitClone.Models;

namespace GitClone
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a command. Ex: ilos --help");
                return;
            }

            var command = args[0].ToLower();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IRepositoryContext, RepositoryContext>();
            serviceCollection.AddScoped<IFileSystem, FileSystem>();
            serviceCollection.AddScoped<IHashService, HashService>();
            serviceCollection.AddScoped<IBlobStore, BlobStore>();
            serviceCollection.AddScoped<IIndexManager, IndexManager>();
            serviceCollection.AddScoped<IFileStagingService, FileStagingService>();
            serviceCollection.AddScoped<IVersionService, VersionService>();
            serviceCollection.AddScoped<IConfigService, ConfigService>();
            serviceCollection.AddScoped<ICloneService, CloneService>();
            serviceCollection.AddScoped<IBranchService, BranchService>();
            serviceCollection.AddScoped<IRepositoryService, RepositoryService>();

            // commands
            serviceCollection.AddScoped <ICommandHandler, InitCommand>();
            serviceCollection.AddScoped<ICommandHandler, HelpCommand>();
            serviceCollection.AddScoped<ICommandHandler, AddCommand>();
            serviceCollection.AddScoped<ICommandHandler, VersionCommand>();
            serviceCollection.AddScoped<ICommandHandler, ConfigCommand>();
            serviceCollection.AddScoped<IConfigStrategy, AddGlobalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, AddLocalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, EditGlobalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, EditLocalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, RemoveGlobalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, RemoveLocalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, ShowGlobalConfigStrategy>();
            serviceCollection.AddScoped<IConfigStrategy, ShowLocalConfigStrategy>();
            serviceCollection.AddScoped<ICommandHandler, CloneCommand>();
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var commandServices = serviceProvider.GetServices<ICommandHandler>();

            var handler = commandServices.FirstOrDefault(cs => cs.CanHandle(command));

            if (handler != null)
            {
                await handler.Handle(args);
            }
            else
            {
                Console.WriteLine("Invalid command! Use: ilos --help");
            }
        }
    }
}