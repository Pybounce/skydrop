
using SkyDrop.Api.DTOs;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class FileDownloader : IFileDownloader
{

    private readonly IFileDb _fileDb;
    private readonly IUsageLimiter _usageLimiter;
    private readonly IFileMetadataDb _metadataDb;

    public FileDownloader(IFileDb fileDb, IUsageLimiter usageLimiter, IFileMetadataDb metadataDb)
    {
        _fileDb = fileDb;
        _usageLimiter = usageLimiter;
        _metadataDb = metadataDb;
    }

    public async Task<DownloadResult> Download(string fileId, CancellationToken ct)
    {
        var metadata = await _metadataDb.IncrementDownloadsAsync(fileId, ct);
        if (metadata == null) { return new DownloadResult.DoesNotExist(); }

        if (await _usageLimiter.TryReserveDownloadAsync(metadata.Size, ct) == false) { return new DownloadResult.UsageLimitReached(); }

        var stream = await _fileDb.DownloadAsync(fileId, ct);
        if (stream == null) { return new DownloadResult.DoesNotExist(); }

        return new DownloadResult.Success(stream, metadata);
    }
}