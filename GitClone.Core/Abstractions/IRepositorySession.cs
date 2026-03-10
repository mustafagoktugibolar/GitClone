using GitClone.Core.Interfaces;

namespace GitClone.Core.Abstractions;

public interface IRepositorySession
{
    IRepositoryContext Context { get; }
    IFileSystem FileSystem { get; }
    IHashService HashService { get; }
    IBlobStore BlobStore { get; }
    IIndexManager IndexManager { get; }
    IIgnoreService IgnoreService { get; }
    IConfigService ConfigService { get; }
    IBranchService BranchService { get; }
}
