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
        public async Task AddFile(string fileName)
        {
            if (fileName == ".")
            {
                foreach (var file in fileSystem.GetTrackedFilesRecursively())
                    await AddFile(file);
                return;
            }

            string content = fileSystem.Read(fileName).Result;
            string hash = hashService.ComputeSha1(content);

            if (!blobStore.Exists(hash))
                await blobStore.Save(hash, content);

            await indexManager.StageFile(fileName, hash);
        }
    }
}
