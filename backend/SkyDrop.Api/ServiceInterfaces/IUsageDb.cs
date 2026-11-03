

namespace SkyDrop.Api.ServiceInterfaces;

public interface IUsageDb
{
    public Task<bool> TryIncrementUploadWithinLimitAsync(string id, long byteIncrement, long byteLimit, CancellationToken ct);
    public Task<bool> TryIncrementDownloadWithinLimitAsync(string id, long byteIncrement, long byteLimit, CancellationToken ct);
}