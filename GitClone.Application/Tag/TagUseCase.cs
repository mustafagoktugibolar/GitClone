using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Tag;

public sealed class TagUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<TagRequest, TagResult>
{
    public async Task<TagResult> ExecuteAsync(TagRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);

        if (!string.IsNullOrWhiteSpace(request.DeleteName))
        {
            return DeleteTag(session, request.DeleteName);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            return await CreateTag(session, request.Name);
        }

        return ListTags(session);
    }

    private static TagResult ListTags(IRepositorySession session)
    {
        var tags = session.FileSystem.DirectoryExists(session.Context.TagsPath)
            ? session.FileSystem.GetFiles(session.Context.TagsPath)
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .Select(name => name!)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList()
            : [];

        return new TagResult(TagOperation.List, true, "Tags listed.", tags);
    }

    private static async Task<TagResult> CreateTag(IRepositorySession session, string name)
    {
        var tagPath = Path.Combine(session.Context.TagsPath, name);
        if (session.FileSystem.FileExists(tagPath))
        {
            return new TagResult(TagOperation.Create, false, $"Tag already exists: {name}");
        }

        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var headCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);

        if (string.IsNullOrWhiteSpace(headCommitId))
        {
            return new TagResult(TagOperation.Create, false, "Cannot tag: no commits yet.");
        }

        session.FileSystem.CreateDirectory(session.Context.TagsPath);
        await session.FileSystem.WriteAtomic(tagPath, headCommitId);

        return new TagResult(TagOperation.Create, true, $"Created tag '{name}' at {headCommitId}.");
    }

    private static TagResult DeleteTag(IRepositorySession session, string name)
    {
        var tagPath = Path.Combine(session.Context.TagsPath, name);
        if (!session.FileSystem.FileExists(tagPath))
        {
            return new TagResult(TagOperation.Delete, false, $"Tag not found: {name}");
        }

        session.FileSystem.DeleteFile(tagPath);
        return new TagResult(TagOperation.Delete, true, $"Deleted tag '{name}'.");
    }
}
