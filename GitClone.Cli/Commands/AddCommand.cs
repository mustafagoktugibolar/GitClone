using GitClone.Core.Abstractions;
using GitClone.Application.Add;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands
{
    public class AddCommand(
        AddUseCase addUseCase,
        AddRenderer renderer,
        IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("add");
        }

        public async Task Handle(string[] args)
        {
            if (args.Length < 2)
            {
                renderer.RenderUsage("Missing file argument.");
                return;
            }

            if (args[1].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                renderer.RenderUsage();
                return;
            }

            var request = new AddRequest(workingDirectoryProvider.GetCurrentDirectory(), args[1]);
            var result = await addUseCase.ExecuteAsync(request);
            renderer.Render(result);
        }
    }
}
