namespace GitClone.Core.Interfaces;

public interface IConfigService
{
    Task InitLocalConfig();
    Task AddLocalConfig(string username, string email, string password);
    Task RemoveLocalConfig(string email);
    Task ShowLocalConfigs();
    Task EditLocalConfig(string editedUserMail, string username, string email, string password);
    Task ShowGlobalConfigs();
    Task EnsureCreated();
    Task AddGlobalConfig(string username, string email, string password);
    Task RemoveGlobalConfig(string email);
    Task EditGlobalConfig(string editedUserMail, string username, string email, string password);
    
}