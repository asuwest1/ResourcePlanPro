using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models;
using ResourcePlanPro.API.Models.DTOs;
using ResourcePlanPro.API.Utilities;

namespace ResourcePlanPro.API.Services
{
    public interface IProjectService
    {
        Task<List<ProjectDto>> GetProjectDashboardAsync(int? projectManagerId = null);
        Task<ProjectDto?> GetProjectByIdAsync(int projectId);
        Task<Project> CreateProjectAsync(CreateProjectRequest request);
        Task<Project> UpdateProjectAsync(int projectId, CreateProjectRequest request);
        Task<bool> DeleteProjectAsync(int projectId);
        Task<List<ProjectDto>> GetProjectsByManagerAsync(int managerId);
    }

    public class ProjectService : IProjectService
    {
        private readonly ResourcePlanProContext _context;

        public ProjectService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<List<ProjectDto>> GetProjectDashboardAsync(int? projectManagerId = null)
        {
            var projectManagerIdParam = new SqlParameter("@ProjectManagerId", 
                projectManagerId.HasValue ? (object)projectManagerId.Value : DBNull.Value);

            var projects = await _context.Database
                .SqlQueryRaw<ProjectDashboardResult>(
                    "EXEC sp_GetProjectDashboard @ProjectManagerId",
                    projectManagerIdParam)
                .ToListAsync();

            return projects.Select(p => new ProjectDto
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Priority = p.Priority,
                Status = p.Status,
                ProjectManagerName = p.ProjectManager,
                DepartmentCount = p.DepartmentCount,
                EmployeeCount = p.EmployeeCount,
                CurrentWeekStatus = p.CurrentWeekStatus ?? "Green",
                CurrentWeekRequiredHours = p.CurrentWeekRequiredHours,
                CurrentWeekAssignedHours = p.CurrentWeekAssignedHours
            }).ToList();
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.ProjectDepartments)
                .Include(p => p.EmployeeAssignments)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                return null;

            var currentWeekStart = DateTimeHelper.GetWeekStartDate(DateTime.Today);
            var weekRequirements = await _context.WeeklyLaborRequirements
                .Where(w => w.ProjectId == projectId && w.WeekStartDate == currentWeekStart)
                .ToListAsync();

            var weekAssignments = await _context.EmployeeAssignments
                .Where(a => a.ProjectId == projectId && a.WeekStartDate == currentWeekStart)
                .ToListAsync();

            return new ProjectDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Description = project.Description,
                ProjectManagerId = project.ProjectManagerId,
                ProjectManagerName = project.ProjectManager != null
                    ? $"{project.ProjectManager.FirstName} {project.ProjectManager.LastName}"
                    : "Unassigned",
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Priority = project.Priority,
                Status = project.Status,
                DepartmentCount = project.ProjectDepartments.Count,
                EmployeeCount = project.EmployeeAssignments.Select(a => a.EmployeeId).Distinct().Count(),
                CurrentWeekRequiredHours = weekRequirements.Sum(w => w.RequiredHours),
                CurrentWeekAssignedHours = weekAssignments.Sum(a => a.AssignedHours)
            };
        }

        public async Task<Project> CreateProjectAsync(CreateProjectRequest request)
        {
            var project = new Project
            {
                ProjectName = request.ProjectName,
                Description = request.Description,
                ProjectManagerId = request.ProjectManagerId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Priority = request.Priority,
                Status = "Planning",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add project departments
            if (request.DepartmentIds != null && request.DepartmentIds.Any())
            {
                foreach (var deptId in request.DepartmentIds)
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

            return project;
        }

        public async Task<Project> UpdateProjectAsync(int projectId, CreateProjectRequest request)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectDepartments)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                throw new KeyNotFoundException($"Project with ID {projectId} not found");

            project.ProjectName = request.ProjectName;
            project.Description = request.Description;
            project.ProjectManagerId = request.ProjectManagerId;
            project.StartDate = request.StartDate;
            project.EndDate = request.EndDate;
            project.Priority = request.Priority;
            project.ModifiedDate = DateTime.UtcNow;

            // Update project departments
            var existingDeptIds = project.ProjectDepartments.Select(pd => pd.DepartmentId).ToList();
            var newDeptIds = request.DepartmentIds ?? new List<int>();

            // Remove departments
            var toRemove = project.ProjectDepartments
                .Where(pd => !newDeptIds.Contains(pd.DepartmentId))
                .ToList();
            _context.ProjectDepartments.RemoveRange(toRemove);

            // Add new departments
            var toAdd = newDeptIds
                .Where(id => !existingDeptIds.Contains(id))
                .Select(id => new ProjectDepartment
                {
                    ProjectId = projectId,
                    DepartmentId = id,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });
            _context.ProjectDepartments.AddRange(toAdd);

            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return false;

            project.IsActive = false;
            project.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProjectDto>> GetProjectsByManagerAsync(int managerId)
        {
            return await GetProjectDashboardAsync(managerId);
        }

        // Helper class for stored procedure result
        private class ProjectDashboardResult
        {
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Priority { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string ProjectManager { get; set; } = string.Empty;
            public int DepartmentCount { get; set; }
            public int EmployeeCount { get; set; }
            public string? CurrentWeekStatus { get; set; }
            public decimal CurrentWeekRequiredHours { get; set; }
            public decimal CurrentWeekAssignedHours { get; set; }
        }
    }
}
