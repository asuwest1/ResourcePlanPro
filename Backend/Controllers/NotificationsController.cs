using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<NotificationLogDto>>>> GetHistory(
            [FromQuery] int count = 50)
        {
            try
            {
                var history = await _notificationService.GetNotificationHistoryAsync(count);
                return Ok(new ApiResponse<List<NotificationLogDto>>
                {
                    Success = true,
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification history");
                return StatusCode(500, new ApiResponse<List<NotificationLogDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving notification history"
                });
            }
        }

        [HttpPost("send-conflict-report")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<ApiResponse<NotificationLogDto>>> SendConflictReport(
            [FromBody] SendNotificationRequest request)
        {
            try
            {
                var result = await _notificationService.SendConflictNotificationsAsync(request);
                return Ok(new ApiResponse<NotificationLogDto>
                {
                    Success = true,
                    Message = result.Status == "NoAction"
                        ? "No conflicts found to report"
                        : "Conflict notification processed successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending conflict notifications");
                return StatusCode(500, new ApiResponse<NotificationLogDto>
                {
                    Success = false,
                    Message = "An error occurred while sending conflict notifications"
                });
            }
        }

        [HttpPost("check-conflicts")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<int>>> CheckAndNotifyConflicts(
            [FromBody] List<string> recipientEmails)
        {
            try
            {
                var count = await _notificationService.CheckAndNotifyConflictsAsync(recipientEmails);
                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Message = count > 0
                        ? $"Found {count} conflict(s) and notifications processed"
                        : "No conflicts found",
                    Data = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conflicts");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while checking for conflicts"
                });
            }
        }
    }
}
