using GitClone.Interfaces;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
using GitClone.Helpers;
using GitClone.Models;

namespace GitClone.Services
{
    public class RepositoryService(IBlobStore blobStore, IIndexManager indexManager, IConfigService configService)
        : IRepositoryService
    {
        private readonly string _repositoryPath = Directory.GetCurrentDirectory();

        public void InitRepository()
        {
            blobStore.EnsureDirectory();
            configService.EnsureCreated();
            indexManager.EnsureCreated();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Repository created successfully at {_repositoryPath}" );
            Console.ResetColor();
        }

        public void ShowHelp()
        {
            Console.WriteLine("Usage: ilos <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  init: Create an empty Ilos repository");
            Console.WriteLine("  add: Add file(s) to an Ilos repository");
            Console.WriteLine("  --help: Show help");
            Console.WriteLine("  --version: Show ilos version");
        }
    }
}