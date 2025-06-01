using GitClone.Services;
using GitClone.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using GitClone.Commands;
using GitClone.Commands.ConfigStrategies;

namespace GitClone
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a command. Ex: ilos --help");
                return;
            }

            var command = args[0].ToLower();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IHashService, HashService>();
            serviceCollection.AddSingleton<IFileSystem, FileSystem>();
            serviceCollection.AddSingleton<IBlobStore, BlobStore>();
            serviceCollection.AddSingleton<IIndexManager, IndexManager>();
            serviceCollection.AddSingleton<IRepositoryService, RepositoryService>();
            serviceCollection.AddSingleton<IFileStagingService, FileStagingService>();
            serviceCollection.AddSingleton<IVersionService, VersionService>();
            serviceCollection.AddSingleton<IConfigService, ConfigService>();

            // commands
            serviceCollection.AddSingleton<ICommandHandler, InitCommand>();
            serviceCollection.AddSingleton<ICommandHandler, HelpCommand>();
            serviceCollection.AddSingleton<ICommandHandler, AddCommand>();
            serviceCollection.AddSingleton<ICommandHandler, VersionCommand>();
            serviceCollection.AddSingleton<ICommandHandler, ConfigCommand>();
            serviceCollection.AddSingleton<IConfigStrategy, AddGlobalConfigStrategy>();
            serviceCollection.AddSingleton<IConfigStrategy, EditGlobalConfigStrategy>();
            serviceCollection.AddSingleton<IConfigStrategy, DeleteGlobalConfigStrategy>();
            serviceCollection.AddSingleton<IConfigStrategy, ShowGlobalConfigStrategy>();
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var commandServices = serviceProvider.GetServices<ICommandHandler>();

            var handler = commandServices.FirstOrDefault(cs => cs.CanHandle(command));

            if (handler != null)
            {
                handler.Handle(args);
            }
            else
            {
                Console.WriteLine("Invalid command! Use: ilos --help");
            }
        }
    }
}