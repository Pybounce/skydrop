
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests.Services;

public class BlobFileDbTests
{
    private const string FileId = "lkjsdflksndjfkl";
    private readonly BlobContainerClient _container = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blob = Substitute.For<BlobClient>();
    private readonly BlobFileDb _db;

    public BlobFileDbTests()
    {
        _container.GetBlobClient(FileId).Returns(_blob);
        _db = new BlobFileDb(_container);
    }

    [Fact]
    public async Task UploadAsync_UsesFileIdAsBlobName()
    {
        await _db.UploadAsync(FileId, Stream.Null, CancellationToken.None);

        _container.Received(1).GetBlobClient(FileId);
    }

    [Fact]
    public async Task UploadAsync_DoesNotOverwriteExistingBlob()
    {
        BlobUploadOptions? ops = null;
        await _blob.UploadAsync(Arg.Any<Stream>(), Arg.Do<BlobUploadOptions>(o => ops = o), Arg.Any<CancellationToken>());

        await _db.UploadAsync(FileId, Stream.Null, CancellationToken.None);

        Assert.Equal(ETag.All, ops?.Conditions?.IfNoneMatch);
    }

    [Fact]
    public async Task UploadAsync_PassesStream()
    {
        using var input = new MemoryStream(new byte[100]);

        await _db.UploadAsync(FileId, input, CancellationToken.None);

        await _blob.Received(1).UploadAsync(input, Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        using var input = new MemoryStream(new byte[100]);

        await _db.UploadAsync(FileId, input, cts.Token);

        await _blob.Received(1).UploadAsync(Arg.Any<MemoryStream>(), Arg.Any<BlobUploadOptions>(), cts.Token);
    }

    [Fact]
    public async Task DownloadAsync_BlobExists_ReturnsStream()
    {
        var stream = new MemoryStream(new byte[100]);
        _blob.OpenReadAsync(Arg.Any<BlobOpenReadOptions>(), Arg.Any<CancellationToken>()).Returns(stream);

        var result = await _db.DownloadAsync(FileId, CancellationToken.None);

        Assert.Same(stream, result);
        _container.Received(1).GetBlobClient(FileId);
    }

    [Fact]
    public async Task DownloadAsync_BlobMissing_ReturnsNull()
    {
        _blob.OpenReadAsync(Arg.Any<BlobOpenReadOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(new RequestFailedException(404, "The specified blob does not exist."));

        var result = await _db.DownloadAsync(FileId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _db.DownloadAsync(FileId, cts.Token);

        await _blob.Received(1).OpenReadAsync(Arg.Any<BlobOpenReadOptions>(), cts.Token);
    }
}