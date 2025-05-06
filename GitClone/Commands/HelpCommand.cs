using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Commands
{
    public class HelpCommand : ICommandHandler
    {
        IRepositoryService _repositoryService;

        public HelpCommand(IRepositoryService  repositoryService)
        {
            _repositoryService = repositoryService;
        }
        public bool CanHandle(string command)
        {
            return command.Equals("--help") || command.Equals("help");
        }

        public void Handle(string[] args)
        {
            _repositoryService.ShowHelp();
        }
    }
}
