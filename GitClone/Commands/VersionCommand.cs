using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Commands
{
    public class VersionCommand : ICommandHandler
    {
        IVersionService _versionService;

        public VersionCommand(IVersionService versionService)
        {
            _versionService = versionService;
        }
        public bool CanHandle(string command)
        {
            return command.Equals("version") || command.Equals("--version") || command.Equals("-v");
        }

        public void Handle(string[] args)
        {
            _versionService.ShowVersion();
        }
    }
}
