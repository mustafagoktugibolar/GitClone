using GitClone.Interfaces;

namespace GitClone.Commands
{
    public class InitCommand(IRepositoryService repositoryService) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("init");
        }

        public async Task Handle(string[] args)
        {
            await repositoryService.InitRepository();
        }
    }
}
