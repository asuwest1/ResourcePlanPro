using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProjectDto>>>> GetProjects([FromQuery] int? managerId = null)
        {
            try
            {
                var projects = await _projectService.GetProjectDashboardAsync(managerId);
                return Ok(new ApiResponse<List<ProjectDto>>
                {
                    Success = true,
                    Data = projects
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects");
                return StatusCode(500, new ApiResponse<List<ProjectDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving projects"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProject(int id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                if (project == null)
                {
                    return NotFound(new ApiResponse<ProjectDto>
                    {
                        Success = false,
                        Message = "Project not found"
                    });
                }

                return Ok(new ApiResponse<ProjectDto>
                {
                    Success = true,
                    Data = project
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project {ProjectId}", id);
                return StatusCode(500, new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the project"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProjectDto>
                    {
                        Success = false,
                        Message = "Invalid project data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var project = await _projectService.CreateProjectAsync(request);
                var projectDto = await _projectService.GetProjectByIdAsync(project.ProjectId);

                return CreatedAtAction(
                    nameof(GetProject),
                    new { id = project.ProjectId },
                    new ApiResponse<ProjectDto>
                    {
                        Success = true,
                        Message = "Project created successfully",
                        Data = projectDto
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return StatusCode(500, new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the project"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(int id, [FromBody] CreateProjectRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProjectDto>
                    {
                        Success = false,
                        Message = "Invalid project data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var project = await _projectService.UpdateProjectAsync(id, request);
                var projectDto = await _projectService.GetProjectByIdAsync(project.ProjectId);

                return Ok(new ApiResponse<ProjectDto>
                {
                    Success = true,
                    Message = "Project updated successfully",
                    Data = projectDto
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "Project not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", id);
                return StatusCode(500, new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the project"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(int id)
        {
            try
            {
                var result = await _projectService.DeleteProjectAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Project not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Project deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the project"
                });
            }
        }
    }
}
