
namespace SkyDrop.Api.Data;

public record UploadResult
{
    public record Success : UploadResult
    {
        public string FileId;

        public Success(string fileId) {
            FileId = fileId;
        }
    }
    public record UsageLimitReached : UploadResult;
}