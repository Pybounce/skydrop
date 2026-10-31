
public record OLDDELETEME_FileInfo {
    public required string FileName { get; init; }
    public required long Size { get; init; }
}

public record FileMetadata
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required int DownloadCount { get; init; }
}