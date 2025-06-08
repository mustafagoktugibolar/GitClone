using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

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
