

namespace SkyDrop.Api.ServiceInterfaces;

public interface IUsageDb
{
    public Task<bool> TryIncrementUploadWithinLimitAsync(string id, long byte_increment, long byte_limit, CancellationToken ct);
    public Task<bool> TryIncrementDownloadWithinLimitAsync(string id, long byte_increment, long byte_limit, CancellationToken ct);
}