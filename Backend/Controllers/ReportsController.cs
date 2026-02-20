using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportingService reportingService, ILogger<ReportsController> logger)
        {
            _reportingService = reportingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<ReportDataDto>>> GetReportData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] int weekCount = 12)
        {
            if (weekCount < 1 || weekCount > 52)
            {
                return BadRequest(new ApiResponse<ReportDataDto>
                {
                    Success = false,
                    Message = "weekCount must be between 1 and 52"
                });
            }

            try
            {
                var report = await _reportingService.GetReportDataAsync(startDate, weekCount);
                return Ok(new ApiResponse<ReportDataDto>
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report data");
                return StatusCode(500, new ApiResponse<ReportDataDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving report data"
                });
            }
        }
    }
}
