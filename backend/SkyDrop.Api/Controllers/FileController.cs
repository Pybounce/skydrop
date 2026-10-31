
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

    public FileController(IStreamService streamService, IUsageLimiter usageLimiter)
    {
        _streamService = streamService;
        _usageLimiter = usageLimiter;
    }

    [HttpPost(Name = "Upload")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        if (Request.ContentLength == null) { return StatusCode(StatusCodes.Status411LengthRequired); }
        var size = Request.ContentLength.Value;
        if (await _usageLimiter.TryReserveUploadAsync(size, ct) == false) { return Problem(null, null, StatusCodes.Status429TooManyRequests, "Update quota reached.", null); }

        // ensure a min size also

        var input = Request.Body;
        var fileName = GetFileName();
        var key = await _streamService.UploadAsync(input, fileName, ct);

        return Ok(Url.Link(nameof(Download), new { key }));
    }

    [HttpGet("{key}", Name = nameof(Download))]
    public async Task<IActionResult> Download(string key, CancellationToken ct)
    {

        var fileInfo = await _streamService.FileInfoAsync(key, ct);
        if (fileInfo == null) { return NotFound(); }

        var size = fileInfo.Size;
        if (await _usageLimiter.TryReserveDownloadAsync(size, ct) == false) { return Problem(null, null, StatusCodes.Status429TooManyRequests, "Download quota reached.", null); }

        var stream = await _streamService.DownloadAsync(key, ct);
        if (stream == null) { return NotFound(); }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, "application/octet-stream", fileInfo.FileName, enableRangeProcessing: true);
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