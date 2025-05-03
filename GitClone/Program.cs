using GitClone.Services;
using GitClone.Features;
using GitClone.Interfaces;

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

            var repoService = new RepositoryService(currentDirectory);

            // Inject dependencies manually (or use DI in future)
            IFileSystem fileSystem = new FileSystem();
            IHashService hashService = new HashService();
            IBlobStore blobStore = new BlobStore();
            IIndexManager indexManager = new IndexManager();
            var stagingService = new FileStagingService(fileSystem, hashService, blobStore, indexManager);

            switch (command)
            {
                case "init":
                    repoService.InitRepository();
                    break;
                case "--help":
                    repoService.ShowHelp();
                    break;
                case "add":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Missing file name. Usage: ilos add <filename>");
                        return;
                    }
                    stagingService.AddFile(args[1]);
                    break;
                default:
                    Console.WriteLine("Invalid command! Use: ilos --help");
                    break;
            }
        }
    }
}