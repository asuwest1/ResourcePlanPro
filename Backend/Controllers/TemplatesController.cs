using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Services;

namespace ResourcePlanPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplateService _templateService;
        private readonly IProjectService _projectService;
        private readonly ILogger<TemplatesController> _logger;

        public TemplatesController(
            ITemplateService templateService,
            IProjectService projectService,
            ILogger<TemplatesController> logger)
        {
            _templateService = templateService;
            _projectService = projectService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProjectTemplateDto>>>> GetTemplates()
        {
            try
            {
                var templates = await _templateService.GetAllTemplatesAsync();
                return Ok(new ApiResponse<List<ProjectTemplateDto>>
                {
                    Success = true,
                    Data = templates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates");
                return StatusCode(500, new ApiResponse<List<ProjectTemplateDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving templates"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectTemplateDto>>> GetTemplate(int id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    return NotFound(new ApiResponse<ProjectTemplateDto>
                    {
                        Success = false,
                        Message = "Template not found"
                    });
                }

                return Ok(new ApiResponse<ProjectTemplateDto>
                {
                    Success = true,
                    Data = template
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
                return StatusCode(500, new ApiResponse<ProjectTemplateDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the template"
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<ApiResponse<ProjectTemplateDto>>> CreateTemplate(
            [FromBody] CreateTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProjectTemplateDto>
                    {
                        Success = false,
                        Message = "Invalid template data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized();

                var template = await _templateService.CreateTemplateAsync(request, userId);
                return CreatedAtAction(
                    nameof(GetTemplate),
                    new { id = template.TemplateId },
                    new ApiResponse<ProjectTemplateDto>
                    {
                        Success = true,
                        Message = "Template created successfully",
                        Data = template
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                return StatusCode(500, new ApiResponse<ProjectTemplateDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the template"
                });
            }
        }

        [HttpPost("from-project/{projectId}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<ApiResponse<ProjectTemplateDto>>> CreateFromProject(
            int projectId,
            [FromQuery] string templateName,
            [FromQuery] string? description = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized();

                var template = await _templateService.CreateTemplateFromProjectAsync(
                    projectId, templateName, description, userId);

                return CreatedAtAction(
                    nameof(GetTemplate),
                    new { id = template.TemplateId },
                    new ApiResponse<ProjectTemplateDto>
                    {
                        Success = true,
                        Message = "Template created from project successfully",
                        Data = template
                    });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<ProjectTemplateDto>
                {
                    Success = false,
                    Message = "Project not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template from project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<ProjectTemplateDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the template"
                });
            }
        }

        [HttpPost("create-project")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProjectFromTemplate(
            [FromBody] CreateProjectFromTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProjectDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var project = await _templateService.CreateProjectFromTemplateAsync(request);
                var projectDto = await _projectService.GetProjectByIdAsync(project.ProjectId);

                return CreatedAtAction(
                    nameof(ProjectsController),
                    new { id = project.ProjectId },
                    new ApiResponse<ProjectDto>
                    {
                        Success = true,
                        Message = "Project created from template successfully",
                        Data = projectDto
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project from template");
                return StatusCode(500, new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the project from template"
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTemplate(int id)
        {
            try
            {
                var result = await _templateService.DeleteTemplateAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Template not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Template deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the template"
                });
            }
        }
    }
}
