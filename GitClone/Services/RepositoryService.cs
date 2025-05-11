using GitClone.Interfaces;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
using GitClone.Helpers;
using GitClone.Models;

namespace GitClone.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly string _repositoryPath;
        private readonly IBlobStore _blobStore;
        private readonly IIndexManager _indexManager;
        private readonly IConfigService _configService;

        public RepositoryService(IBlobStore blobStore, IIndexManager indexManager, IConfigService configService)
        {
            _repositoryPath = Directory.GetCurrentDirectory();
            _blobStore = blobStore;
            _indexManager = indexManager;
            _configService = configService;
        }

        public void InitRepository()
        {
            _blobStore.EnsureDirectory();
            _configService.EnsureCreated();
            _indexManager.EnsureCreated();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Repository created successfully at {_repositoryPath}" );
            Console.ResetColor();
        }

        public void ShowHelp()
        {
            Console.WriteLine("dededede");
            Console.WriteLine("Usage: ilos <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  init: Create an empty Ilos repository");
            Console.WriteLine("  add: Add file(s) to an Ilos repository");
            Console.WriteLine("  --help: Show help");
            Console.WriteLine("  --version: Show ilos version");
        }
    }
}