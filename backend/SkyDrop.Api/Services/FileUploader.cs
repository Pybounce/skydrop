
using SkyDrop.Api.Data;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class FileUploader : IFileUploader
{

    private readonly IFileDb _fileDb;
    private readonly IUsageLimiter _usageLimiter;
    private readonly IKeyGenerator _keyGenerator;
    private readonly IFileMetadataDb _metadataDb;

    public FileUploader(IFileDb fileDb, IUsageLimiter usageLimiter, IKeyGenerator keyGenerator, IFileMetadataDb metadataDb)
    {
        _fileDb = fileDb;
        _usageLimiter = usageLimiter;
        _keyGenerator = keyGenerator;
        _metadataDb = metadataDb;
    }

    public async Task<UploadResult> Upload(Stream content, string fileName, long fileSize, CancellationToken ct)
    {
        if (await _usageLimiter.TryReserveUploadAsync(fileSize, ct) == false) { return new UploadResult.UsageLimitReached(); }

        var fileId = _keyGenerator.GenerateKey();
        var metadata = new FileMetadata()
        {
            Id = fileId,
            Name = fileName,
            Size = fileSize,
            DownloadCount = 0
        };
        await _metadataDb.CreateAsync(metadata, ct);
        // ensure a min size also

        await _fileDb.UploadAsync(fileId, content, ct);

        return new UploadResult.Success(fileId);
    }
}