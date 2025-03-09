using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Services
{
    public class RepositoryService
    {
        private readonly string _currentDirectory;
        private readonly string _repositoryPath;

        public RepositoryService(string currentDirectory)
        {
            _currentDirectory = currentDirectory;
            _repositoryPath = Path.Combine(currentDirectory, ".ilos");
        }

        public void InitRepository()
        {
            if (Directory.Exists(_repositoryPath))
            {
                Console.WriteLine("Repository already exists at " + _repositoryPath);
                return;
            }
            Directory.CreateDirectory(_repositoryPath);
            // store commits, trees, blobs
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "objects"));
            // store refs(head, branches, tags)
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "refs"));

            Console.WriteLine("Repository created successfully at " + _repositoryPath);
        }

        public void ShowHelp()
        {
            Console.WriteLine("Usage: ilos <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  init: Create an empty Ilos repository");
            Console.WriteLine(" --help: Show help");
        }
    }
}
