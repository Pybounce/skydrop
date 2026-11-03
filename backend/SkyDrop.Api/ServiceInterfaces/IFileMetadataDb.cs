

using SkyDrop.Api.DTOs;

namespace SkyDrop.Api.ServiceInterfaces;

public interface IFileMetadataDb
{
    public Task CreateAsync(FileMetadata metadata, CancellationToken ct);
    public Task<FileMetadata?> ReadAsync(string fileId, CancellationToken ct);
    public Task<FileMetadata?> IncrementDownloadsAsync(string fileId, CancellationToken ct);
}