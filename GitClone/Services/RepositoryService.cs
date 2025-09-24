using GitClone.Interfaces;

namespace GitClone.Services
{
    public class RepositoryService(IRepositoryContext repositoryContext, IBlobStore blobStore, IIndexManager indexManager, IConfigService configService, IBranchService branchService)
        : IRepositoryService
    {
        private readonly string _repositoryPath = repositoryContext.RootPath;

        public void InitRepository()
        {
            if(!Directory.Exists(repositoryContext.IlosPath))
            {
                Directory.CreateDirectory(repositoryContext.IlosPath);
            }
            blobStore.EnsureDirectory();
            configService.EnsureCreated();
            indexManager.EnsureCreated();
            branchService.EnsureCreated();
            
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