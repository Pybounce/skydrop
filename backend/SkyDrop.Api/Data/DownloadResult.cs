
namespace SkyDrop.Api.Data;

public record DownloadResult
{
    public record Success : DownloadResult
    {
        public Stream Content;
        public FileMetadata Metadata;

        public Success(Stream content, FileMetadata metadata) {
            Content = content;
            Metadata = metadata;
        }
    }

    public record UsageLimitReached : DownloadResult;

    public record DoesNotExist : DownloadResult;
}