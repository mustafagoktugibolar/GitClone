using GitClone.Core.Interfaces;

namespace GitClone.Infrastructure.Models
{
    public sealed class RepositoryContext(string rootPath) : IRepositoryContext
    {
        public string RootPath { get; init; } = Path.GetFullPath(rootPath);
        public string IlosPath => Path.Combine(RootPath, ".ilos");
        public string ObjectsPath => Path.Combine(IlosPath, "objects");
        public string CommitsPath => Path.Combine(IlosPath, "commits");
        public string IndexPath => Path.Combine(IlosPath, "index");
        public string HEADPath => Path.Combine(IlosPath, "HEAD");
        public string RefsPath => Path.Combine(IlosPath, "refs");
        public string HeadsPath => Path.Combine(RefsPath, "heads");
        public string TagsPath => Path.Combine(RefsPath, "tags");
        public string RemotesPath => Path.Combine(RefsPath, "remotes");
        public string RemotesConfigPath => Path.Combine(IlosPath, "remotes.json");
        public string StashPath => Path.Combine(IlosPath, "stash");
        public string LocalConfigPath => Path.Combine(IlosPath, "config.json");
        public string IgnorePath => Path.Combine(RootPath, ".ilosignore");
        public string MergeHeadPath => Path.Combine(IlosPath, "MERGE_HEAD");
    }
}
