

using SkyDrop.Api.DTOs;

namespace SkyDrop.Api.ServiceInterfaces;

public interface IFileDownloader
{
    public Task<DownloadResult> Download(string fileId, CancellationToken ct);
}