using System.Text.Json;
using GitClone.Helpers;
using GitClone.Interfaces;
using GitClone.Models;

namespace GitClone.Services;

public class ConfigService : IConfigService
{
    private readonly string _globalConfigPath;
    private string _localConfigPath;
    private readonly JsonSerializerOptions? _jsonOptions;
    private readonly IHashService _hashService;
    public ConfigService(IHashService hashService)
    {
        _hashService = hashService;
        _globalConfigPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ilos"), "config.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    #region FILE_CREATION
    public void InitLocalConfig()
    {
        try
        {
            var jsonFile = File.ReadAllText(_globalConfigPath);
            if (jsonFile.Length <= 0) 
                return;
            
            var configs = JsonSerializer.Deserialize<Config>(jsonFile);
            var activeConfig = configs?.Configs.FirstOrDefault(c => c.Mail == configs.ActiveUser);
            
            if (activeConfig == null) 
                return;
            var localDir = Path.GetDirectoryName(_localConfigPath)!;
            if (!Directory.Exists(localDir))
                Directory.CreateDirectory(localDir);
            
            var newConfigs = new List<User>() { activeConfig };
            var localConfig = new Config() { Configs = newConfigs, ActiveUser = activeConfig.Mail };
            SaveConfig(localConfig, _localConfigPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            throw;
        }
    }
    
    public void EnsureCreated()
    {
        var repoPath = Path.Combine(Environment.CurrentDirectory, ".ilos");
        _localConfigPath = Path.Combine(repoPath, "configs", "config.json");
        var globalConfigDir = Path.GetDirectoryName(_globalConfigPath)!;
        if (!Directory.Exists(globalConfigDir))
        {
            Directory.CreateDirectory(globalConfigDir);
        }

        if (!File.Exists(_globalConfigPath))
        {
            CreateGlobalConfigFile();
        }

        var localConfigDir = Path.GetDirectoryName(_localConfigPath);
        if (localConfigDir != null && !Directory.Exists(localConfigDir))
        {
            Directory.CreateDirectory(localConfigDir);
        }

        if (!File.Exists(_localConfigPath))
        {
            InitLocalConfig();
        }
    }
    
    private void CreateGlobalConfigFile()
    {
        try
        {
            File.WriteAllText(_globalConfigPath, "{}");
            var user = new User() { Username = Environment.UserName, Mail = $"{Environment.UserName}@localhost"};
            var gc = new Config() { Configs = [user], ActiveUser = user.Mail };
            SaveConfig(gc, _globalConfigPath);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error creating global config");
            Console.WriteLine(e.Message);
            Console.Error.WriteLine();
            throw;
        }
    }
    #endregion
    
    #region SHOW

    public void ShowLocalConfigs()
    {   
        var gc = GetLocalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        Console.WriteLine("Local Configs:");
        if (gc.Configs.Count == 0)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find any configs");
        }
        foreach (var config in gc.Configs)
        {
            Console.WriteLine($"  [{config.Username} {config.Mail}]");
        }
    }

    public void ShowGlobalConfigs()
    {   
        var gc = GetGlobalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        Console.WriteLine("Global Configs:");
        if (gc.Configs.Count == 0)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find any configs");
        }
        foreach (var config in gc.Configs)
        {
            Console.WriteLine($"  [{config.Username} {config.Mail}]");
        }
    }
    #endregion
    
    #region ADD

