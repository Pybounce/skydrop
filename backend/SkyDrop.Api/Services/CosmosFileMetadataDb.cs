

using System.Net;
using Microsoft.Azure.Cosmos;
using SkyDrop.Api.Data;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Services;

public class CosmosFileMetadataDb : IFileMetadataDb
{
    private readonly Container _metadataContainer;

    public CosmosFileMetadataDb([FromKeyedServices(CosmosContainerKeys.Metadata)] Container metadataContainer)
    {
        _metadataContainer = metadataContainer;
    }

    public async Task CreateAsync(FileMetadata metadata, CancellationToken ct)
    {
        await _metadataContainer.CreateItemAsync(metadata, null, null, ct);
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