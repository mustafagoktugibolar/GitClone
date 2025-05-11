using GitClone.Interfaces;

namespace GitClone.Commands;

public class GlobalConfigStrategy : IConfigStrategy
{
    private readonly IConfigService _configService;

    public GlobalConfigStrategy(IConfigService configService)
    {
        _configService = configService;
    }
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