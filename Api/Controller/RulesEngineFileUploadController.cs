using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace RulesEngineFileUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesEngineFileUploadController : ControllerBase
    {

        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<RulesEngineFileUploadController> _logger;

        public RulesEngineFileUploadController(IBlobStorageService blobStorageService,ILogger<RulesEngineFileUploadController> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is empty.");

            using var stream = request.File.OpenReadStream();
            var fileUrl = await _blobStorageService.UploadFileAsync(stream, request.File.FileName);

            return Ok(new { FileUrl = fileUrl });
        }


        [HttpGet("index")]
        public IActionResult Index()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "Rules Engine Blob Uploader API",
                Timestamp = DateTime.UtcNow
            });
        }

    }
}
