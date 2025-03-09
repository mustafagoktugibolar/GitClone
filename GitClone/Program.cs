using GitClone.Services;
using System;
using System.IO;

namespace GitClone
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a command. Exm: ilos --help");
                return;
            }

            string command = args[0].ToLower();
            Console.WriteLine(args.Length);
            string currentDirectory = Directory.GetCurrentDirectory();

            RepositoryService repositoryService = new RepositoryService(currentDirectory);

            switch (command)
            {
                case "init":
                    repositoryService.InitRepository();
                    break;
                case "--help":
                   repositoryService.ShowHelp();
                    break;
                default:
                    Console.WriteLine("Invalid command! For more information: ilos --help");
                    break;
            }

        }
    } 
}
