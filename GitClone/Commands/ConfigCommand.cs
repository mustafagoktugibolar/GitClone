using GitClone.Interfaces;

namespace GitClone.Commands;

public class ConfigCommand(IEnumerable<ICommandStrategy> strategies) : ICommandHandler
{
    private List<ICommandStrategy>  _strategies = strategies.ToList();

    public bool CanHandle(string command)
    {
        return command.Equals("config", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(string[] args)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanExecute(args));
        if (strategy != null)
        {
            await strategy.Execute(args); 
        }
        else
        {
            Console.WriteLine("Unknown config command. Try --global or --l");
        }
    }
}