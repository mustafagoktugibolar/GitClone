using GitClone.Core.Abstractions;
using GitClone.Infrastructure.Helpers;
using GitClone.Infrastructure.Models;
using GitClone.Infrastructure.Services;

namespace GitClone.Infrastructure.Runtime;

public sealed class RepositorySessionFactory(
    IWorkingDirectoryProvider workingDirectoryProvider,
    IConsole console,
    IPasswordPrompter passwordPrompter) : IRepositorySessionFactory
{
    public IRepositorySession CreateForCurrentDirectory(bool forInit = false)
    {
        return CreateForPath(workingDirectoryProvider.GetCurrentDirectory(), forInit);
    }

    public IRepositorySession CreateForPath(string workingDirectory, bool forInit = false)
    {
        var rootPath = forInit ? RepoLocator.GetInitRoot(workingDirectory) : RepoLocator.FindRepoRoot(workingDirectory);
        var context = new RepositoryContext(rootPath);
        var fileSystem = new FileSystem(context);
        var hashService = new HashService();
        var blobStore = new BlobStore(context, fileSystem);
        var indexManager = new IndexManager(context, fileSystem);
        var ignoreService = new IgnoreService(context);
        var configService = new ConfigService(hashService, context, fileSystem, console, passwordPrompter);
        var branchService = new BranchService(context, fileSystem);

        return new RepositorySession(
            context,
            fileSystem,
            hashService,
            blobStore,
            indexManager,
            ignoreService,
            configService,
            branchService);
    }
}