    private void AddConfig(Config config, string username, string email, string password, string filePath)
    {
        // check is user exists (PK is email)
        if (!config.Configs.Exists(c => c.Mail == email))
        {
            var user = new User() { Username = username, Mail = email, PasswordHash = _hashService.ComputeSha256(password) };
            config.Configs.Add(user);
            
            if (config.Configs.Any(c =>
                    c.Username == Environment.UserName && c.Mail == $"{Environment.UserName}@localhost"))
            {
                config.Configs.RemoveAll(c =>
                    c.Username == Environment.UserName &&
                    c.Mail == $"{Environment.UserName}@localhost");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Config created successfully");
            Console.ResetColor();
            
            Console.Write("Do you want to make the user active? (Y/N):");
            var isActive = Console.ReadLine();
            
            if (isActive?.Trim().ToLower() == "y")
            {
                config.ActiveUser = user.Mail;
            }

            SaveConfig(config, filePath);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Users null or user exists!");
        }
    }
    public void AddGlobalConfig(string username, string email, string password)
    {
        var config = GetGlobalConfig();
        if (config == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        AddConfig(config, username, email, password, _globalConfigPath);
    }
    public void AddLocalConfig(string username, string email, string password)
    {
        var config = GetLocalConfig();
        if (config == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        AddConfig(config, username, email, password, _localConfigPath);
    }
    #endregion

    #region EDIT
    public void EditGlobalConfig(string editedUserMail, string username, string email, string password)
    {
        var gc = GetGlobalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        EditUser(gc, editedUserMail, new User() { Username = username, Mail = email, PasswordHash = _hashService.ComputeSha256(password) }, _globalConfigPath);
    }
    
    public void EditLocalConfig(string editedUserMail, string username, string email, string password)
    {
        var gc = GetLocalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        EditUser(gc, editedUserMail, new User() { Username = username, Mail = email, PasswordHash = _hashService.ComputeSha256(password) }, _localConfigPath);
    }
    private bool EditUser(Config gc, string editedUserMail, User newUser, string filePath)
    {
        var user = gc.Configs.FirstOrDefault(c => c.Mail == editedUserMail);
        if (user == null)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"User not found: {editedUserMail}");
            return false;
        }
        // ask user's password for security
        var validatedPassword = _hashService.ComputeSha256(ConsoleHelper.ReadConfirmedPassword(PasswordValidator.Validate));
        if (validatedPassword.Equals(user.PasswordHash))
        {
            user.Mail = newUser.Mail.Equals(string.Empty) ? user.Mail : newUser.Mail;
            user.Username = newUser.Username.Equals(string.Empty) ? user.Username : newUser.Username;
            user.PasswordHash = newUser.PasswordHash.Equals(string.Empty) ? user.PasswordHash : _hashService.ComputeSha256(newUser.PasswordHash);
            
            SaveConfig(gc, filePath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Config edited successfully");
            return true;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Invalid password Try again");
        return false;
    }
    #endregion

    #region REMOVE

    private void RemoveConfig(Config gc, string email, string filePath)
    {
        var user = gc.Configs.FirstOrDefault(c => c.Mail == email);

        if (user == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR] user with {email} email not found");
            Console.ResetColor();
            return;
        }

        if (gc.ActiveUser == user.Mail)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"This user is active. Do you want to delete it? (Y/N):");
            Console.ResetColor();
            var isDelete = Console.ReadLine();
            if (isDelete != null && isDelete.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Which user do you want to make active (email)");
                Console.ResetColor();
                ShowGlobalConfigs();

                var newActiveUser = Console.ReadLine();
                if (newActiveUser != null)
                {
                    SetActiveUser(gc, newActiveUser);
                }
            }
        }
        gc.Configs.Remove(user);
        SaveConfig(gc, filePath);
        Console.WriteLine("Config removed successfully");
    }

    public void RemoveGlobalConfig(string email)
    {
        var gc = GetGlobalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        RemoveConfig(gc, email, _globalConfigPath);
    }

    public void RemoveLocalConfig(string email)
    {
        var gc = GetLocalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        RemoveConfig(gc, email, _localConfigPath);
    }
    #endregion

    #region HELPERS
    private Config? GetGlobalConfig()
    {
        return GetConfig(_globalConfigPath);
    }

    private Config? GetLocalConfig()
    {
        return GetConfig(_localConfigPath);
    }
    private Config? GetConfig(string path)
    {
        var file = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(file, _jsonOptions);
    }
    private void SaveConfig(Config config, string path)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(path, json);
    }
    private static void SetActiveUser(Config gc, string email)
    {
        if (gc.Configs.Exists(c => c.Mail == email))
        {
            gc.ActiveUser = email;
        }
    }
    #endregion
}