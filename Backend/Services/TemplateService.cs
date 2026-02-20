using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public interface ITemplateService
    {
        Task<List<ProjectTemplateDto>> GetAllTemplatesAsync();
        Task<ProjectTemplateDto?> GetTemplateByIdAsync(int templateId);
        Task<ProjectTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, int userId);
        Task<ProjectTemplateDto> CreateTemplateFromProjectAsync(int projectId, string templateName, string? description, int userId);
        Task<bool> DeleteTemplateAsync(int templateId);
        Task<Project> CreateProjectFromTemplateAsync(CreateProjectFromTemplateRequest request);
    }

    public class TemplateService : ITemplateService
    {
        private readonly ResourcePlanProContext _context;

        public TemplateService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<List<ProjectTemplateDto>> GetAllTemplatesAsync()
        {
            var templates = await _context.ProjectTemplates
                .Include(t => t.CreatedBy)
                .Where(t => t.IsActive)
                .OrderBy(t => t.TemplateName)
                .ToListAsync();

            return templates.Select(MapToDto).ToList();
        }

        public async Task<ProjectTemplateDto?> GetTemplateByIdAsync(int templateId)
        {
            var template = await _context.ProjectTemplates
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.IsActive);

            return template == null ? null : MapToDto(template);
        }

        public async Task<ProjectTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, int userId)
        {
            var template = new ProjectTemplate
            {
                TemplateName = request.TemplateName,
                Description = request.Description,
                Priority = request.Priority,
                DurationWeeks = request.DurationWeeks,
                DepartmentIds = request.DepartmentIds.Any()
                    ? JsonSerializer.Serialize(request.DepartmentIds)
                    : null,
                DefaultHoursJson = request.DefaultHours.Any()
                    ? JsonSerializer.Serialize(request.DefaultHours)
                    : null,
                CreatedByUserId = userId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.ProjectTemplates.Add(template);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            var created = await _context.ProjectTemplates
                .Include(t => t.CreatedBy)
                .FirstAsync(t => t.TemplateId == template.TemplateId);

            return MapToDto(created);
        }

        public async Task<ProjectTemplateDto> CreateTemplateFromProjectAsync(
            int projectId, string templateName, string? description, int userId)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectDepartments)
                .Include(p => p.LaborRequirements)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found");

            var deptIds = project.ProjectDepartments.Select(pd => pd.DepartmentId).ToList();

            // Calculate week-based hours from actual requirements
            var projectStart = project.StartDate;
            var defaultHours = project.LaborRequirements
                .Select(lr => new TemplateHourEntry
                {
                    DepartmentId = lr.DepartmentId,
                    WeekNumber = (int)((lr.WeekStartDate - projectStart).TotalDays / 7),
                    Hours = lr.RequiredHours
                })
                .Where(h => h.WeekNumber >= 0)
                .ToList();

            var durationWeeks = defaultHours.Any()
                ? defaultHours.Max(h => h.WeekNumber) + 1
                : (int)Math.Ceiling((project.EndDate - project.StartDate).TotalDays / 7);

            var request = new CreateTemplateRequest
            {
                TemplateName = templateName,
                Description = description ?? $"Template created from project: {project.ProjectName}",
                Priority = project.Priority,
                DurationWeeks = durationWeeks,
                DepartmentIds = deptIds,
                DefaultHours = defaultHours
            };

            return await CreateTemplateAsync(request, userId);
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            var template = await _context.ProjectTemplates.FindAsync(templateId);
            if (template == null)
                return false;

            template.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Project> CreateProjectFromTemplateAsync(CreateProjectFromTemplateRequest request)
        {
            var template = await _context.ProjectTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == request.TemplateId && t.IsActive);

            if (template == null)
                throw new KeyNotFoundException($"Template with ID {request.TemplateId} not found");

            var endDate = request.StartDate.AddDays(template.DurationWeeks * 7);

            var project = new Project
            {
                ProjectName = request.ProjectName,
                Description = request.Description ?? template.Description,
                ProjectManagerId = request.ProjectManagerId,
                StartDate = request.StartDate,
                EndDate = endDate,
                Priority = template.Priority,
                Status = "Planning",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add departments
            if (!string.IsNullOrEmpty(template.DepartmentIds))
            {
                var deptIds = JsonSerializer.Deserialize<List<int>>(template.DepartmentIds) ?? new List<int>();
                foreach (var deptId in deptIds)
                {
                    _context.ProjectDepartments.Add(new ProjectDepartment
                    {
                        ProjectId = project.ProjectId,
                        DepartmentId = deptId,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Add default labor requirements
            if (!string.IsNullOrEmpty(template.DefaultHoursJson))
            {
                var hours = JsonSerializer.Deserialize<List<TemplateHourEntry>>(template.DefaultHoursJson)
                    ?? new List<TemplateHourEntry>();

                foreach (var entry in hours)
                {
                    var weekStart = request.StartDate.AddDays(entry.WeekNumber * 7);
                    // Align to Monday
                    var dayOfWeek = (int)weekStart.DayOfWeek;
                    var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
                    weekStart = weekStart.AddDays(diff);

                    _context.WeeklyLaborRequirements.Add(new WeeklyLaborRequirement
                    {
                        ProjectId = project.ProjectId,
                        DepartmentId = entry.DepartmentId,
                        WeekStartDate = weekStart,
                        RequiredHours = entry.Hours,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            return project;
        }

        private ProjectTemplateDto MapToDto(ProjectTemplate template)
        {
            var deptIds = !string.IsNullOrEmpty(template.DepartmentIds)
                ? JsonSerializer.Deserialize<List<int>>(template.DepartmentIds) ?? new List<int>()
                : new List<int>();

            var defaultHours = !string.IsNullOrEmpty(template.DefaultHoursJson)
                ? JsonSerializer.Deserialize<List<TemplateHourEntry>>(template.DefaultHoursJson) ?? new List<TemplateHourEntry>()
                : new List<TemplateHourEntry>();

            return new ProjectTemplateDto
            {
                TemplateId = template.TemplateId,
                TemplateName = template.TemplateName,
                Description = template.Description,
                Priority = template.Priority,
                DurationWeeks = template.DurationWeeks,
                DepartmentIds = deptIds,
                DefaultHours = defaultHours,
                CreatedByName = template.CreatedBy != null
                    ? $"{template.CreatedBy.FirstName} {template.CreatedBy.LastName}"
                    : "Unknown",
                CreatedDate = template.CreatedDate
            };
        }
    }
}
