
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
    private readonly IKeyGenerator _keyGenerator;

    public BlobStreamService(BlobContainerClient containerClient, IKeyGenerator keyGenerator)
    {
        _containerClient = containerClient;
        _keyGenerator = keyGenerator;
    }

    public async Task<string> UploadAsync(Stream input, string fileName, CancellationToken ct)
    {
        var fileId = _keyGenerator.GenerateKey();

        var blob = _containerClient.GetBlockBlobClient(fileId);
        input.Position = 0;
        await blob.UploadAsync(input, new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
            Metadata = new Dictionary<string, string>
            {
                ["filename"] = Uri.EscapeDataString(fileName)
            },
        }, ct);

        return fileId;
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

    public async Task<FileInfo?> FileInfoAsync(string fileId, CancellationToken ct)
    {
        var blob = _containerClient.GetBlockBlobClient(fileId);
        try
        {
            var props = (await blob.GetPropertiesAsync(cancellationToken: ct)).Value;
            var fileName = props.Metadata.TryGetValue("filename", out var encodedName) ? Uri.UnescapeDataString(encodedName) : fileId;
            return new FileInfo
            {
                FileName = fileName,
                Size = props.ContentLength
            };
        } 
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

}