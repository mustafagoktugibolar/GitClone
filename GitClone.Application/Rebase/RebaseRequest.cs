namespace GitClone.Application.Rebase;

public sealed record RebaseRequest(string WorkingDirectory, string OntoBranch) : IUseCaseRequest;
