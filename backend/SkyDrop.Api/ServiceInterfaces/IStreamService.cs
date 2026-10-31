

namespace SkyDrop.Api.ServiceInterfaces;

public interface IStreamService
{
    public Task<string> UploadAsync(Stream input, string fileName, CancellationToken ct);

    public Task<Stream?> DownloadAsync(string key, CancellationToken ct);

    public Task<OLDDELETEME_FileInfo?> FileInfoAsync(string fileId, CancellationToken ct);

}