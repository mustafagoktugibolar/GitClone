using GitClone.Core.Abstractions;
using GitClone.Application.Init;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands
{
    public class InitCommand(InitUseCase initUseCase, InitRenderer renderer, IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("init");
        }

        public async Task Handle(string[] args)
        {
            var request = new InitRequest(workingDirectoryProvider.GetCurrentDirectory());
            var result = await initUseCase.ExecuteAsync(request);
            renderer.Render(result);
        }
    }
}
