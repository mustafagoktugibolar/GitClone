using GitClone.Interfaces;

namespace GitClone.Services
{
    public class FileStagingService : IFileStagingService
    {
        private readonly IFileSystem fileSystem;
        private readonly IHashService hashService;
        private readonly IBlobStore blobStore;
        private readonly IIndexManager indexManager;

        public FileStagingService(
            IFileSystem fileSystem,
            IHashService hashService,
            IBlobStore blobStore,
            IIndexManager indexManager)
        {
            this.fileSystem = fileSystem;
            this.hashService = hashService;
            this.blobStore = blobStore;
            this.indexManager = indexManager;
        }

        public void AddFile(string fileName)
        {
            if (fileName == ".")
            {
                foreach (var file in fileSystem.GetTrackedFilesRecursively())
                    AddFile(file);
                return;
            }

            string content = fileSystem.Read(fileName);
            string hash = hashService.ComputeSha1(content);

            if (!blobStore.Exists(hash))
                blobStore.Save(hash, content);

            indexManager.StageFile(fileName, hash);
        }
    }
}
