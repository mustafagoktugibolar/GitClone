namespace GitClone.Interfaces;

public interface IConfigService
{
    void InitLocalConfig();
    void CreateConfig();
    void DeleteConfig();
    void ShowLocalConfigs();
    void ShowGlobalConfigs();
    void EnsureCreated();
}