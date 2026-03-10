using GitClone.Application.Help;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands
{
    public class HelpCommand(HelpUseCase helpUseCase, HelpRenderer renderer) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("--help") || command.Equals("help") || command.Equals("-h");
        }

        public async Task Handle(string[] args)
        {
            var result = await helpUseCase.ExecuteAsync(new HelpRequest());
            renderer.Render(result);
        }
    }
}
