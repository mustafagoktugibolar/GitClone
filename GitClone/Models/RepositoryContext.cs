using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Models
{
    public sealed class RepositoryContext(string rootPath) : IRepositoryContext
    {
        public string RootPath { get; init; } = Path.GetFullPath(rootPath);
        public string IlosPath => Path.Combine(RootPath, ".ilos");
        public string ObjectsPath => Path.Combine(IlosPath, "objects");
        public string IndexPath => Path.Combine(IlosPath, "index");
        public string HEADPath => Path.Combine(IlosPath, "HEAD");
        public string RefsPath => Path.Combine(IlosPath, "refs");
        public string HeadsPath => Path.Combine(RefsPath, "heads");
        public string LocalConfigPath => Path.Combine(IlosPath, "config.json");
        public string IgnorePath => Path.Combine(RootPath, ".ilosignore");
    }
}
