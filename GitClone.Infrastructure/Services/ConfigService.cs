using System.Text.Json;
using GitClone.Core.Abstractions;
using GitClone.Core.Helpers;
using GitClone.Core.Interfaces;
using GitClone.Core.Models;

namespace GitClone.Infrastructure.Services;

public class ConfigService(
    IHashService hashService,
    IRepositoryContext repositoryContext,
    IFileSystem fileSystem,
    IConsole console,
    IPasswordPrompter passwordPrompter) : IConfigService
{
    private readonly string globalConfigPath = Path.Combine(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ilos"),
        "config.json");

    private string localConfigPath = repositoryContext.LocalConfigPath;
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    #region FILE_CREATION
    public async Task InitLocalConfig()
    {
        try
        {
            var jsonFile = File.ReadAllText(globalConfigPath);
            if (jsonFile.Length <= 0) 
                return;
            
            var configs = JsonSerializer.Deserialize<Config>(jsonFile);
            var activeConfig = configs?.Configs.FirstOrDefault(c => c.Mail == configs.ActiveUser);
            localConfigPath = repositoryContext.LocalConfigPath;
            if (activeConfig == null) 
                return;
            
            var localConfig = new Config { Configs = [activeConfig], ActiveUser = activeConfig.Mail };
            await SaveConfig(localConfig, localConfigPath);
        }
        catch (Exception ex)
        {
            console.WriteErrorLine($"[ERROR] {ex.Message}");
            throw;
        }
    }
    
    public async Task EnsureCreated()
    {
        localConfigPath = Path.Combine(repositoryContext.IlosPath, "config.json");

        var globalConfigDir = Path.GetDirectoryName(globalConfigPath)!;
        if (!Directory.Exists(globalConfigDir))
        {
            Directory.CreateDirectory(globalConfigDir);
        }

        if (!File.Exists(globalConfigPath))
        {
            await CreateGlobalConfigFile();
        }

        var localConfigDir = Path.GetDirectoryName(localConfigPath);
        if (localConfigDir != null && !Directory.Exists(localConfigDir))
        {
            Directory.CreateDirectory(localConfigDir);
        }

        if (!File.Exists(localConfigPath))
        {
            await InitLocalConfig();
        }
    }
    
    private async Task CreateGlobalConfigFile()
    {
        try
        {
            var user = new User { Username = Environment.UserName, Mail = $"{Environment.UserName}@localhost" };
            var config = new Config { Configs = [user], ActiveUser = user.Mail };
            await SaveConfig(config, globalConfigPath);
        }
        catch (Exception e)
        {
            console.WriteLine("Error creating global config");
            console.WriteLine(e.Message);
            console.WriteErrorLine(string.Empty);
            throw;
        }
    }
    #endregion
    
    #region SHOW

    public Task ShowLocalConfigs()
    {   
        var config = GetLocalConfig();
        if (config == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return Task.CompletedTask;
        }

        PrintConfigs("Local Configs:", config);
        return Task.CompletedTask;
    }

    public Task ShowGlobalConfigs()
    {   
        var config = GetGlobalConfig();
        if (config == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return Task.CompletedTask;
        }

        PrintConfigs("Global Configs:", config);
        return Task.CompletedTask;
    }
    #endregion
    
    #region ADD

    private async Task AddConfig(Config config, string username, string email, string password, string filePath)
    {
        if (!config.Configs.Exists(c => c.Mail == email))
        {
            var user = new User { Username = username, Mail = email, PasswordHash = hashService.ComputeSha256(password) };
            config.Configs.Add(user);
            
            if (config.Configs.Any(c =>
                    c.Username == Environment.UserName && c.Mail == $"{Environment.UserName}@localhost"))
            {
                config.Configs.RemoveAll(c =>
                    c.Username == Environment.UserName &&
                    c.Mail == $"{Environment.UserName}@localhost");
            }
            console.SetForegroundColor(ConsoleColor.Yellow);
            console.WriteLine("Config created successfully");
            console.ResetColor();
            
            console.Write("Do you want to make the user active? (Y/N):");
            var isActive = console.ReadLine();
            
            if (isActive?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true)
            {
                config.ActiveUser = user.Mail;
            }

            await SaveConfig(config, filePath);
        }
        else
        {
            console.SetForegroundColor(ConsoleColor.DarkYellow);
            console.WriteLine("User already exists.");
            console.ResetColor();
        }
    }
    public async Task AddGlobalConfig(string username, string email, string password)
    {
        var config = GetGlobalConfig();
        if (config == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return;
        }
        await AddConfig(config, username, email, password, globalConfigPath);
    }
    public async Task AddLocalConfig(string username, string email, string password)
    {
        var config = GetLocalConfig();
        if (config == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return;
        }
        await AddConfig(config, username, email, password, localConfigPath);
    }
    #endregion

    #region EDIT
    public async Task EditGlobalConfig(string editedUserMail, string username, string email, string password)
    {
        var config = GetGlobalConfig();
        if (config == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return;
        }

        await EditUser(config, editedUserMail, username, email, password, globalConfigPath);
    }
    
    public async Task EditLocalConfig(string editedUserMail, string username, string email, string password)
    {
        var config = GetLocalConfig();
        if (config == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return;
        }

        await EditUser(config, editedUserMail, username, email, password, localConfigPath);
    }

    private async Task<bool> EditUser(
        Config config,
        string editedUserMail,
        string newUsername,
        string newEmail,
        string newPassword,
        string filePath)
    {
        var user = config.Configs.FirstOrDefault(c => c.Mail == editedUserMail);
        if (user == null)
        {
            console.SetForegroundColor(ConsoleColor.DarkYellow);
            console.WriteLine($"User not found: {editedUserMail}");
            console.ResetColor();
            return false;
        }

        var validatedPassword = hashService.ComputeSha256(passwordPrompter.ReadConfirmedPassword(PasswordValidator.Validate));
        if (validatedPassword.Equals(user.PasswordHash))
        {
            if (!string.IsNullOrWhiteSpace(newEmail))
            {
                user.Mail = newEmail;
            }

            if (!string.IsNullOrWhiteSpace(newUsername))
            {
                user.Username = newUsername;
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = hashService.ComputeSha256(newPassword);
            }
            
            await SaveConfig(config, filePath);
            console.SetForegroundColor(ConsoleColor.Green);
            console.WriteLine("Config edited successfully");
            console.ResetColor();
            return true;
        }

        console.SetForegroundColor(ConsoleColor.DarkYellow);
        console.WriteLine("Invalid password. Try again.");
        console.ResetColor();
        return false;
    }
    #endregion

    #region REMOVE

    private async Task RemoveConfig(Config gc, string email, string filePath)
    {
        var user = gc.Configs.FirstOrDefault(c => c.Mail == email);

        if (user == null)
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteErrorLine($"[ERROR] user with {email} email not found");
            console.ResetColor();
            return;
        }

        if (gc.ActiveUser == user.Mail)
        {
            console.SetForegroundColor(ConsoleColor.DarkYellow);
            console.Write("This user is active. Do you want to delete it? (Y/N):");
            console.ResetColor();
            var isDelete = console.ReadLine();
            if (isDelete != null && isDelete.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                console.WriteLine("Which user do you want to make active (email)");
                PrintConfigs("Available Configs:", gc);

                var newActiveUser = console.ReadLine();
                if (newActiveUser != null)
                {
                    SetActiveUser(gc, newActiveUser);
                }
            }
        }
        gc.Configs.Remove(user);
        await SaveConfig(gc, filePath);
        console.WriteLine("Config removed successfully");
    }

    public async Task RemoveGlobalConfig(string email)
    {
        var gc = GetGlobalConfig();
        if (gc == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return;
        }
        await RemoveConfig(gc, email, globalConfigPath);
    }

    public async Task RemoveLocalConfig(string email)
    {
        var gc = GetLocalConfig();
        if (gc == null)
        {
            console.WriteErrorLine("[ERROR] couldn't find config");
            return;
        }
        await RemoveConfig(gc, email, localConfigPath);
    }
    #endregion

    #region HELPERS
    private Config? GetGlobalConfig()
    {
        return GetConfig(globalConfigPath);
    }

    private Config? GetLocalConfig()
    {
        return GetConfig(localConfigPath);
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

    private void PrintConfigs(string title, Config config)
    {
        console.WriteLine(title);
        if (config.Configs.Count == 0)
        {
            console.WriteErrorLine("[ERROR] couldn't find any configs");
            return;
        }

        foreach (var user in config.Configs)
        {
            console.WriteLine($"  [{user.Username} {user.Mail}]");
        }
    }
    #endregion
}
