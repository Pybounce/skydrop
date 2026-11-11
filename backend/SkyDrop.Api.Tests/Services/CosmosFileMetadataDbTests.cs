

using System.Net;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SkyDrop.Api.Data;
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests.Services;

public class CosmosFileMetadataDbTests
{

    private const string FileId = "myFileId";
    private const string FileName = "myFileName";
    private const long FileSize = 5000;
    private const int FileDownloadCount = 0;
    private static readonly MemoryStream FileContent = new MemoryStream(new byte[FileSize]); 
    private static readonly FileMetadata Metadata = new FileMetadata
    {
            Id = FileId,
            Name = FileName,
            Size = FileSize,
            DownloadCount = FileDownloadCount
    };    
    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosFileMetadataDb _db;

    public CosmosFileMetadataDbTests()
    {
        _db = new CosmosFileMetadataDb(_container);
    }

    [Fact]
    public async Task CreateAsync_SendsDocumentToContainer()
    {
        await _db.CreateAsync(Metadata, CancellationToken.None);

        await _container.Received(1).CreateItemAsync(Metadata, Arg.Any<PartitionKey?>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _db.CreateAsync(Metadata, cts.Token);

        await _container.Received(1).CreateItemAsync(Arg.Any<FileMetadata>(), Arg.Any<PartitionKey?>(), Arg.Any<ItemRequestOptions>(), cts.Token);
    }

    [Fact]
    public async Task ReadAsync_DocumentMissing_ReturnsNull()
    {
        _container.ReadItemAsync<FileMetadata>(FileId, Arg.Any<PartitionKey>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(new CosmosException("Cosmos error", HttpStatusCode.NotFound, 0, "", 0));

        var result = await _db.ReadAsync(FileId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAsync_OtherCosmosError_IsNotSwallowed()
    {
        _container.ReadItemAsync<FileMetadata>(FileId, Arg.Any<PartitionKey>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(new CosmosException("Cosmos error", HttpStatusCode.TooManyRequests, 0, "", 0));

        var ex = await Assert.ThrowsAsync<CosmosException>(() => _db.ReadAsync(FileId, CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
    }

    [Fact]
    public async Task ReadAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        
        var response = Substitute.For<ItemResponse<FileMetadata>>();
        response.Resource.Returns(Metadata);
        _container.ReadItemAsync<FileMetadata>(FileId, Arg.Any<PartitionKey>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>()).Returns(response);

        await _db.ReadAsync(FileId, cts.Token);

        await _container.Received(1).ReadItemAsync<FileMetadata>(FileId, new PartitionKey(FileId), Arg.Any<ItemRequestOptions>(), cts.Token);
    }

    [Fact]
    public async Task IncrementDownloadsAsync_SendsSingleIncrementOnDownloadCount()
    {
        var response = Substitute.For<ItemResponse<FileMetadata>>();
        response.Resource.Returns(Metadata);

        IReadOnlyList<PatchOperation>? sentOps = null;
        _container.PatchItemAsync<FileMetadata>(FileId, new PartitionKey(FileId),
                Arg.Do<IReadOnlyList<PatchOperation>>(ops => sentOps = ops),
                Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _db.IncrementDownloadsAsync(FileId, CancellationToken.None);

        var op = Assert.Single(sentOps!);
        Assert.Equal(PatchOperationType.Increment, op.OperationType);
        Assert.Equal("/downloadCount", op.Path);
    }

   [Fact]
    public async Task IncrementDownloadsAsync_ReturnsTheUpdatedDocument()
    {
        var response = Substitute.For<ItemResponse<FileMetadata>>();
        response.Resource.Returns(Metadata);

        _container.PatchItemAsync<FileMetadata>(FileId, new PartitionKey(FileId),
                Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _db.IncrementDownloadsAsync(FileId, CancellationToken.None);

        Assert.Same(Metadata, result);
    }

    [Fact]
    public async Task IncrementDownloadsAsync_DocumentMissing_ReturnsNull()
    {
        _container.PatchItemAsync<FileMetadata>(FileId, Arg.Any<PartitionKey>(),
                Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(new CosmosException("Cosmos error", HttpStatusCode.NotFound, 0, "", 0));

        var result = await _db.IncrementDownloadsAsync(FileId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task IncrementDownloadsAsync_PassesCancellationToken()
    {
        var response = Substitute.For<ItemResponse<FileMetadata>>();
        response.Resource.Returns(Metadata);

        using var cts = new CancellationTokenSource();
        _container.PatchItemAsync<FileMetadata>(FileId, Arg.Any<PartitionKey>(),
                Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _db.IncrementDownloadsAsync(FileId, cts.Token);

        await _container.Received(1).PatchItemAsync<FileMetadata>(FileId, new PartitionKey(FileId),
            Arg.Any<IReadOnlyList<PatchOperation>>(), Arg.Any<PatchItemRequestOptions>(), cts.Token);
    }

}