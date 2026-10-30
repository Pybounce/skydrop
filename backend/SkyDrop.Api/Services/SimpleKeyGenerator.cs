
using System.Buffers.Text;
using System.Security.Cryptography;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class SimpleKeyGenerator : IKeyGenerator
{
    public string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Base64Url.EncodeToString(bytes);
    }
}