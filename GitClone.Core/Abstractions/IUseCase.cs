namespace GitClone.Core.Abstractions;

public interface IUseCaseRequest
{
}

public interface IUseCaseResult
{
}

public interface IUseCase<in TRequest, TResult>
    where TRequest : IUseCaseRequest
    where TResult : IUseCaseResult
{
    Task<TResult> ExecuteAsync(TRequest request);
}
