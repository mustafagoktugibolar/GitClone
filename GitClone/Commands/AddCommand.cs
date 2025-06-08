using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Commands
{
    public class AddCommand(IFileStagingService fileStagingService) : ICommandHandler
    {
        public bool CanHandle(string command)
        {
            return command.Equals("add");
        }

        public void Handle(string[] args)
        {
            fileStagingService.AddFile(args[1]);
        }
    }
}
