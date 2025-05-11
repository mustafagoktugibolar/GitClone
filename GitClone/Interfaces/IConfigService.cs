namespace GitClone.Interfaces;

public interface IConfigService
{
    void InitConfig();
    void CreateConfig();
    void DeleteConfig();
    void ShowConfig();
    void EnsureCreated();
}