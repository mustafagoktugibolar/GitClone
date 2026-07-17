using System.Text;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Shared;

/// <summary>
/// Shared by any use case that can produce a three-way merge conflict (merge, cherry-pick,
/// stash apply): auto-merges and stages everything that didn't conflict, writes git-style
/// conflict markers into the working tree for conflicting text files, and leaves binary
/// conflicts untouched (reported, not corrupted with markers).
/// </summary>
internal static class ConflictResolution
{
    public static async Task<List<MergeConflict>> WriteConflictsAndPartialMerge(
        IRepositorySession session,
        string rootPath,
        string oursLabel,
        string theirsLabel,
        TreeMergeResult mergeResult,
        IReadOnlyDictionary<string, string> oursTree,
        IReadOnlyDictionary<string, string> theirsTree)
    {
        var conflictPaths = mergeResult.Conflicts.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cleanlyRemoved = oursTree.Keys.Concat(theirsTree.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !conflictPaths.Contains(path) && !mergeResult.Tree.ContainsKey(path));

        foreach (var path in cleanlyRemoved)
        {
            var fullPath = RepositoryState.ToFullPath(rootPath, path);
            if (session.FileSystem.FileExists(fullPath))
            {
                session.FileSystem.DeleteFile(fullPath);
            }
        }

        foreach (var entry in mergeResult.Tree)
        {
            await RepositoryState.WriteBlobToWorkingTree(session, rootPath, entry.Key, entry.Value);
        }

        var newIndex = new Dictionary<string, string>(mergeResult.Tree, StringComparer.OrdinalIgnoreCase);
        foreach (var path in mergeResult.Conflicts)
        {
            // Keep our version staged for a conflicting path (mirrors git leaving the "ours"
            // stage as what a plain `commit` would otherwise pick up); a path we deleted while
            // the other side changed it has nothing to keep staged.
            if (oursTree.TryGetValue(path, out var oursHash))
            {
                newIndex[path] = oursHash;
            }
        }
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, newIndex);

        var conflicts = new List<MergeConflict>();
        foreach (var path in mergeResult.Conflicts)
        {
            oursTree.TryGetValue(path, out var oursHash);
            theirsTree.TryGetValue(path, out var theirsHash);

            var oursContent = oursHash is null ? null : await RepositoryState.ReadBlob(session, oursHash);
            var theirsContent = theirsHash is null ? null : await RepositoryState.ReadBlob(session, theirsHash);

            var canWriteMarkers = (oursContent is null || IsLikelyText(oursContent)) &&
                                   (theirsContent is null || IsLikelyText(theirsContent));

            if (canWriteMarkers)
            {
                var markerContent = BuildConflictMarkers(oursLabel, oursContent, theirsLabel, theirsContent);
                var fullPath = RepositoryState.ToFullPath(rootPath, path);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    session.FileSystem.CreateDirectory(directory);
                }

                await session.FileSystem.WriteAtomicBytes(fullPath, markerContent);
            }

            conflicts.Add(new MergeConflict(path, canWriteMarkers));
        }

        return conflicts;
    }

    private static byte[] BuildConflictMarkers(string oursLabel, byte[]? oursContent, string theirsLabel, byte[]? theirsContent)
    {
        var builder = new StringBuilder();
        AppendSection(builder, $"<<<<<<< {oursLabel}", oursContent);
        builder.Append("=======\n");
        AppendSection(builder, null, theirsContent);
        builder.Append(">>>>>>> ").Append(theirsLabel).Append('\n');
        return Encoding.UTF8.GetBytes(builder.ToString());

        static void AppendSection(StringBuilder builder, string? header, byte[]? content)
        {
            if (header is not null)
            {
                builder.Append(header).Append('\n');
            }

            if (content is not null && content.Length > 0)
            {
                var text = Encoding.UTF8.GetString(content);
                builder.Append(text);
                if (!text.EndsWith('\n'))
                {
                    builder.Append('\n');
                }
            }
        }
    }

    private static bool IsLikelyText(byte[] content)
    {
        if (content.Length == 0)
        {
            return true;
        }

        if (Array.IndexOf(content, (byte)0) >= 0)
        {
            return false;
        }

        try
        {
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(content);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
