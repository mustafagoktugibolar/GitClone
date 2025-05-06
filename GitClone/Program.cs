using GitClone.Services;
using GitClone.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using GitClone.Commands;

namespace GitClone
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a command. Ex: ilos --help");
                return;
            }

            string command = args[0].ToLower();
            string currentDirectory = Directory.GetCurrentDirectory();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IHashService, HashService>();
            serviceCollection.AddSingleton<IFileSystem, FileSystem>();
            serviceCollection.AddSingleton<IBlobStore, BlobStore>();
            serviceCollection.AddSingleton<IIndexManager, IndexManager>();
            serviceCollection.AddSingleton<IRepositoryService, RepositoryService>();
            serviceCollection.AddSingleton<IFileStagingService, FileStagingService>();
            serviceCollection.AddSingleton<IVersionService, VersionService>();

            // commands
            serviceCollection.AddSingleton<ICommandHandler, InitCommand>();
            serviceCollection.AddSingleton<ICommandHandler, HelpCommand>();
            serviceCollection.AddSingleton<ICommandHandler, AddCommand>();
            serviceCollection.AddSingleton<ICommandHandler, VersionCommand>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var commandServices = serviceProvider.GetServices<ICommandHandler>();

            var handler = commandServices.FirstOrDefault(cs => cs.CanHandle(command) == true);

            if (handler != null)
            {
                handler.Handle(args);
            }
            else
            {
                Console.WriteLine("Invalid command! Use: ilos --help");
                return;
            }
        }
    }
}