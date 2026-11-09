
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests.Services;

public class SimpleKeyGeneratorTests
{
    private readonly SimpleKeyGenerator _generator = new();

    [Fact]
    public void GenerateKey_KeyIs22Characters()
    {
        var key = _generator.GenerateKey();

        Assert.Equal(22, key.Length);
    }

    [Fact]
    public void GenerateKey_OnlyContainsUrlSafeCharacters()
    {
        for (var i = 0; i < 1_000; i++)
        {
            var key = _generator.GenerateKey();

            Assert.Matches("^[A-Za-z0-9_-]{22}", key);
        }
    }

    [Fact] 
    public void GenerateKey_UniqueKeys()
    {
        var count = 100_000;
        var keys = new HashSet<string>();

        for (var i = 0; i < count; i++)
        {
            keys.Add(_generator.GenerateKey());
        }

        Assert.Equal(count, keys.Count);
    }
}