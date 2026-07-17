using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Log;

public sealed class LogUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<LogRequest, LogResult>
{
    public async Task<LogResult> ExecuteAsync(LogRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var head = await RepositoryState.ReadHeadReference(session);
        var branchRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var headCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, branchRefPath);

        var entries = new List<LogEntry>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = headCommitId;

        while (!string.IsNullOrWhiteSpace(current))
        {
            if (request.MaxCount is > 0 && entries.Count >= request.MaxCount.Value)
            {
                break;
            }

            if (!visited.Add(current))
            {
                break;
            }

            var commit = await RepositoryState.ReadCommit(session, current);
            if (commit is null)
            {
                throw new InvalidOperationException($"Commit '{current}' is missing.");
            }

            entries.Add(new LogEntry(
                commit.CommitId,
                commit.Message,
                commit.AuthorName,
                commit.AuthorEmail,
                commit.CommittedAtUtc,
                commit.Parents.Length > 1));

            // Follow first-parent only: a linear log through merge commits, same convention
            // `git log --first-parent` uses.
            current = commit.Parents.Length > 0 ? commit.Parents[0] : null;
        }

        return new LogResult(true, false, head.BranchName, entries);
    }
}
