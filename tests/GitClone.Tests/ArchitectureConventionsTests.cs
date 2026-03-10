using System.Reflection;
using GitClone.Application.Add;
using GitClone.Core.Abstractions;
using Xunit;

namespace GitClone.Tests;

public class ArchitectureConventionsTests
{
    [Fact]
    public void AllUseCases_ImplementGenericUseCaseContract()
    {
        var applicationAssembly = typeof(AddUseCase).Assembly;
        var useCaseTypes = applicationAssembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsPublic: true } &&
                type.Name.EndsWith("UseCase", StringComparison.Ordinal) &&
                type.Namespace != null &&
                type.Namespace.StartsWith("GitClone.Application", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(useCaseTypes);

        foreach (var type in useCaseTypes)
        {
            var implementsUseCase = type
                .GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUseCase<,>));

            Assert.True(implementsUseCase, $"{type.FullName} must implement IUseCase<TRequest, TResult>.");
        }
    }

    [Fact]
    public void AllRequestsAndResults_ImplementMarkerContracts()
    {
        var applicationAssembly = typeof(AddUseCase).Assembly;
        var requestTypes = GetPublicTopLevelTypesEndingWith(applicationAssembly, "Request");
        var resultTypes = GetPublicTopLevelTypesEndingWith(applicationAssembly, "Result");

        Assert.NotEmpty(requestTypes);
        Assert.NotEmpty(resultTypes);

        foreach (var requestType in requestTypes)
        {
            Assert.True(
                typeof(IUseCaseRequest).IsAssignableFrom(requestType),
                $"{requestType.FullName} must implement IUseCaseRequest.");
        }

        foreach (var resultType in resultTypes)
        {
            Assert.True(
                typeof(IUseCaseResult).IsAssignableFrom(resultType),
                $"{resultType.FullName} must implement IUseCaseResult.");
        }
    }

    private static IReadOnlyList<Type> GetPublicTopLevelTypesEndingWith(Assembly assembly, string suffix)
    {
        return assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsPublic: true, IsNested: false } &&
                type.Name.EndsWith(suffix, StringComparison.Ordinal) &&
                type.Namespace != null &&
                type.Namespace.StartsWith("GitClone.Application", StringComparison.Ordinal))
            .ToList();
    }
}
