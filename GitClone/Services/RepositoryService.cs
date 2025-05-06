using GitClone.Interfaces;
using System;
using System.IO;

namespace GitClone.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly string _repositoryPath;

        public RepositoryService(string currentDirectory)
        {
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
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "objects"));
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "refs"));

            Console.WriteLine("Repository created successfully at " + _repositoryPath);
        }

        public void ShowHelp()
        {
            Console.WriteLine("Usage: ilos <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  init: Create an empty Ilos repository");
            Console.WriteLine("  add: Add file(s) to an Ilos repository");
            Console.WriteLine("  --help: Show help");
        }
    }
}