using GitClone.Core.Abstractions;

namespace GitClone.Infrastructure.Runtime;

public sealed class WorkingDirectoryProvider : IWorkingDirectoryProvider
{
    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }
}
