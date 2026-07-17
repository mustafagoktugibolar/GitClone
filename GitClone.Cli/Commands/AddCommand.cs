using System.CommandLine;
using GitClone.Core.Abstractions;
using GitClone.Application.Add;
using GitClone.Cli.Rendering;

namespace GitClone.Cli.Commands
{
    public class AddCommand(
        AddUseCase addUseCase,
        AddRenderer renderer,
        IWorkingDirectoryProvider workingDirectoryProvider)
    {
        public Command Build()
        {
            var pathspecArgument = new Argument<string>("pathspec")
            {
                Description = "File to stage, or '.' to stage everything"
            };

            var command = new Command("add", "Stage file contents for the next commit");
            command.Arguments.Add(pathspecArgument);

            command.SetAction(async (parseResult, _) =>
            {
                var path = parseResult.GetValue(pathspecArgument)!;
                var request = new AddRequest(workingDirectoryProvider.GetCurrentDirectory(), path);
                var result = await addUseCase.ExecuteAsync(request);
                renderer.Render(result);
                return 0;
            });

            return command;
        }
    }
}
