using System.Security.Cryptography;
using System.Text;
using GitClone.Interfaces;

namespace GitClone.Services;

public class HashService : IHashService
{
    public string ComputeSha1(string content)
    {
        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public string ComputeSha256(string content)
    {
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
