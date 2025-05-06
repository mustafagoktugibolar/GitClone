using GitClone.Interfaces;

namespace GitClone.Commands
{
    public class InitCommand : ICommandHandler
    {
        IRepositoryService _repositoryService;

        public InitCommand(IRepositoryService repositoryService)
        {
            _repositoryService = repositoryService;
        }
        public bool CanHandle(string command)
        {
            return command.Equals("init");
        }

        public void Handle(string[] args)
        {
            _repositoryService.InitRepository();
        }
    }
}
