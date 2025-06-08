using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Commands
{
    public class HelpCommand(IRepositoryService repositoryService) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("--help") || command.Equals("help") || command.Equals("-h");
        }

        public void Handle(string[] args)
        {
            repositoryService.ShowHelp();
        }
    }
}
