using GitClone.Interfaces;

namespace GitClone.Commands;

public class ConfigCommand(IEnumerable<IConfigStrategy> strategies) : ICommandHandler
{
    private List<IConfigStrategy>  _strategies = strategies.ToList();

    public bool CanHandle(string command)
    {
        return command.Equals("config", StringComparison.OrdinalIgnoreCase);
    }

    public void Handle(string[] args)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanExecute(args));
        if (strategy != null)
        {
            strategy.Execute(args); 
        }
        else
        {
            Console.WriteLine("Unknown config command. Try --global or --l");
        }
    }
}