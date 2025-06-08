using GitClone.Interfaces;

namespace GitClone.Services
{
    public class FileStagingService(
        IFileSystem fileSystem,
        IHashService hashService,
        IBlobStore blobStore,
        IIndexManager indexManager)
        : IFileStagingService
    {
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
