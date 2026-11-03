
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SkyDrop.Api.DTOs;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController: ControllerBase
{
    private readonly IFileUploader _fileUploader;
    private readonly IFileDownloader _fileDownloader;
    private readonly IFileMetadataDb _metadataDb;

    public FileController(IFileUploader fileUploader, IFileDownloader fileDownloader, IFileMetadataDb metadataDb)
    {
        _fileUploader = fileUploader;
        _fileDownloader = fileDownloader;
        _metadataDb = metadataDb;
    }

    [HttpPost(Name = "Upload")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        if (Request.ContentLength == null) { return StatusCode(StatusCodes.Status411LengthRequired); }
        var size = Request.ContentLength.Value;
        var name = GetFileName();
        var content = Request.Body;

        var uploadResult = await _fileUploader.Upload(content, name, size, ct);
        return uploadResult switch {
            UploadResult.Success r => Ok(r.FileId),
            UploadResult.UsageLimitReached => Problem(null, null, StatusCodes.Status429TooManyRequests, "Update quota reached.", null),
            _ => throw new UnreachableException() 
        };
    }

    [HttpGet("Download/{key}", Name = nameof(Download))]
    public async Task<IActionResult> Download(string key, CancellationToken ct)
    {

        var downloadResult = await _fileDownloader.Download(key, ct);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return downloadResult switch
        {
            DownloadResult.Success r => File(r.Content, "application/octet-stream", r.Metadata.Name, enableRangeProcessing: true),
            DownloadResult.UsageLimitReached => Problem(null, null, StatusCodes.Status429TooManyRequests, "Download quota reached.", null),
            _ => throw new UnreachableException() 
        };
    }

    [HttpGet("Metadata/{key}", Name = nameof(Metadata))]
    public async Task<IActionResult> Metadata(string key, CancellationToken ct)
    {
        var metadata = await _metadataDb.ReadAsync(key, ct);
        if (metadata == null) { return NotFound(); }
        return Ok(metadata);
    }

    private string GetFileName()
    {
        var raw = Request.Headers["X-File-Name"].ToString();
        var name = string.IsNullOrWhiteSpace(raw) ? "file" : Uri.UnescapeDataString(raw);
        name = Path.GetFileName(name);
        if (name.Length > 200) { name = name[..200]; }
        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

}