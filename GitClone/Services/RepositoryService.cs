using GitClone.Interfaces;

namespace GitClone.Services
{
    public class RepositoryService(IBlobStore blobStore, IIndexManager indexManager, IConfigService configService)
        : IRepositoryService
    {
        private readonly string _repositoryPath = Directory.GetCurrentDirectory();

        public void InitRepository(string? repositoryPath = null)
        {
            if(!Directory.Exists(Path.Combine(_repositoryPath, ".ilos")))
            {
                Directory.CreateDirectory(repositoryPath == null ? Path.Combine(_repositoryPath, ".ilos") : Path.Combine(repositoryPath, ".ilos"));
            }
            blobStore.EnsureDirectory();
            configService.EnsureCreated();
            indexManager.EnsureCreated();
            //logHelper.EnsureCreated();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Repository created successfully at {_repositoryPath}" );
            Console.ResetColor();
        }

        public void ShowHelp()
        {
            Console.WriteLine("Usage: ilos <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  init: Create an empty Ilos repository");
            Console.WriteLine("  add: Add file(s) to an Ilos repository");
            Console.WriteLine("  --help: Show help");
            Console.WriteLine("  --version: Show ilos version");
        }
    }
}