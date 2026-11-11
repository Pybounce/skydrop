

using System.Globalization;
using Microsoft.Extensions.Options;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class MonthlyUsageLimiter : IUsageLimiter
{
    private readonly IUsageDb _usageDb;
    private readonly MonthlyUsageLimits _usageLimits;
    private readonly TimeProvider _time;

    public MonthlyUsageLimiter(IUsageDb usageDb, IOptions<MonthlyUsageLimits> usageLimits, TimeProvider time)
    {
        _usageDb = usageDb;
        _usageLimits = usageLimits.Value;
        _time = time;
    }
    
    public async Task<bool> TryReserveDownloadAsync(long bytes, CancellationToken ct)
    {
        return await _usageDb.TryIncrementDownloadWithinLimitAsync(GenerateMonthId(), bytes, _usageLimits.DownloadedBytes, ct);
    }

    public async Task<bool> TryReserveUploadAsync(long bytes, CancellationToken ct)
    {
        return await _usageDb.TryIncrementUploadWithinLimitAsync(GenerateMonthId(), bytes, _usageLimits.UploadedBytes, ct);
    }

    private string GenerateMonthId()
    {
        return $"monthlim-{_time.GetUtcNow().ToString("yyyy-MM", CultureInfo.InvariantCulture).ToLower()}";
    }
}