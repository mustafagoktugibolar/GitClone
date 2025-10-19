using System.Text.Json;
using GitClone.Helpers;
using GitClone.Interfaces;
using GitClone.Models;

namespace GitClone.Services;

public class ConfigService(IHashService hashService, IRepositoryContext repositoryContext, IFileSystem fileSystem) : IConfigService
{
    private readonly string GlobalConfigPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ilos"), "config.json");
    private string LocalConfigPath = repositoryContext.LocalConfigPath;
    private readonly JsonSerializerOptions? jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    #region FILE_CREATION
    public async Task InitLocalConfig()
    {
        try
        {
            var jsonFile = File.ReadAllText(GlobalConfigPath);
            if (jsonFile.Length <= 0) 
                return;
            
            var configs = JsonSerializer.Deserialize<Config>(jsonFile);
            var activeConfig = configs?.Configs.FirstOrDefault(c => c.Mail == configs.ActiveUser);
            var repoPath = repositoryContext.IlosPath;
            LocalConfigPath = repositoryContext.LocalConfigPath;
            if (activeConfig == null) 
                return;
            
            var newConfigs = new List<User>() { activeConfig };
            var localConfig = new Config() { Configs = newConfigs, ActiveUser = activeConfig.Mail };
            await SaveConfig(localConfig, LocalConfigPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            throw;
        }
    }
    
    public async Task EnsureCreated()
    {
        var repoPath = repositoryContext.IlosPath;
        LocalConfigPath = Path.Combine(repoPath, "config.json");
        var globalConfigDir = Path.GetDirectoryName(GlobalConfigPath)!;
        if (!Directory.Exists(globalConfigDir))
        {
            Directory.CreateDirectory(globalConfigDir);
        }

        if (!File.Exists(GlobalConfigPath))
        {
            await CreateGlobalConfigFile();
        }

        var localConfigDir = Path.GetDirectoryName(LocalConfigPath);
        if (localConfigDir != null && !Directory.Exists(localConfigDir))
        {
            Directory.CreateDirectory(localConfigDir);
        }

        if (!File.Exists(LocalConfigPath))
        {
            await InitLocalConfig();
        }
    }
    
    private async Task CreateGlobalConfigFile()
    {
        try
        {
            var user = new User() { Username = Environment.UserName, Mail = $"{Environment.UserName}@localhost"};
            var gc = new Config() { Configs = [user], ActiveUser = user.Mail };
            await SaveConfig(gc, GlobalConfigPath);
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

    public async Task ShowLocalConfigs()
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

    public async Task ShowGlobalConfigs()
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

    private async Task AddConfig(Config config, string username, string email, string password, string filePath)
    {
        // check is user exists (PK is email)
        if (!config.Configs.Exists(c => c.Mail == email))
        {
            var user = new User() { Username = username, Mail = email, PasswordHash = hashService.ComputeSha256(password) };
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

            await SaveConfig(config, filePath);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Users null or user exists!");
        }
    }
    public async Task AddGlobalConfig(string username, string email, string password)
    {
        var config = GetGlobalConfig();
        if (config == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        await AddConfig(config, username, email, password, GlobalConfigPath);
    }
    public async Task AddLocalConfig(string username, string email, string password)
    {
        var config = GetLocalConfig();
        if (config == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        await AddConfig(config, username, email, password, LocalConfigPath);
    }
    #endregion

    #region EDIT
    public async Task EditGlobalConfig(string editedUserMail, string username, string email, string password)
    {
        var gc = GetGlobalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        await EditUser(gc, editedUserMail, new User() { Username = username, Mail = email, PasswordHash = hashService.ComputeSha256(password) }, GlobalConfigPath);
    }
    
    public async Task EditLocalConfig(string editedUserMail, string username, string email, string password)
    {
        var gc = GetLocalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        await EditUser(gc, editedUserMail, new User() { Username = username, Mail = email, PasswordHash = hashService.ComputeSha256(password) }, LocalConfigPath);
    }
    private async Task<bool> EditUser(Config gc, string editedUserMail, User newUser, string filePath)
    {
        var user = gc.Configs.FirstOrDefault(c => c.Mail == editedUserMail);
        if (user == null)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"User not found: {editedUserMail}");
            return false;
        }
        // ask user's password for security
        var validatedPassword = hashService.ComputeSha256(ConsoleHelper.ReadConfirmedPassword(PasswordValidator.Validate));
        if (validatedPassword.Equals(user.PasswordHash))
        {
            user.Mail = newUser.Mail.Equals(string.Empty) ? user.Mail : newUser.Mail;
            user.Username = newUser.Username.Equals(string.Empty) ? user.Username : newUser.Username;
            user.PasswordHash = newUser.PasswordHash.Equals(string.Empty) ? user.PasswordHash : hashService.ComputeSha256(newUser.PasswordHash);
            
            await SaveConfig(gc, filePath);
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

    private async Task RemoveConfig(Config gc, string email, string filePath)
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
                await ShowGlobalConfigs();

                var newActiveUser = Console.ReadLine();
                if (newActiveUser != null)
                {
                    SetActiveUser(gc, newActiveUser);
                }
            }
        }
        gc.Configs.Remove(user);
        await SaveConfig(gc, filePath);
        Console.WriteLine("Config removed successfully");
    }

    public async Task RemoveGlobalConfig(string email)
    {
        var gc = GetGlobalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        await RemoveConfig(gc, email, GlobalConfigPath);
    }

    public async Task RemoveLocalConfig(string email)
    {
        var gc = GetLocalConfig();
        if (gc == null)
        {
            Console.Error.WriteLine($"[ERROR] couldn't find config");
            return;
        }
        await RemoveConfig(gc, email, LocalConfigPath);
    }
    #endregion

    #region HELPERS
    private Config? GetGlobalConfig()
    {
        return GetConfig(GlobalConfigPath);
    }

    private Config? GetLocalConfig()
    {
        return GetConfig(LocalConfigPath);
    }
    private Config? GetConfig(string path)
    {
        var file = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(file, jsonOptions);
    }
    private async Task SaveConfig(Config config, string path)
    {
        var json = JsonSerializer.Serialize(config, jsonOptions);
        await fileSystem.WriteAtomic(path, json);
    }
    private void SetActiveUser(Config gc, string email)
    {
        if (gc.Configs.Exists(c => c.Mail == email))
        {
            gc.ActiveUser = email;
        }
    }
    #endregion
}