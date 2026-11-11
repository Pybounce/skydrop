
using SkyDrop.Api.ServiceInterfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;

namespace SkyDrop.Api.Services;

public class BlobFileDb : IFileDb
{
    private readonly BlobContainerClient _containerClient;

    public BlobFileDb(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public async Task UploadAsync(string fileId, Stream input, CancellationToken ct)
    {
        var blob = _containerClient.GetBlobClient(fileId);
        await blob.UploadAsync(input, new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        }, ct);
    }

    public async Task<Stream?> DownloadAsync(string fileId, CancellationToken ct)
    {
        var blob = _containerClient.GetBlobClient(fileId);
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