
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SkyDrop.Api.ServiceInterfaces;
using SkyDrop.Api.Services;

namespace SkyDrop.Api.Tests.Services;

public class MonthlyUsageLimiterTests
{
    private const long DownloadLimit = 100_000_000_000;
    private const long UploadLimit = 101_000_000_000;
    private readonly IUsageDb _usageDb = Substitute.For<IUsageDb>(); 
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly MonthlyUsageLimiter _limiter;

    public MonthlyUsageLimiterTests()
    {
        var limits = Options.Create(new MonthlyUsageLimits
        {
            DownloadedBytes = DownloadLimit,
            UploadedBytes = UploadLimit
        });
        _limiter = new MonthlyUsageLimiter(_usageDb, limits, _time);
    }

    [Fact]
    public async Task TryReserveDownload_PassesMonthIdAndDownloadLimit()
    {
        await _limiter.TryReserveDownloadAsync(5_000, CancellationToken.None);

        await _usageDb.Received(1).TryIncrementDownloadWithinLimitAsync("monthlim-2026-10", 5_000, DownloadLimit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryReserveUpload_PassesMonthIdAndUploadLimit()
    {
        await _limiter.TryReserveUploadAsync(6_000, CancellationToken.None);

        await _usageDb.Received(1).TryIncrementUploadWithinLimitAsync("monthlim-2026-10", 6_000, UploadLimit, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryReserveDownload_ReturnsSuccessOrFailure(bool dbResult)
    {
        _usageDb.TryIncrementDownloadWithinLimitAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(dbResult);

        var result = await _limiter.TryReserveDownloadAsync(5_000, CancellationToken.None);

        Assert.Equal(dbResult, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryReserveUpload_ReturnsSuccessOrFailure(bool dbResult)
    {
        _usageDb.TryIncrementUploadWithinLimitAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(dbResult);

        var result = await _limiter.TryReserveUploadAsync(5_000, CancellationToken.None);

        Assert.Equal(dbResult, result);
    }

    [Fact]
    public async Task TryReserveDownload_MonthRollover_WritesToNewMonth()
    {
        _time.SetUtcNow(new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero));
        
        await _limiter.TryReserveDownloadAsync(5_000, CancellationToken.None);
        _time.Advance(TimeSpan.FromDays(2));
        await _limiter.TryReserveDownloadAsync(6_000, CancellationToken.None);

        await _usageDb.Received(1).TryIncrementDownloadWithinLimitAsync("monthlim-2026-10", 5_000, DownloadLimit, Arg.Any<CancellationToken>());
        await _usageDb.Received(1).TryIncrementDownloadWithinLimitAsync("monthlim-2026-11", 6_000, DownloadLimit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryReserveUpload_MonthRollover_WritesToNewMonth()
    {
        _time.SetUtcNow(new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero));
        
        await _limiter.TryReserveUploadAsync(5_000, CancellationToken.None);
        _time.Advance(TimeSpan.FromDays(2));
        await _limiter.TryReserveUploadAsync(6_000, CancellationToken.None);

        await _usageDb.Received(1).TryIncrementUploadWithinLimitAsync("monthlim-2026-10", 5_000, UploadLimit, Arg.Any<CancellationToken>());
        await _usageDb.Received(1).TryIncrementUploadWithinLimitAsync("monthlim-2026-11", 6_000, UploadLimit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryReserveDownload_MonthIdIgnoresServerCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            await _limiter.TryReserveDownloadAsync(5_000, CancellationToken.None);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        await _usageDb.Received(1).TryIncrementDownloadWithinLimitAsync("monthlim-2026-10", 5_000, DownloadLimit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryReserveUpload_MonthIdIgnoresServerCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            await _limiter.TryReserveUploadAsync(5_000, CancellationToken.None);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        await _usageDb.Received(1).TryIncrementUploadWithinLimitAsync("monthlim-2026-10", 5_000, UploadLimit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryReserveDownload_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _limiter.TryReserveDownloadAsync(5_000, cts.Token);

        await _usageDb.Received(1).TryIncrementDownloadWithinLimitAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), cts.Token);
    }

    [Fact]
    public async Task TryReserveUpload_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _limiter.TryReserveUploadAsync(5_000, cts.Token);

        await _usageDb.Received(1).TryIncrementUploadWithinLimitAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), cts.Token);
    }
}