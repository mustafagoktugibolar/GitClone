using GitClone.Interfaces;

namespace GitClone.Services
{
    public class FileStagingService(
        IFileSystem fileSystem,
        IHashService hashService,
        IBlobStore blobStore,
        IIndexManager indexManager,
        IIgnoreService ignoreService)
        : IFileStagingService
    {
        public async Task AddFile(string fileName)
        {
            if (fileName == ".")
            {
                await blobStore.EnsureDirectory();

                foreach (var file in fileSystem.GetTrackedFilesRecursively())
                {
                    if (ignoreService.IsIgnored(file))
                    {
                        continue;
                    }
                    
                    var content = await fileSystem.Read(file);
                    var hash = hashService.ComputeSha1(content);

                    if (!blobStore.Exists(hash))
                    {
                        await blobStore.Save(hash, content);
                    }
                    await indexManager.StageFile(file, hash);
                }

                return;
            }

            if (ignoreService.IsIgnored(fileName))
            {
                return;
            }
            var singleContent = await fileSystem.Read(fileName);
            var singleHash = hashService.ComputeSha1(singleContent);

            await blobStore.EnsureDirectory();
            if (!blobStore.Exists(singleHash))
            {
                await blobStore.Save(singleHash, singleContent);
            }
            
            await indexManager.StageFile(fileName, singleHash);
        }
    }
}
