

using SkyDrop.Api.Data;

namespace SkyDrop.Api.ServiceInterfaces;

public interface IFileUploader
{
    public Task<UploadResult> Upload(Stream content, string fileName, long fileSize, CancellationToken ct);
}