namespace GitClone.Application.Init;

public sealed record InitResult(string RepositoryRoot, bool CreatedNewRepository) : IUseCaseResult;
