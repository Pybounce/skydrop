
using System.Net;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SkyDrop.Api.Data;
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests.Services;


public class CosmosUsageDbTests
{
    private const string FileId = "myFileId";
    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosUsageDb _db;
    public enum Counter { Download, Upload }
    private static string Field(Counter counter) => counter == Counter.Download ? "downloadedBytes" : "uploadedBytes";
    
    public CosmosUsageDbTests()
    {
        _container.PatchItemAsync<MonthlyUsage>(FileId, Arg.Any<PartitionKey>(), Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<ItemResponse<MonthlyUsage>>());
        _db = new CosmosUsageDb(_container);
    }

    private Task<bool> IncrementWithinLimit(Counter counter, long increment, long limit, CancellationToken ct)
    {
        return counter switch
        {
            Counter.Download => _db.TryIncrementDownloadWithinLimitAsync(FileId, increment, limit, ct),
            Counter.Upload => _db.TryIncrementUploadWithinLimitAsync(FileId, increment, limit, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(counter))
        };
    }

    [Theory]
    [InlineData(Counter.Download)]
    [InlineData(Counter.Upload)]
    public async Task WithinLimit_ReturnsTrue(Counter counter)
    {
        var result = await IncrementWithinLimit(counter, 5, 100, CancellationToken.None);

        Assert.True(result);
    }

    [Theory]
    [InlineData(Counter.Download)]
    [InlineData(Counter.Upload)]
    public async Task OverLimit_ReturnsFalse(Counter counter)
    {
        _container.PatchItemAsync<MonthlyUsage>(FileId, Arg.Any<PartitionKey>(), Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Cosmos error", HttpStatusCode.PreconditionFailed, 0, "", 0));

        var result = await IncrementWithinLimit(counter, 5, 100, CancellationToken.None);

        Assert.False(result);
    }

    [Theory]
    [InlineData(Counter.Download)]
    [InlineData(Counter.Upload)]
    public async Task OverLimit_DoesNotCreateDocument(Counter counter)
    {
        _container.PatchItemAsync<MonthlyUsage>(FileId, Arg.Any<PartitionKey>(), Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Cosmos error", HttpStatusCode.PreconditionFailed, 0, "", 0));

        var result = await IncrementWithinLimit(counter, 5, 100, CancellationToken.None);

        await _container.DidNotReceiveWithAnyArgs()
            .CreateItemAsync<MonthlyUsage>(default!, default, default, default);
    }

    [Theory]
    [InlineData(Counter.Download)]
    [InlineData(Counter.Upload)]
    public async Task DocumentMissing_CreatesItWithZeroUsageThenRetries(Counter counter)
    {
        _container.PatchItemAsync<MonthlyUsage>(FileId, Arg.Any<PartitionKey>(), Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(
            _ => Task.FromException<ItemResponse<MonthlyUsage>>(new CosmosException("Cosmos error", HttpStatusCode.NotFound, 0, "", 0)),
            _ => Task.FromResult(Substitute.For<ItemResponse<MonthlyUsage>>())
        );

        var result = await IncrementWithinLimit(counter, 5, 100, CancellationToken.None);

        Assert.True(result);
        await _container.Received(1).CreateItemAsync(Arg.Is<MonthlyUsage>(u => u.Id == FileId && u.DownloadedBytes == 0 && u.UploadedBytes == 0), new PartitionKey(FileId), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>());
        await _container.Received(2).PatchItemAsync<MonthlyUsage>(FileId, Arg.Any<PartitionKey>(), Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(Counter.Download)]
    [InlineData(Counter.Upload)]
    public async Task DocumentStillMissingAfterCreate_Throws(Counter counter)
    {
        _container.PatchItemAsync<MonthlyUsage>(FileId, Arg.Any<PartitionKey>(), Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CosmosException("Cosmos error", HttpStatusCode.NotFound, 0, "", 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() => IncrementWithinLimit(counter, 5, 100, CancellationToken.None));
    }
}