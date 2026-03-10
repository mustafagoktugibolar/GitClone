using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Infrastructure.Runtime;

public sealed class RepositorySession(
    IRepositoryContext context,
    IFileSystem fileSystem,
    IHashService hashService,
    IBlobStore blobStore,
    IIndexManager indexManager,
    IIgnoreService ignoreService,
    IConfigService configService,
    IBranchService branchService) : IRepositorySession
{
    public IRepositoryContext Context { get; } = context;
    public IFileSystem FileSystem { get; } = fileSystem;
    public IHashService HashService { get; } = hashService;
    public IBlobStore BlobStore { get; } = blobStore;
    public IIndexManager IndexManager { get; } = indexManager;
    public IIgnoreService IgnoreService { get; } = ignoreService;
    public IConfigService ConfigService { get; } = configService;
    public IBranchService BranchService { get; } = branchService;
}
