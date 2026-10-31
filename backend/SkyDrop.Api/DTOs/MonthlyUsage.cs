public record MonthlyUsage
{
    public required string Id { get; init; }
    public required long UploadedBytes { get; init; }
    public required long DownloadedBytes { get; init; }
}