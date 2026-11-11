

using System.Net;
using Microsoft.Azure.Cosmos;
using SkyDrop.Api.Data;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class CosmosUsageDb : IUsageDb
{
    private readonly Container _usageContainer;

    public CosmosUsageDb([FromKeyedServices(CosmosContainerKeys.Usage)] Container usageContainer)
    {
        _usageContainer = usageContainer;
    }

    public async Task<bool> TryIncrementDownloadWithinLimitAsync(string id, long byteIncrement, long byteLimit, CancellationToken ct)
    {
        return await TryIncrementWithinLimitAsync(id, byteIncrement, byteLimit, "downloadedBytes", ct);
    }

    public async Task<bool> TryIncrementUploadWithinLimitAsync(string id, long byteIncrement, long byteLimit, CancellationToken ct)
    {
        return await TryIncrementWithinLimitAsync(id, byteIncrement, byteLimit, "uploadedBytes", ct);
    }

    private async Task<bool> TryIncrementWithinLimitAsync(string id, long byteIncrement, long byteLimit, string fieldName, CancellationToken ct)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var patch_op = PatchOperation.Increment($"/{fieldName}", byteIncrement);
                await _usageContainer.PatchItemAsync<MonthlyUsage>(id, new PartitionKey(id), [patch_op] , requestOptions: new PatchItemRequestOptions
                {
                    FilterPredicate = $"FROM c WHERE c.{fieldName} <= {byteLimit - byteIncrement}"
                }, ct);
                return true;
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                await CreateIfNotExistsAsync(id, ct);
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
        }

        throw new InvalidOperationException($"Failed to increment {fieldName} with id {id}");
    }

    private async Task CreateIfNotExistsAsync(string id, CancellationToken ct)
    {
        try
        {
            await _usageContainer.CreateItemAsync(new MonthlyUsage
            {
                Id = id,
                DownloadedBytes = 0,
                UploadedBytes = 0
            },
            new PartitionKey(id), cancellationToken: ct);
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.Conflict)
        {
            // this is fine, was created by another request elsewhere.
        }
    }

}