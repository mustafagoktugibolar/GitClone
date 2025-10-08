using GitClone.Interfaces;

namespace GitClone.Commands
{
    public class VersionCommand(IVersionService versionService) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("version") || command.Equals("--version") || command.Equals("-v");
        }

        public void Handle(string[] args)
        {
            versionService.ShowVersion();
        }
    }
}
