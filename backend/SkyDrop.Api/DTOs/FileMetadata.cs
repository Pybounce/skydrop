
namespace SkyDrop.Api.DTOs;


public record FileMetadata
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required int DownloadCount { get; init; }
}