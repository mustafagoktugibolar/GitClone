namespace GitClone.Application.Add;

public sealed class AddUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<AddRequest, AddResult>
{
    public async Task<AddResult> ExecuteAsync(AddRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetPath))
        {
            throw new ArgumentException("A file path (or '.') is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var staged = new List<AddStagedFile>();
        var ignored = new List<string>();

        await session.BlobStore.EnsureDirectory();
        await session.IndexManager.EnsureCreated();

        if (request.TargetPath == ".")
        {
            foreach (var file in session.FileSystem.GetTrackedFilesRecursively())
            {
                if (session.IgnoreService.IsIgnored(file))
                {
                    ignored.Add(ToRelativePath(session.Context.RootPath, file));
                    continue;
                }

                await StageFile(session, file, staged);
            }

            return new AddResult(staged, ignored);
        }

        var fullPath = Path.IsPathRooted(request.TargetPath)
            ? Path.GetFullPath(request.TargetPath)
            : Path.GetFullPath(Path.Combine(request.WorkingDirectory, request.TargetPath));

        if (!session.FileSystem.FileExists(fullPath))
        {
            throw new FileNotFoundException($"File does not exist: {request.TargetPath}");
        }

        if (session.IgnoreService.IsIgnored(fullPath))
        {
            ignored.Add(ToRelativePath(session.Context.RootPath, fullPath));
            return new AddResult(staged, ignored);
        }

        await StageFile(session, fullPath, staged);
        return new AddResult(staged, ignored);
    }

    private static async Task StageFile(IRepositorySession session, string fullPath, ICollection<AddStagedFile> staged)
    {
        var relativePath = ToRelativePath(session.Context.RootPath, fullPath);
        var content = await session.FileSystem.ReadBytes(fullPath);
        var hash = session.HashService.ComputeSha1(content);

        if (!session.BlobStore.Exists(hash))
        {
            await session.BlobStore.Save(hash, content);
        }

        await session.IndexManager.StageFile(relativePath, hash);
        staged.Add(new AddStagedFile(relativePath, hash));
    }

    private static string ToRelativePath(string rootPath, string path)
    {
        return Path.GetRelativePath(rootPath, path).Replace('\\', '/');
    }
}
