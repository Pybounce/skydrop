

using System.Net;
using Microsoft.Azure.Cosmos;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class CosmosUsageDb : IUsageDb
{
    private readonly Container _usageContainer;

    public CosmosUsageDb(Database db, IConfiguration config)
    {
        // I don't love having configuration outside of program.cs
        //  consider putting all container names into IOptions<CosmosContainerNames> or just a service ICosmosContainersContainer... 
        var containerName = config["Cosmos:UsageContainerName"];
        if (string.IsNullOrWhiteSpace(containerName)) { throw new InvalidOperationException("Cosmos:UsageContainerName is not conofigured."); }
        
        _usageContainer = db.GetContainer(containerName);
    }

    public async Task<bool> TryIncrementDownloadWithinLimitAsync(string id, long byte_increment, long byte_limit, CancellationToken ct)
    {
        return await TryIncrementWithinLimitAsync(id, byte_increment, byte_limit, "downloadedBytes", ct);
    }

    public async Task<bool> TryIncrementUploadWithinLimitAsync(string id, long byte_increment, long byte_limit, CancellationToken ct)
    {
        return await TryIncrementWithinLimitAsync(id, byte_increment, byte_limit, "uploadedBytes", ct);
    }

    private async Task<bool> TryIncrementWithinLimitAsync(string id, long byte_increment, long byte_limit, string fieldName, CancellationToken ct)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var patch_op = PatchOperation.Increment($"/{fieldName}", byte_increment);
                await _usageContainer.PatchItemAsync<MonthlyUsage>(id, new PartitionKey(id), [patch_op] , requestOptions: new PatchItemRequestOptions
                {
                    FilterPredicate = $"FROM c WHERE c.{fieldName} <= {byte_limit - byte_increment}"
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