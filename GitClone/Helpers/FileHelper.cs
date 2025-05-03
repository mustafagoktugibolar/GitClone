using System.Security.Cryptography;
using System.Text;

namespace GitClone.Helpers;

public static class FileHelper
{
    public static bool DoesFileExist(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("File Name is empty");
            return false;
        }
        var fullPath = Path.GetFullPath(fileName);
        
        if (File.Exists(fullPath))
        {
            Console.WriteLine("File does not exist: " + fullPath);
            return false;
        }
        return true;
    }
}