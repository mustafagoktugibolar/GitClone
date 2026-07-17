namespace GitClone.Application.CherryPick;

public sealed record CherryPickRequest(string WorkingDirectory, string CommitId) : IUseCaseRequest;
