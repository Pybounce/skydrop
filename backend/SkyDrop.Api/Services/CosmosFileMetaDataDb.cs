

using System.Net;
using Microsoft.Azure.Cosmos;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class CosmosFileMetadataDb : IFileMetadataDb
{

    private readonly Container _metadataContainer;

    public CosmosFileMetadataDb(Database db, IConfiguration config)
    {
        // I don't love having configuration outside of program.cs
        //  consider putting all container names into IOptions<CosmosContainerNames> or just a service ICosmosContainersContainer... 
        var containerName = config["Cosmos:FileMetadataContainerName"];
        if (string.IsNullOrWhiteSpace(containerName)) { throw new InvalidOperationException("Cosmos:FileMetadataContainerName is not configured."); }
        
        _metadataContainer = db.GetContainer(containerName);
    }

    public async Task CreateAsync(FileMetadata metadata, CancellationToken ct)
    {
        await _metadataContainer.CreateItemAsync(metadata);
    }

    public async Task<FileMetadata?> IncrementDownloadsAsync(string fileId, CancellationToken ct)
    {
        try
        {
            var patch_op = PatchOperation.Increment("/downloadCount", 1);
            var metadata = await _metadataContainer.PatchItemAsync<FileMetadata>(fileId, new PartitionKey(fileId), [patch_op], cancellationToken: ct);
            return metadata.Resource;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

    }

    public async Task<FileMetadata?> ReadAsync(string fileId, CancellationToken ct)
    {
        try
        {
            return (await _metadataContainer.ReadItemAsync<FileMetadata>(fileId, new PartitionKey(fileId), null, ct)).Resource;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}