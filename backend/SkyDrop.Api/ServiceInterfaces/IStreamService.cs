

namespace SkyDrop.Api.ServiceInterfaces;

public interface IStreamService
{
    public Task UploadAsync(string fileId, Stream input, CancellationToken ct);

    public Task<Stream?> DownloadAsync(string key, CancellationToken ct);
}