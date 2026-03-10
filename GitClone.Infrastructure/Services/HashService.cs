using System.Security.Cryptography;
using System.Text;
using GitClone.Core.Interfaces;

namespace GitClone.Infrastructure.Services;

public class HashService : IHashService
{
    public string ComputeSha1(string content)
    {
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(hashBytes);
    }

    public string ComputeSha256(string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(hashBytes);
    }
}
