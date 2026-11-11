using NSubstitute;
using SkyDrop.Api.Data;
using SkyDrop.Api.ServiceInterfaces;
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests.Services;

public class FileDownloaderTests
{
    private const string FileId = "myFileId";
    private const string FileName = "myFileName";
    private const long FileSize = 5000;
    private const int FileDownloadCount = 0;

    private static readonly FileMetadata Metadata = new FileMetadata
    {
            Id = FileId,
            Name = FileName,
            Size = FileSize,
            DownloadCount = FileDownloadCount
    };

    private readonly IFileDb _fileDb = Substitute.For<IFileDb>();
    private readonly IUsageLimiter _usageLimiter = Substitute.For<IUsageLimiter>();
    private readonly IFileMetadataDb _metadataDb = Substitute.For<IFileMetadataDb>();
    private readonly FileDownloader _downloader;


    public FileDownloaderTests() {
        _fileDb.DownloadAsync(FileId, Arg.Any<CancellationToken>()).Returns(new MemoryStream(new byte[FileSize]));
        _usageLimiter.TryReserveDownloadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _metadataDb.IncrementDownloadsAsync(FileId, Arg.Any<CancellationToken>()).Returns(Metadata);
        
        _downloader = new FileDownloader(_fileDb, _usageLimiter, _metadataDb);
    }

    [Fact]
    public async Task Download_FileExists_ReturnSuccess()
    {
        var result = await _downloader.Download(FileId, CancellationToken.None);

        var success = Assert.IsType<DownloadResult.Success>(result);
        Assert.NotNull(success);
        Assert.Equal(Metadata, success.Metadata);
        Assert.Equal(FileSize, success.Content.Length);
    }

    [Fact]
    public async Task Download_FileExists_ReservesUsage()
    {
        await _downloader.Download(FileId, CancellationToken.None);

        await _usageLimiter.Received(1).TryReserveDownloadAsync(FileSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_FileExists_IncrementsDownloadCounter()
    {
        await _downloader.Download(FileId, CancellationToken.None);

        await _metadataDb.Received(1).IncrementDownloadsAsync(FileId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_FileExists_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await _downloader.Download(FileId, cts.Token);

        await _usageLimiter.Received().TryReserveDownloadAsync(Arg.Any<long>(), cts.Token);
        await _metadataDb.Received().IncrementDownloadsAsync(Arg.Any<string>(), cts.Token);
        await _fileDb.Received().DownloadAsync(Arg.Any<string>(), cts.Token);
    }

    [Fact]
    public async Task Download_NoMetadata_ReturnsNotFound()
    {
        _metadataDb.IncrementDownloadsAsync(FileId, Arg.Any<CancellationToken>()).Returns((FileMetadata?)null);

        var result = await _downloader.Download(FileId, CancellationToken.None);

        Assert.IsType<DownloadResult.DoesNotExist>(result);
    }

    [Fact]
    public async Task Download_NoMetadata_DoesNotReserveUsage()
    {
        _metadataDb.IncrementDownloadsAsync(FileId, Arg.Any<CancellationToken>()).Returns((FileMetadata?)null);

        await _downloader.Download(FileId, CancellationToken.None);

        await _usageLimiter.DidNotReceive().TryReserveDownloadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_NoMetadata_DoesNotDownloadFile()
    {
        _metadataDb.IncrementDownloadsAsync(FileId, Arg.Any<CancellationToken>()).Returns((FileMetadata?)null);

        await _downloader.Download(FileId, CancellationToken.None);

        await _fileDb.DidNotReceive().DownloadAsync(FileId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_MetadataButNoFile_ReturnsNotFound()
    {
        _fileDb.DownloadAsync(FileId, Arg.Any<CancellationToken>()).Returns((Stream?)null);

        var result = await _downloader.Download(FileId, CancellationToken.None);

        Assert.IsType<DownloadResult.DoesNotExist>(result);
    }

    [Fact]
    public async Task Download_UsageLimitReached_ReturnsUsageLimitReached()
    {
        _usageLimiter.TryReserveDownloadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _downloader.Download(FileId, CancellationToken.None);

        Assert.IsType<DownloadResult.UsageLimitReached>(result);
    }

    [Fact]
    public async Task Download_UsageLimitReached_DoesNotDownloadFile()
    {
        _usageLimiter.TryReserveDownloadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _downloader.Download(FileId, CancellationToken.None);

        await _fileDb.DidNotReceive().DownloadAsync(FileId, Arg.Any<CancellationToken>());
    }
}
