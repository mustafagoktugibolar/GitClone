using System.Text.Json;
using GitClone.Interfaces;
using GitClone.Models;

namespace GitClone.Services;

public class ConfigService : IConfigService
{
    private readonly string _globalConfigPath;
    private readonly string _localConfigPath;
    
    public ConfigService()
    {
        string repoPath = Path.Combine(Environment.CurrentDirectory, ".ilos");
        _localConfigPath = Path.Combine(repoPath, "configs");
        _globalConfigPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ilos"), "config.json");
    }


    public void InitConfig()
    {
        try
        {
            var jsonFile = File.ReadAllText(_globalConfigPath);
            if (jsonFile.Length > 0)
            {
                var configs = JsonSerializer.Deserialize<Config>(jsonFile);
                if (configs != null)
                {
                    var activeConfig = configs.Configs.FirstOrDefault(c => c.Username == configs.ActiveUser);
                    if (activeConfig != null)
                    {
                        List<User> newConfigs = new();
                        newConfigs.Add(activeConfig);
                        var localConfig = new Config() { Configs = newConfigs, ActiveUser = activeConfig.Username };
                        var jsonString = JsonSerializer.Serialize(localConfig, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(Path.Combine(_localConfigPath, "config.json"), jsonString);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public void CreateConfig()
    {
        throw new NotImplementedException();
    }

    public void DeleteConfig()
    {
        throw new NotImplementedException();
    }

    public void ShowConfig()
    {
        throw new NotImplementedException();
    }

    public void EnsureCreated()
    {
        if (!File.Exists(_globalConfigPath))
        {
            File.WriteAllText(_globalConfigPath, "{}");
        }
        if (!Directory.Exists(_localConfigPath))
        {
            Directory.CreateDirectory(_localConfigPath);
        }
    }
}