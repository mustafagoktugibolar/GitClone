using GitClone.Commands;
using GitClone.Commands.ConfigStrategies;
using GitClone.Services;
using GitClone.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
            serviceCollection.AddScoped<ICommandStrategy, AddGlobalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, AddLocalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, EditGlobalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, EditLocalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, RemoveGlobalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, RemoveLocalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, ShowGlobalCommandStrategy>();
            serviceCollection.AddScoped<ICommandStrategy, ShowLocalCommandStrategy>();
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