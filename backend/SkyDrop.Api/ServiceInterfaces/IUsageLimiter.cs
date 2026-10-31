

using System.Runtime.CompilerServices;

namespace SkyDrop.Api.ServiceInterfaces;

public interface IUsageLimiter
{
    public Task<bool> TryReserveDownloadAsync(long bytes, CancellationToken ct);
    public Task<bool> TryReserveUploadAsync(long bytes, CancellationToken ct);

    // eventually could add IUploadReservation and IDownloadReservation
    //  this way if I add more limiters (downloadcountlimiter), then I can undo a reservation
    //  The concrete reservation would probably be defined alongside the concrete ratelimiter but we'll see. 
}