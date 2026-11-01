
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc;
using SkyDrop.Api.ServiceInterfaces;

namespace SkyDrop.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController: ControllerBase
{

    private readonly IStreamService _streamService;
    private readonly IUsageLimiter _usageLimiter;
    private readonly IKeyGenerator _keyGenerator;   // yes I know this sucks, will be splitting out into 2 services soon.
    private readonly IFileMetadataDb _metadataDb;

    public FileController(IStreamService streamService, IUsageLimiter usageLimiter, IKeyGenerator keyGenerator, IFileMetadataDb metadataDb)
    {
        _streamService = streamService;
        _usageLimiter = usageLimiter;
        _keyGenerator = keyGenerator;
        _metadataDb = metadataDb;
    }

    [HttpPost(Name = "Upload")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        if (Request.ContentLength == null) { return StatusCode(StatusCodes.Status411LengthRequired); }
        var size = Request.ContentLength.Value;
        if (await _usageLimiter.TryReserveUploadAsync(size, ct) == false) { return Problem(null, null, StatusCodes.Status429TooManyRequests, "Update quota reached.", null); }

        var fileId = _keyGenerator.GenerateKey();
        var metadata = new FileMetadata()
        {
            Id = fileId,
            Name = GetFileName(),
            Size = size,
            DownloadCount = 0
        };
        await _metadataDb.CreateAsync(metadata, ct);
        // ensure a min size also

        var input = Request.Body;
        await _streamService.UploadAsync(fileId, input, ct);

        return Ok(fileId);
    }

    [HttpGet("Download/{key}", Name = nameof(Download))]
    public async Task<IActionResult> Download(string key, CancellationToken ct)
    {
        var metadata = await _metadataDb.IncrementDownloadsAsync(key, ct);
        if (metadata == null) { return NotFound(); }

        if (await _usageLimiter.TryReserveDownloadAsync(metadata.Size, ct) == false) { return Problem(null, null, StatusCodes.Status429TooManyRequests, "Download quota reached.", null); }

        var stream = await _streamService.DownloadAsync(key, ct);
        if (stream == null) { return NotFound(); }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, "application/octet-stream", metadata.Name, enableRangeProcessing: true);
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