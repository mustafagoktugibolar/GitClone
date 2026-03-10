using System.ComponentModel.DataAnnotations.Schema;

namespace GitClone.Core.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string CreatedAt { get; } = DateTime.UtcNow.ToString("o");
}