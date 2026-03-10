using System.Reflection;

namespace GitClone.Application.Version;

public sealed class VersionUseCase : IUseCase<VersionRequest, VersionResult>
{
    public Task<VersionResult> ExecuteAsync(VersionRequest request)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        var version = fileVersion ?? assembly.GetName().Version?.ToString() ?? "unknown";
        return Task.FromResult(new VersionResult(version));
    }
}
