using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync(int userId);
        Task<List<ConflictSummaryDto>> GetConflictsAsync();
        Task<QuickStatsDto> GetQuickStatsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ResourcePlanProContext _context;
        private readonly IProjectService _projectService;
        private readonly IResourceService _resourceService;

        public DashboardService(
            ResourcePlanProContext context,
            IProjectService projectService,
            IResourceService resourceService)
        {
            _context = context;
            _projectService = projectService;
            _resourceService = resourceService;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            };

            // Get active projects based on user role
            List<ProjectDto> projects;
            if (user.Role == "Admin" || user.Role == "Viewer")
            {
                projects = await _projectService.GetProjectDashboardAsync();
            }
            else
            {
                projects = await _projectService.GetProjectsByManagerAsync(userId);
            }

            var quickStats = await GetQuickStatsAsync();
            var conflicts = await GetConflictsAsync();
            var timeline = await _resourceService.GetResourceTimelineAsync();

            return new DashboardDto
            {
                CurrentUser = userDto,
                ActiveProjects = projects,
                QuickStats = quickStats,
                Conflicts = conflicts.Take(5).ToList(),
                Timeline = timeline
            };
        }

        public async Task<List<ConflictSummaryDto>> GetConflictsAsync()
        {
            var conflicts = await _context.Database
                .SqlQueryRaw<ConflictResult>("EXEC sp_GetConflictSummary")
                .ToListAsync();

            return conflicts.Select(c => new ConflictSummaryDto
            {
                ConflictType = c.ConflictType,
                EntityId = c.EntityId,
                EntityName = c.EntityName,
                DepartmentName = c.DepartmentName ?? string.Empty,
                WeekStartDate = c.WeekStartDate,
                Variance = c.Variance,
                UtilizationPercentage = c.UtilizationPercentage,
                Priority = c.Priority,
                Description = c.Description,
                AffectedProjects = c.AffectedProjects
            }).ToList();
        }

        public async Task<QuickStatsDto> GetQuickStatsAsync()
        {
            var activeProjects = await _context.Projects
                .CountAsync(p => p.IsActive && p.Status == "Active");

            var totalEmployees = await _context.Employees
                .CountAsync(e => e.IsActive);

            var currentWeekStart = GetWeekStartDate(DateTime.Today);
            
            var utilization = await _context.Employees
                .Where(e => e.IsActive)
                .Select(e => new
                {
                    e.HoursPerWeek,
                    AssignedHours = e.Assignments
                        .Where(a => a.WeekStartDate == currentWeekStart)
                        .Sum(a => a.AssignedHours)
                })
                .ToListAsync();

            var avgUtilization = utilization.Any() 
                ? utilization.Average(u => u.HoursPerWeek > 0 ? (u.AssignedHours / u.HoursPerWeek * 100) : 0)
                : 0;

            var overallocated = utilization.Count(u => u.AssignedHours > u.HoursPerWeek);

            var understaffed = await _context.WeeklyLaborRequirements
                .Where(w => w.WeekStartDate >= currentWeekStart)
                .Select(w => new
                {
                    w.ProjectId,
                    w.DepartmentId,
                    w.WeekStartDate,
                    w.RequiredHours,
                    AssignedHours = _context.EmployeeAssignments
                        .Where(a => a.ProjectId == w.ProjectId && a.WeekStartDate == w.WeekStartDate)
                        .Join(_context.Employees,
                              a => a.EmployeeId,
                              e => e.EmployeeId,
                              (a, e) => new { a, e })
                        .Where(x => x.e.DepartmentId == w.DepartmentId)
                        .Sum(x => x.a.AssignedHours)
                })
                .Where(x => x.RequiredHours - x.AssignedHours > 10)
                .Select(x => x.ProjectId)
                .Distinct()
                .CountAsync();

            return new QuickStatsDto
            {
                ActiveProjects = activeProjects,
                TotalEmployees = totalEmployees,
                AverageUtilization = Math.Round(avgUtilization, 2),
                OverallocatedEmployees = overallocated,
                UnderstaffedProjects = understaffed
            };
        }

        private DateTime GetWeekStartDate(DateTime date)
        {
            var diff = date.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return date.AddDays(-diff).Date;
        }

        private class ConflictResult
        {
            public string ConflictType { get; set; } = string.Empty;
            public int EntityId { get; set; }
            public string EntityName { get; set; } = string.Empty;
            public string? DepartmentName { get; set; }
            public DateTime WeekStartDate { get; set; }
            public decimal Variance { get; set; }
            public decimal UtilizationPercentage { get; set; }
            public string Priority { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? AffectedProjects { get; set; }
        }
    }

    public interface IEmployeeService
    {
        Task<List<EmployeeDto>> GetAllEmployeesAsync(bool includeInactive = false);
        Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId);
        Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId);
        Task<EmployeeWorkloadSummary?> GetEmployeeWorkloadAsync(int employeeId, DateTime weekStartDate);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly ResourcePlanProContext _context;

        public EmployeeService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeDto>> GetAllEmployeesAsync(bool includeInactive = false)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var employees = await query.OrderBy(e => e.LastName).ToListAsync();

            return employees.Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.DepartmentName,
                JobTitle = e.JobTitle,
                HoursPerWeek = e.HoursPerWeek,
                Skills = e.Skills,
                IsActive = e.IsActive
            }).ToList();
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return null;

            return new EmployeeDto
            {
                EmployeeId = employee.EmployeeId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                DepartmentId = employee.DepartmentId,
                DepartmentName = employee.Department.DepartmentName,
                JobTitle = employee.JobTitle,
                HoursPerWeek = employee.HoursPerWeek,
                Skills = employee.Skills,
                IsActive = employee.IsActive
            };
        }

        public async Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.DepartmentId == departmentId && e.IsActive)
                .OrderBy(e => e.LastName)
                .ToListAsync();

            return employees.Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.DepartmentName,
                JobTitle = e.JobTitle,
                HoursPerWeek = e.HoursPerWeek,
                Skills = e.Skills,
                IsActive = e.IsActive
            }).ToList();
        }

        public async Task<EmployeeWorkloadSummary?> GetEmployeeWorkloadAsync(int employeeId, DateTime weekStartDate)
        {
            return await _context.EmployeeWorkloadSummaries
                .FromSqlRaw(@"
                    SELECT * FROM vw_EmployeeWorkloadSummary 
                    WHERE EmployeeId = {0} AND WeekStartDate = {1}",
                    employeeId, weekStartDate)
                .FirstOrDefaultAsync();
        }
    }
}
