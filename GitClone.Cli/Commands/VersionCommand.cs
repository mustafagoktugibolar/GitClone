using GitClone.Application.Version;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands
{
    public class VersionCommand(VersionUseCase versionUseCase, VersionRenderer renderer) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("version") || command.Equals("--version") || command.Equals("-v");
        }

        public async Task Handle(string[] args)
        {
            var result = await versionUseCase.ExecuteAsync(new VersionRequest());
            renderer.Render(result);
        }
    }
}
