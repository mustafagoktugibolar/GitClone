using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class GlobalConfigStrategy(IConfigService configService) : IConfigStrategy
{
    private readonly IConfigService _configService = configService;

    public bool CanExecute(string[] args)
    {
        return args.Length > 2 && args[1] == "--global";
    }

    public void Execute(string[] args)
    {
        if (args.Length >= 4)
        {
            string key =  args[2];
            string value =  args[3];
            
            
            
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Usage: ilos config --global <key> <value>");
            Console.ResetColor();
        }
    }
}