using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResourcesController : ControllerBase
    {
        private readonly IResourceService _resourceService;
        private readonly IEmployeeService _employeeService;
        private readonly ISkillMatchingService _skillMatchingService;
        private readonly ILogger<ResourcesController> _logger;

        public ResourcesController(
            IResourceService resourceService,
            IEmployeeService employeeService,
            ISkillMatchingService skillMatchingService,
            ILogger<ResourcesController> logger)
        {
            _resourceService = resourceService;
            _employeeService = employeeService;
            _skillMatchingService = skillMatchingService;
            _logger = logger;
        }

        // Labor Requirements Endpoints (read)
        [HttpGet("requirements")]
        public async Task<ActionResult<ApiResponse<List<LaborRequirementDto>>>> GetLaborRequirements(
            [FromQuery] int projectId,
            [FromQuery] DateTime? weekStartDate = null)
        {
            try
            {
                var requirements = await _resourceService.GetLaborRequirementsAsync(projectId, weekStartDate);
                return Ok(new ApiResponse<List<LaborRequirementDto>>
                {
                    Success = true,
                    Data = requirements
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving labor requirements for project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<List<LaborRequirementDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving labor requirements"
                });
            }
        }

        [HttpPost("requirements")]
        [Authorize(Roles = "Admin,ProjectManager,DepartmentManager")]
        public async Task<ActionResult<ApiResponse<bool>>> SaveLaborRequirement([FromBody] SaveLaborRequirementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid requirement data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                await _resourceService.SaveLaborRequirementAsync(request);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Labor requirement saved successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving labor requirement");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while saving the labor requirement"
                });
            }
        }

        [HttpPost("requirements/bulk")]
        [Authorize(Roles = "Admin,ProjectManager,DepartmentManager")]
        public async Task<ActionResult<ApiResponse<bool>>> BulkSaveLaborRequirements([FromBody] BulkLaborRequirementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid requirement data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                await _resourceService.BulkSaveLaborRequirementsAsync(request);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Labor requirements saved successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk saving labor requirements");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while saving labor requirements"
                });
            }
        }

        // Employee Availability
        [HttpGet("available-employees")]
        public async Task<ActionResult<ApiResponse<List<EmployeeAvailabilityDto>>>> GetAvailableEmployees(
            [FromQuery] int departmentId,
            [FromQuery] DateTime weekStartDate,
            [FromQuery] decimal minAvailableHours = 0)
        {
            if (minAvailableHours < 0)
            {
                return BadRequest(new ApiResponse<List<EmployeeAvailabilityDto>>
                {
                    Success = false,
                    Message = "minAvailableHours cannot be negative"
                });
            }

            try
            {
                var employees = await _resourceService.GetAvailableEmployeesAsync(
                    departmentId, weekStartDate, minAvailableHours);
                
                return Ok(new ApiResponse<List<EmployeeAvailabilityDto>>
                {
                    Success = true,
                    Data = employees
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available employees");
                return StatusCode(500, new ApiResponse<List<EmployeeAvailabilityDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving available employees"
                });
            }
        }

        // Employee Assignments
        [HttpGet("assignments")]
        public async Task<ActionResult<ApiResponse<List<EmployeeAssignmentDto>>>> GetAssignments(
            [FromQuery] int projectId,
            [FromQuery] DateTime? weekStartDate = null)
        {
            try
            {
                var assignments = await _resourceService.GetEmployeeAssignmentsAsync(projectId, weekStartDate);
                return Ok(new ApiResponse<List<EmployeeAssignmentDto>>
                {
                    Success = true,
                    Data = assignments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignments for project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<List<EmployeeAssignmentDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving assignments"
                });
            }
        }

        [HttpPost("assignments")]
        [Authorize(Roles = "Admin,ProjectManager,DepartmentManager")]
        public async Task<ActionResult<ApiResponse<bool>>> CreateAssignment([FromBody] CreateAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid assignment data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                await _resourceService.CreateAssignmentAsync(request);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Assignment created successfully",
                    Data = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while creating the assignment"
                });
            }
        }

        [HttpPut("assignments/{id}")]
        [Authorize(Roles = "Admin,ProjectManager,DepartmentManager")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateAssignment(int id, [FromBody] CreateAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid assignment data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                await _resourceService.UpdateAssignmentAsync(id, request);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Assignment updated successfully",
                    Data = true
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Assignment not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment {AssignmentId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while updating the assignment"
                });
            }
        }

        [HttpDelete("assignments/{id}")]
        [Authorize(Roles = "Admin,ProjectManager,DepartmentManager")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAssignment(int id)
        {
            try
            {
                var result = await _resourceService.DeleteAssignmentAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Assignment not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Assignment deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment {AssignmentId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the assignment"
                });
            }
        }

        // Timeline
        [HttpGet("timeline")]
        public async Task<ActionResult<ApiResponse<List<TimelineDto>>>> GetTimeline(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] int weekCount = 12)
        {
            if (weekCount < 1 || weekCount > 52)
            {
                return BadRequest(new ApiResponse<List<TimelineDto>>
                {
                    Success = false,
                    Message = "weekCount must be between 1 and 52"
                });
            }

            try
            {
                var timeline = await _resourceService.GetResourceTimelineAsync(startDate, weekCount);
                return Ok(new ApiResponse<List<TimelineDto>>
                {
                    Success = true,
                    Data = timeline
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resource timeline");
                return StatusCode(500, new ApiResponse<List<TimelineDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the resource timeline"
                });
            }
        }

        // Bulk Assignment
        [HttpPost("assignments/bulk")]
        [Authorize(Roles = "Admin,ProjectManager,DepartmentManager")]
        public async Task<ActionResult<ApiResponse<int>>> BulkCreateAssignments(
            [FromBody] BulkEmployeeAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Invalid assignment data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var created = await _resourceService.BulkCreateAssignmentsAsync(request);
                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Message = $"{request.Assignments.Count} assignment(s) processed ({created} new)",
                    Data = created
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating assignments");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while creating assignments"
                });
            }
        }

        // Calendar Events
        [HttpGet("calendar")]
        public async Task<ActionResult<ApiResponse<List<CalendarEventDto>>>> GetCalendarEvents(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? departmentId = null,
            [FromQuery] int? employeeId = null)
        {
            try
            {
                var events = await _resourceService.GetCalendarEventsAsync(
                    startDate, endDate, departmentId, employeeId);
                return Ok(new ApiResponse<List<CalendarEventDto>>
                {
                    Success = true,
                    Data = events
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar events");
                return StatusCode(500, new ApiResponse<List<CalendarEventDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving calendar events"
                });
            }
        }

        // Skills-Based Matching
        [HttpPost("skill-match")]
        public async Task<ActionResult<ApiResponse<List<SkillMatchResultDto>>>> FindSkillMatches(
            [FromBody] SkillMatchRequest request)
        {
            try
            {
                var matches = await _skillMatchingService.FindMatchingEmployeesAsync(request);
                return Ok(new ApiResponse<List<SkillMatchResultDto>>
                {
                    Success = true,
                    Data = matches
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding skill matches");
                return StatusCode(500, new ApiResponse<List<SkillMatchResultDto>>
                {
                    Success = false,
                    Message = "An error occurred while finding skill matches"
                });
            }
        }

        [HttpGet("skills")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetAllSkills()
        {
            try
            {
                var skills = await _skillMatchingService.GetAllSkillsAsync();
                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = skills
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving skills");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving skills"
                });
            }
        }
    }
}
