using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly ResourcePlanProContext _context;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(
            ResourcePlanProContext context,
            ILogger<DepartmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments(
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.Departments.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(d => d.IsActive);
                }

                var departments = await query
                    .Include(d => d.Manager)
                    .Include(d => d.Employees)
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                var dtos = departments.Select(d => new DepartmentDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    Description = d.Description,
                    ManagerUserId = d.ManagerUserId,
                    ManagerName = d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : null,
                    EmployeeCount = d.Employees.Count(e => e.IsActive),
                    IsActive = d.IsActive
                }).ToList();

                return Ok(new ApiResponse<List<DepartmentDto>>
                {
                    Success = true,
                    Data = dtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return StatusCode(500, new ApiResponse<List<DepartmentDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving departments"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Manager)
                    .Include(d => d.Employees)
                    .FirstOrDefaultAsync(d => d.DepartmentId == id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = "Department not found"
                    });
                }

                var dto = new DepartmentDto
                {
                    DepartmentId = department.DepartmentId,
                    DepartmentName = department.DepartmentName,
                    Description = department.Description,
                    ManagerUserId = department.ManagerUserId,
                    ManagerName = department.Manager != null ? $"{department.Manager.FirstName} {department.Manager.LastName}" : null,
                    EmployeeCount = department.Employees.Count(e => e.IsActive),
                    IsActive = department.IsActive
                };

                return Ok(new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department {DepartmentId}", id);
                return StatusCode(500, new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the department"
                });
            }
        }

        [HttpGet("{id}/utilization")]
        public async Task<ActionResult<ApiResponse<List<DepartmentUtilization>>>> GetDepartmentUtilization(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] int weekCount = 12)
        {
            try
            {
                var start = startDate ?? DateTime.Today;
                var weekStart = start.AddDays(-(int)start.DayOfWeek + 1);

                var utilization = await _context.DepartmentUtilizations
                    .FromSqlRaw(@"
                        SELECT * FROM vw_DepartmentUtilization 
                        WHERE DepartmentId = {0} 
                        AND WeekStartDate >= {1}
                        AND WeekStartDate < {2}
                        ORDER BY WeekStartDate",
                        id, weekStart, weekStart.AddDays(7 * weekCount))
                    .ToListAsync();

                return Ok(new ApiResponse<List<DepartmentUtilization>>
                {
                    Success = true,
                    Data = utilization
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department utilization for {DepartmentId}", id);
                return StatusCode(500, new ApiResponse<List<DepartmentUtilization>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving department utilization"
                });
            }
        }
    }
}
