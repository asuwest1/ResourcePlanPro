using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly EmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            EmployeeService employeeService,
            ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetEmployees(
            [FromQuery] bool includeInactive = false)
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
                
                return Ok(new ApiResponse<EmployeeDto>
                {
                    Success = true,
                    Data = employee
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
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

        [HttpGet("{id}/workload")]
        public async Task<ActionResult<ApiResponse<List<EmployeeWorkloadDto>>>> GetEmployeeWorkload(
            int id,
            [FromQuery] DateTime? weekStartDate = null,
            [FromQuery] int weekCount = 12)
        {
            if (weekCount < 1 || weekCount > 52)
            {
                return BadRequest(new ApiResponse<List<EmployeeWorkloadDto>>
                {
                    Success = false,
                    Message = "weekCount must be between 1 and 52"
                });
            }

            try
            {
                var workload = await _employeeService.GetEmployeeWorkloadAsync(id, weekStartDate, weekCount);
                
                return Ok(new ApiResponse<List<EmployeeWorkloadDto>>
                {
                    Success = true,
                    Data = workload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workload for employee {EmployeeId}", id);
                return StatusCode(500, new ApiResponse<List<EmployeeWorkloadDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employee workload"
                });
            }
        }

        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetEmployeesByDepartment(
            int departmentId,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId, includeInactive);
                
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
    }
}
