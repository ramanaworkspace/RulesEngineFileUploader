using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Models;
using Application.Interfaces;

namespace RulesEngineFileUploader.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesEngineFileUploadController : ControllerBase
{
    private readonly IBlobStorageService _storage;

    public RulesEngineFileUploadController(IBlobStorageService storage) => _storage = storage;

    /// <summary>Upload a rules JSON file (multipart/form-data). Creates a versioned blob and updates the stable alias.</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "RulesUploader")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadResponse>> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file.");
        await using var stream = file.OpenReadStream();

        var uploadedBy = User.Identity?.Name ?? "unknown";
        var result = await _storage.UploadVersionedAsync(stream, uploadedBy, ct);
        return Ok(result);
    }

    /// <summary>List all versions (including the stable alias).</summary>
    [HttpGet("versions")]
    [Authorize(Roles = "RulesUploader")]
    public async Task<ActionResult<IReadOnlyList<BlobVersionInfo>>> List(CancellationToken ct)
        => Ok(await _storage.ListVersionsAsync(ct));

    /// <summary>Get the URL of the current latest (stable alias).</summary>
    [HttpGet("latest-url")]
    [AllowAnonymous] // or [Authorize] if you want protection
    public async Task<ActionResult<string>> LatestUrl(CancellationToken ct)
        => Ok(await _storage.GetLatestUrlAsync(ct));

    /// <summary>Rollback latest to a specific versioned blob name.</summary>
    [HttpPost("rollback")]
    [Authorize(Roles = "RulesUploader")]
    public async Task<ActionResult<UploadResponse>> Rollback([FromQuery] string versionedName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(versionedName)) return BadRequest("versionedName is required.");
        var requestedBy = User.Identity?.Name ?? "unknown";
        var result = await _storage.RollbackAsync(versionedName, requestedBy, ct);
        return Ok(result);
    }
}
