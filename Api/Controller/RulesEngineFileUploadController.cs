using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Application.Interfaces;

namespace RulesEngineFileUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesEngineFileUploadController : ControllerBase
    {

        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<RulesEngineFileUploadController> _logger;

        public RulesEngineFileUploadController(ILogger<RulesEngineFileUploadController> logger)
        {
            _logger = logger;
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
