using GitClone.Core.Abstractions;

namespace GitClone.Application.Init;

public sealed class InitUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<InitRequest, InitResult>
{
    public async Task<InitResult> ExecuteAsync(InitRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory, forInit: true);
        var repositoryExists = session.FileSystem.DirectoryExists(session.Context.IlosPath);

        if (!repositoryExists)
        {
            session.FileSystem.CreateDirectory(session.Context.IlosPath);
        }

        session.IgnoreService.EnsureCreated();
        await session.BlobStore.EnsureDirectory();
        session.FileSystem.CreateDirectory(session.Context.CommitsPath);
        await session.ConfigService.EnsureCreated();
        await session.IndexManager.EnsureCreated();
        await session.BranchService.EnsureCreated();

        return new InitResult(session.Context.RootPath, !repositoryExists);
    }
}
