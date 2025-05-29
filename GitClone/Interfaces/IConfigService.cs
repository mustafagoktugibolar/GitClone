namespace GitClone.Interfaces;

public interface IConfigService
{
    void InitLocalConfig();
    void CreateConfig();
    void DeleteConfig();
    void ShowConfigs();
    void EnsureCreated();
}