using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Interfaces
{
    public interface IRepositoryContext
    {
        string RootPath { get; set; }
        string IlosPath { get; }
        string ObjectsPath { get; }
        string IndexPath { get; }
        string LocalConfigPath { get; }
    }
}
