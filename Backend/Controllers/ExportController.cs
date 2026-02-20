using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(IExportService exportService, ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        [HttpGet("projects")]
        public async Task<IActionResult> ExportProjects([FromQuery] int? projectId = null)
        {
            try
            {
                var csv = await _exportService.ExportProjectsToCsvAsync(projectId);
                return File(csv, "text/csv", $"projects_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting projects");
                return StatusCode(500, "An error occurred while exporting projects");
            }
        }

        [HttpGet("employees")]
        public async Task<IActionResult> ExportEmployees([FromQuery] int? departmentId = null)
        {
            try
            {
                var csv = await _exportService.ExportEmployeesToCsvAsync(departmentId);
                return File(csv, "text/csv", $"employees_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting employees");
                return StatusCode(500, "An error occurred while exporting employees");
            }
        }

        [HttpGet("assignments")]
        public async Task<IActionResult> ExportAssignments(
            [FromQuery] int? projectId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var csv = await _exportService.ExportAssignmentsToCsvAsync(projectId, startDate, endDate);
                return File(csv, "text/csv", $"assignments_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting assignments");
                return StatusCode(500, "An error occurred while exporting assignments");
            }
        }

        [HttpGet("conflicts")]
        public async Task<IActionResult> ExportConflicts()
        {
            try
            {
                var csv = await _exportService.ExportConflictsToCsvAsync();
                return File(csv, "text/csv", $"conflicts_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting conflicts");
                return StatusCode(500, "An error occurred while exporting conflicts");
            }
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> ExportTimeline(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] int weekCount = 12)
        {
            if (weekCount < 1 || weekCount > 52)
                return BadRequest("weekCount must be between 1 and 52");

            try
            {
                var csv = await _exportService.ExportResourceTimelineToCsvAsync(startDate, weekCount);
                return File(csv, "text/csv", $"timeline_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting timeline");
                return StatusCode(500, "An error occurred while exporting timeline data");
            }
        }
    }
}
