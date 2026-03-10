using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Core.Interfaces
{
    public interface ICommandHandler
    {
        bool CanHandle(string command);
        Task Handle(string[] args);
    }
}
