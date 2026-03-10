namespace GitClone.Application.Add;

public sealed record AddStagedFile(string Path, string Hash);

public sealed record AddResult(
    IReadOnlyList<AddStagedFile> StagedFiles,
    IReadOnlyList<string> IgnoredFiles) : IUseCaseResult;
