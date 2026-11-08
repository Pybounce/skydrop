using NSubstitute;
using SkyDrop.Api.DTOs;
using SkyDrop.Api.ServiceInterfaces;
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests;

public class FileUploaderTests
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

    private readonly IFileDb _fileDb = Substitute.For<IFileDb>();
    private readonly IUsageLimiter _usageLimiter = Substitute.For<IUsageLimiter>();
    private readonly IFileMetadataDb _metadataDb = Substitute.For<IFileMetadataDb>();
    private readonly IKeyGenerator _keyGenerator = Substitute.For<IKeyGenerator>();
    private readonly FileUploader _uploader;

    public FileUploaderTests() {
        _fileDb.UploadAsync(FileId, FileContent, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _usageLimiter.TryReserveUploadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _metadataDb.CreateAsync(Metadata, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _keyGenerator.GenerateKey().Returns(FileId);

        _uploader = new FileUploader(_fileDb, _usageLimiter, _keyGenerator, _metadataDb);
    }

    [Fact]
    public async Task Upload_ValidUpload_ReturnsUploadSuccess()
    {
        var result = await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);

        var success = Assert.IsType<UploadResult.Success>(result);
        Assert.NotNull(success);
        Assert.Equal(FileId, success.FileId);
    }

    [Fact]
    public async Task Upload_ValidUpload_ReservesUpload()
    {
        await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);

        await _usageLimiter.Received(1).TryReserveUploadAsync(FileSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_ValidUpload_CreatesMetadata()
    {
        await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);
    
        await _metadataDb.Received(1).CreateAsync(Metadata, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_ValidUpload_WritesContent()
    {
        await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);

        await _fileDb.Received(1).UploadAsync(FileId, FileContent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_ValidUpload_PassesCancelletionToken()
    {
        using var cts = new CancellationTokenSource();
        
        await _uploader.Upload(FileContent, FileName, FileSize, cts.Token);

        await _fileDb.Received().UploadAsync(Arg.Any<string>(), Arg.Any<MemoryStream>(), cts.Token);
        await _metadataDb.Received().CreateAsync(Arg.Any<FileMetadata>(), cts.Token);
        await _usageLimiter.Received().TryReserveUploadAsync(Arg.Any<long>(), cts.Token);
    }

    [Fact]
    public async Task Upload_UsageLimitReached_ReturnsUsageLimitReached()
    {
        _usageLimiter.TryReserveUploadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);

        var failure = Assert.IsType<UploadResult.UsageLimitReached>(result);
        Assert.NotNull(failure);
    }

    [Fact]
    public async Task Upload_UsageLimitReached_DoesNotCreateMetadata()
    {
        _usageLimiter.TryReserveUploadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);

        await _metadataDb.DidNotReceive().CreateAsync(Metadata, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_UsageLimitReached_DoesNotWriteFile()
    {
        _usageLimiter.TryReserveUploadAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        await _uploader.Upload(FileContent, FileName, FileSize, CancellationToken.None);

        await _fileDb.DidNotReceive().UploadAsync(FileId, FileContent, Arg.Any<CancellationToken>());
    }
}