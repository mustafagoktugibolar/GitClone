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
            if (args[1].Equals("help") || args[1].Equals("-h"))
            {
                ShowHelp();
            }
            fileStagingService.AddFile(args[1]);
        }

        private void ShowHelp()
        {
            throw new NotImplementedException();
        }
    }
}
