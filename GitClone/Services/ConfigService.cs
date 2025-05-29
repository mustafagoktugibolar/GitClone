using System.Text.Json;
using GitClone.Interfaces;
using GitClone.Models;

namespace GitClone.Services;

public class ConfigService : IConfigService
{
    private readonly string _globalConfigPath;
    private readonly string _localConfigPath;
    private readonly JsonSerializerOptions _jsonOptions;
    public ConfigService()
    {
        string repoPath = Path.Combine(Environment.CurrentDirectory, ".ilos");
        _localConfigPath = Path.Combine(repoPath, "configs");
        _globalConfigPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ilos"), "config.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }


    public void InitLocalConfig()
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
                        var jsonString = JsonSerializer.Serialize(localConfig, _jsonOptions);
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

    public void ShowConfigs()
    {
        throw new NotImplementedException();
    }

    public void EnsureCreated()
    {
        string globalConfigDir = Path.GetDirectoryName(_globalConfigPath)!;
        if (!Directory.Exists(globalConfigDir))
        {
            Directory.CreateDirectory(globalConfigDir);
        }

        if (!File.Exists(_globalConfigPath))
        {
            CreateGlobalConfig();
        }

        if (!Directory.Exists(_localConfigPath))
        {
            Directory.CreateDirectory(_localConfigPath);
        }

        if (!File.Exists(_localConfigPath))
        {
            InitLocalConfig();
        }
    }

    private void CreateGlobalConfig()
    {
        try
        {
            File.WriteAllText(_globalConfigPath, "{}");
            var user = new User() { Username = Environment.UserName };
            var gc = new Config() { Configs = [user], ActiveUser = user.Username };
        
            var jsonString = JsonSerializer.Serialize(gc, _jsonOptions);
            File.WriteAllText(_globalConfigPath, jsonString);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error creating global config");
            Console.WriteLine(e.Message);
            throw;
        }
    }
}