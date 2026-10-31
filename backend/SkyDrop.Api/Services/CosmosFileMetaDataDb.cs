

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
        if (string.IsNullOrWhiteSpace(containerName)) { throw new InvalidOperationException("Cosmos:FileMetadataContainerName is not conofigured."); }
        
        _metadataContainer = db.GetContainer(containerName);
    }

    public Task CreateAsync(FileMetadata metadata, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<long?> IncrementDownloadsAsync(string fileId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<FileMetadata?> ReadAsync(string fileId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}