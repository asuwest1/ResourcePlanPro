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
        private readonly ILogger<ResourcesController> _logger;

        public ResourcesController(
            IResourceService resourceService,
            IEmployeeService employeeService,
            ILogger<ResourcesController> logger)
        {
            _resourceService = resourceService;
            _employeeService = employeeService;
            _logger = logger;
        }

        // Labor Requirements Endpoints
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
    }
}
