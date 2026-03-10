namespace GitClone.Core.Models;

public class Config
{
    public List<User> Configs { get; set; } = new();
    public string ActiveUser { get; set; } = string.Empty;
}