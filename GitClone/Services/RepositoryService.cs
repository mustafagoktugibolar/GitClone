using GitClone.Interfaces;

namespace GitClone.Services
{
    public class RepositoryService(IBlobStore blobStore, IBranchService branchService, IIndexManager indexManager, IConfigService configService, IRepositoryContext repositoryContext, IIgnoreService ignoreService)
        : IRepositoryService
    {
        private string RepositoryPath => repositoryContext.RootPath;

        public async Task InitRepository(string? repositoryPath = null)
        {
            if(!Directory.Exists(repositoryContext.IlosPath))
            {
                Directory.CreateDirectory(repositoryPath == null ? repositoryContext.IlosPath : Path.Combine(repositoryPath, ".ilos"));
            }

            ignoreService.EnsureCreated();
            await blobStore.EnsureDirectory();
            await configService.EnsureCreated();
            await indexManager.EnsureCreated();
            await branchService.EnsureCreated();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Repository created successfully at {RepositoryPath}" );
            Console.ResetColor();
        }

        public Task ShowHelp()
        {
            Console.WriteLine("Usage: ilos <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  init: Create an empty Ilos repository");
            Console.WriteLine("  add: Add file(s) to an Ilos repository");
            Console.WriteLine("  --help: Show help");
            Console.WriteLine("  --version: Show ilos version");
            return Task.CompletedTask;
        }
    }
}