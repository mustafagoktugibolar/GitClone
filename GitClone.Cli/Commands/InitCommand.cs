using System.CommandLine;
using GitClone.Application.Init;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands
{
    public class InitCommand(InitUseCase initUseCase, InitRenderer renderer, IWorkingDirectoryProvider workingDirectoryProvider)
    {
        public Command Build()
        {
            var command = new Command("init", "Create an empty repository");

            command.SetAction(async (_, _) =>
            {
                var request = new InitRequest(workingDirectoryProvider.GetCurrentDirectory());
                var result = await initUseCase.ExecuteAsync(request);
                renderer.Render(result);
                return 0;
            });

            return command;
        }
    }
}
