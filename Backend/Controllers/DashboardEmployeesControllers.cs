using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized();
                }

                var dashboard = await _dashboardService.GetDashboardDataAsync(userId);
                return Ok(new ApiResponse<DashboardDto>
                {
                    Success = true,
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, new ApiResponse<DashboardDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving dashboard data"
                });
            }
        }

        [HttpGet("conflicts")]
        public async Task<ActionResult<ApiResponse<List<ConflictSummaryDto>>>> GetConflicts()
        {
            try
            {
                var conflicts = await _dashboardService.GetConflictsAsync();
                return Ok(new ApiResponse<List<ConflictSummaryDto>>
                {
                    Success = true,
                    Data = conflicts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conflicts");
                return StatusCode(500, new ApiResponse<List<ConflictSummaryDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving conflicts"
                });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<QuickStatsDto>>> GetStats()
        {
            try
            {
                var stats = await _dashboardService.GetQuickStatsAsync();
                return Ok(new ApiResponse<QuickStatsDto>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quick stats");
                return StatusCode(500, new ApiResponse<QuickStatsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving statistics"
                });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetEmployees([FromQuery] bool includeInactive = false)
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync(includeInactive);
                return Ok(new ApiResponse<List<EmployeeDto>>
                {
                    Success = true,
                    Data = employees
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, new ApiResponse<List<EmployeeDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employees"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(int id)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    return NotFound(new ApiResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                return Ok(new ApiResponse<EmployeeDto>
                {
                    Success = true,
                    Data = employee
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
                return StatusCode(500, new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the employee"
                });
            }
        }

        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetEmployeesByDepartment(int departmentId)
        {
            try
            {
                var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);
                return Ok(new ApiResponse<List<EmployeeDto>>
                {
                    Success = true,
                    Data = employees
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for department {DepartmentId}", departmentId);
                return StatusCode(500, new ApiResponse<List<EmployeeDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employees"
                });
            }
        }

        [HttpGet("{id}/workload")]
        public async Task<ActionResult<ApiResponse<Models.EmployeeWorkloadSummary>>> GetEmployeeWorkload(
            int id,
            [FromQuery] DateTime weekStartDate)
        {
            try
            {
                var workload = await _employeeService.GetEmployeeWorkloadAsync(id, weekStartDate);
                if (workload == null)
                {
                    return NotFound(new ApiResponse<Models.EmployeeWorkloadSummary>
                    {
                        Success = false,
                        Message = "Employee workload data not found"
                    });
                }

                return Ok(new ApiResponse<Models.EmployeeWorkloadSummary>
                {
                    Success = true,
                    Data = workload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workload for employee {EmployeeId}", id);
                return StatusCode(500, new ApiResponse<Models.EmployeeWorkloadSummary>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employee workload"
                });
            }
        }
    }
}
