using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Interfaces
{
    public interface IRepositoryContext
    {
        string RootPath { get; init; }
        string IlosPath { get; }
        string ObjectsPath { get; }
        string IndexPath { get; }
        string RefsPath { get; }
        string HEADPath { get; }
        string HeadsPath { get; }
        string LocalConfigPath { get; }
    }
}
