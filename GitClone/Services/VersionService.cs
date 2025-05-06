using GitClone.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GitClone.Services
{
    public class VersionService : IVersionService
    {
        public void ShowVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var version = attribute?.Version;
            Console.WriteLine("ilos " + version);
        }
    }
}
