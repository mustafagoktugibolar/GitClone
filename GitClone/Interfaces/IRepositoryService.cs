using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Interfaces
{
    public interface IRepositoryService
    {
        void InitRepository(string? repositoryPath = null);
        void ShowHelp();
    }
}
