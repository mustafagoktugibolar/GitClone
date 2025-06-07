namespace GitClone.Interfaces;

public interface IConfigService
{
    void InitLocalConfig();
    void AddLocalConfig(string username, string email, string password);
    void RemoveLocalConfig(string email);
    void ShowLocalConfigs();
    void EditLocalConfig(string editedUserMail, string username, string email, string password);
    void ShowGlobalConfigs();
    void EnsureCreated();
    void AddGlobalConfig(string username, string email, string password);
    void RemoveGlobalConfig(string email);
    void EditGlobalConfig(string editedUserMail, string username, string email, string password);
    
}