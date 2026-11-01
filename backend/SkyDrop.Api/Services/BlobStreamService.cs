
using SkyDrop.Api.ServiceInterfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Core;
using Azure.Storage.Blobs.Models;
using Azure;

namespace SkyDrop.Api.Services;

public class BlobStreamService : IStreamService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStreamService(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public async Task UploadAsync(string fileId, Stream input, CancellationToken ct)
    {
        var blob = _containerClient.GetBlockBlobClient(fileId);
        input.Position = 0;
        await blob.UploadAsync(input, new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        }, ct);
    }

    public async Task<Stream?> DownloadAsync(string fileId, CancellationToken ct)
    {
        var blob = _containerClient.GetBlockBlobClient(fileId);
        try
        {
            return await blob.OpenReadAsync(new BlobOpenReadOptions(false), ct);
        } 
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
        
    }
}