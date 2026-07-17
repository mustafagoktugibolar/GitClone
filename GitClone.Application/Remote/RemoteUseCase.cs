using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Remote;

/// <summary>
/// Remotes here are local filesystem paths to another .ilos repository (the same way real git
/// supports file-based remotes) rather than URLs served over a network protocol - there is no
/// git wire protocol implementation in this codebase.
/// </summary>
public sealed class RemoteUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<RemoteRequest, RemoteResult>
{
    public async Task<RemoteResult> ExecuteAsync(RemoteRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);

        return request.Action switch
        {
            RemoteAction.Add => await Add(session, request.Name, request.Path),
            RemoteAction.Remove => await Remove(session, request.Name),
            RemoteAction.List => await List(session),
            _ => new RemoteResult(false, request.Action, "Unknown remote action.")
        };
    }

    private static async Task<RemoteResult> Add(IRepositorySession session, string? name, string? path)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
        {
            return new RemoteResult(false, RemoteAction.Add, "A remote name and path are required.");
        }

        var remotes = await RepositoryState.LoadRemotes(session);
        if (remotes.ContainsKey(name))
        {
            return new RemoteResult(false, RemoteAction.Add, $"Remote already exists: {name}");
        }

        remotes[name] = Path.GetFullPath(path);
        await RepositoryState.SaveRemotes(session, remotes);

        return new RemoteResult(true, RemoteAction.Add, $"Added remote '{name}'.");
    }

    private static async Task<RemoteResult> Remove(IRepositorySession session, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new RemoteResult(false, RemoteAction.Remove, "A remote name is required.");
        }

        var remotes = await RepositoryState.LoadRemotes(session);
        if (!remotes.Remove(name))
        {
            return new RemoteResult(false, RemoteAction.Remove, $"Remote not found: {name}");
        }

        await RepositoryState.SaveRemotes(session, remotes);

        return new RemoteResult(true, RemoteAction.Remove, $"Removed remote '{name}'.");
    }

    private static async Task<RemoteResult> List(IRepositorySession session)
    {
        var remotes = await RepositoryState.LoadRemotes(session);
        var entries = remotes
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => new RemoteEntry(kvp.Key, kvp.Value))
            .ToList();

        return new RemoteResult(true, RemoteAction.List, "Remotes listed.", entries);
    }
}
