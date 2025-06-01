namespace GitClone.Interfaces;

public interface IConfigService
{
    void InitLocalConfig();
    void CreateConfig();
    void DeleteConfig();
    void ShowLocalConfigs();
    void ShowGlobalConfigs();
    void EnsureCreated();
    void AddGlobalConfig(string username, string email, string password);
    void RemoveGlobalConfig();
    void EditGlobalConfig(string editedUserMail, string username, string email, string password);
    
}