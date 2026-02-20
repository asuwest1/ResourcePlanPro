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
    public interface IResourceService
    {
        Task<List<LaborRequirementDto>> GetLaborRequirementsAsync(int projectId, DateTime? weekStartDate = null);
        Task<WeeklyLaborRequirement> SaveLaborRequirementAsync(SaveLaborRequirementRequest request);
        Task<bool> BulkSaveLaborRequirementsAsync(BulkLaborRequirementRequest request);
        Task<List<EmployeeAvailabilityDto>> GetAvailableEmployeesAsync(int departmentId, DateTime weekStartDate, decimal minAvailableHours = 0);
        Task<List<EmployeeAssignmentDto>> GetEmployeeAssignmentsAsync(int projectId, DateTime? weekStartDate = null);
        Task<EmployeeAssignment> CreateAssignmentAsync(CreateAssignmentRequest request);
        Task<EmployeeAssignment> UpdateAssignmentAsync(int assignmentId, CreateAssignmentRequest request);
        Task<bool> DeleteAssignmentAsync(int assignmentId);
        Task<List<TimelineDto>> GetResourceTimelineAsync(DateTime? startDate = null, int weekCount = 12);
        Task<int> BulkCreateAssignmentsAsync(BulkEmployeeAssignmentRequest request);
        Task<List<CalendarEventDto>> GetCalendarEventsAsync(DateTime startDate, DateTime endDate, int? departmentId = null, int? employeeId = null);
    }

    public class ResourceService : IResourceService
    {
        private readonly ResourcePlanProContext _context;

        public ResourceService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<List<LaborRequirementDto>> GetLaborRequirementsAsync(int projectId, DateTime? weekStartDate = null)
        {
            var query = _context.WeeklyLaborRequirements
                .Include(w => w.Department)
                .Where(w => w.ProjectId == projectId);

            if (weekStartDate.HasValue)
            {
                query = query.Where(w => w.WeekStartDate == weekStartDate.Value);
            }

            var requirements = await query.ToListAsync();

            // Batch-load assigned hours to avoid N+1 query
            var assignedHoursLookup = await _context.EmployeeAssignments
                .Join(_context.Employees,
                      a => a.EmployeeId,
                      e => e.EmployeeId,
                      (a, e) => new { Assignment = a, Employee = e })
                .Where(x => x.Assignment.ProjectId == projectId)
                .GroupBy(x => new { x.Assignment.ProjectId, x.Assignment.WeekStartDate, x.Employee.DepartmentId })
                .Select(g => new
                {
                    g.Key.ProjectId,
                    g.Key.WeekStartDate,
                    g.Key.DepartmentId,
                    TotalHours = g.Sum(x => x.Assignment.AssignedHours)
                })
                .ToListAsync();

            var result = new List<LaborRequirementDto>();
            foreach (var req in requirements)
            {
                var assignedHours = assignedHoursLookup
                    .Where(a => a.ProjectId == req.ProjectId &&
                                a.WeekStartDate == req.WeekStartDate &&
                                a.DepartmentId == req.DepartmentId)
                    .Sum(a => a.TotalHours);

                result.Add(new LaborRequirementDto
                {
                    RequirementId = req.RequirementId,
                    ProjectId = req.ProjectId,
                    DepartmentId = req.DepartmentId,
                    DepartmentName = req.Department.DepartmentName,
                    WeekStartDate = req.WeekStartDate,
                    RequiredHours = req.RequiredHours,
                    AssignedHours = assignedHours,
                    RemainingHours = req.RequiredHours - assignedHours,
                    StaffingPercentage = req.RequiredHours > 0 ? (assignedHours / req.RequiredHours * 100) : 0
                });
            }

            return result;
        }

        public async Task<WeeklyLaborRequirement> SaveLaborRequirementAsync(SaveLaborRequirementRequest request)
        {
            var existing = await _context.WeeklyLaborRequirements
                .FirstOrDefaultAsync(w => 
                    w.ProjectId == request.ProjectId && 
                    w.DepartmentId == request.DepartmentId && 
                    w.WeekStartDate == request.WeekStartDate);

            if (existing != null)
            {
                existing.RequiredHours = request.RequiredHours;
                existing.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                existing = new WeeklyLaborRequirement
                {
                    ProjectId = request.ProjectId,
                    DepartmentId = request.DepartmentId,
                    WeekStartDate = request.WeekStartDate,
                    RequiredHours = request.RequiredHours,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.WeeklyLaborRequirements.Add(existing);
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> BulkSaveLaborRequirementsAsync(BulkLaborRequirementRequest request)
        {
            foreach (var item in request.Requirements)
            {
                var req = new SaveLaborRequirementRequest
                {
                    ProjectId = request.ProjectId,
                    DepartmentId = item.DepartmentId,
                    WeekStartDate = item.WeekStartDate,
                    RequiredHours = item.RequiredHours
                };
                await SaveLaborRequirementAsync(req);
            }
            return true;
        }

        public async Task<List<EmployeeAvailabilityDto>> GetAvailableEmployeesAsync(
            int departmentId, 
            DateTime weekStartDate, 
            decimal minAvailableHours = 0)
        {
            var deptIdParam = new SqlParameter("@DepartmentId", departmentId);
            var weekStartParam = new SqlParameter("@WeekStartDate", weekStartDate);
            var minHoursParam = new SqlParameter("@MinAvailableHours", minAvailableHours);

            var employees = await _context.Database
                .SqlQueryRaw<AvailableEmployeeResult>(
                    "EXEC sp_GetAvailableEmployees @DepartmentId, @WeekStartDate, @MinAvailableHours",
                    deptIdParam, weekStartParam, minHoursParam)
                .ToListAsync();

            return employees.Select(e => new EmployeeAvailabilityDto
            {
                EmployeeId = e.EmployeeId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                JobTitle = e.JobTitle,
                Skills = e.Skills,
                HoursPerWeek = e.HoursPerWeek,
                CurrentlyAssigned = e.CurrentlyAssigned,
                AvailableHours = e.AvailableHours,
                CurrentUtilization = e.CurrentUtilization,
                ActiveProjects = e.ActiveProjects
            }).ToList();
        }

        public async Task<List<EmployeeAssignmentDto>> GetEmployeeAssignmentsAsync(int projectId, DateTime? weekStartDate = null)
        {
            var query = _context.EmployeeAssignments
                .Include(a => a.Project)
                .Include(a => a.Employee)
                .Where(a => a.ProjectId == projectId);

            if (weekStartDate.HasValue)
            {
                query = query.Where(a => a.WeekStartDate == weekStartDate.Value);
            }

            var assignments = await query.ToListAsync();

            return assignments.Select(a => new EmployeeAssignmentDto
            {
                AssignmentId = a.AssignmentId,
                ProjectId = a.ProjectId,
                ProjectName = a.Project.ProjectName,
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee.FullName,
                WeekStartDate = a.WeekStartDate,
                AssignedHours = a.AssignedHours,
                Notes = a.Notes
            }).ToList();
        }

        public async Task<EmployeeAssignment> CreateAssignmentAsync(CreateAssignmentRequest request)
        {
            // Check if assignment already exists
            var existing = await _context.EmployeeAssignments
                .FirstOrDefaultAsync(a => 
                    a.ProjectId == request.ProjectId && 
                    a.EmployeeId == request.EmployeeId && 
                    a.WeekStartDate == request.WeekStartDate);

            if (existing != null)
            {
                throw new InvalidOperationException("Assignment already exists for this employee, project, and week");
            }

            var assignment = new EmployeeAssignment
            {
                ProjectId = request.ProjectId,
                EmployeeId = request.EmployeeId,
                WeekStartDate = request.WeekStartDate,
                AssignedHours = request.AssignedHours,
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.EmployeeAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            return assignment;
        }

        public async Task<EmployeeAssignment> UpdateAssignmentAsync(int assignmentId, CreateAssignmentRequest request)
        {
            var assignment = await _context.EmployeeAssignments.FindAsync(assignmentId);
            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with ID {assignmentId} not found");

            assignment.AssignedHours = request.AssignedHours;
            assignment.Notes = request.Notes;
            assignment.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return assignment;
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            var assignment = await _context.EmployeeAssignments.FindAsync(assignmentId);
            if (assignment == null)
                return false;

            _context.EmployeeAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TimelineDto>> GetResourceTimelineAsync(DateTime? startDate = null, int weekCount = 12)
        {
            var startDateParam = new SqlParameter("@StartDate", 
                startDate.HasValue ? (object)startDate.Value : DBNull.Value);
            var weekCountParam = new SqlParameter("@WeekCount", weekCount);

            var timeline = await _context.Database
                .SqlQueryRaw<TimelineResult>(
                    "EXEC sp_GetResourceTimeline @StartDate, @WeekCount",
                    startDateParam, weekCountParam)
                .ToListAsync();

            return timeline.Select(t => new TimelineDto
            {
                DepartmentId = t.DepartmentId,
                DepartmentName = t.DepartmentName,
                WeekNumber = t.WeekNumber,
                WeekStart = t.WeekStart,
                TotalCapacity = t.TotalCapacity,
                TotalAssignedHours = t.TotalAssignedHours,
                UtilizationPercentage = t.UtilizationPercentage,
                LoadLevel = t.LoadLevel
            }).ToList();
        }

        public async Task<int> BulkCreateAssignmentsAsync(BulkEmployeeAssignmentRequest request)
        {
            int created = 0;

            foreach (var item in request.Assignments)
            {
                var existing = await _context.EmployeeAssignments
                    .FirstOrDefaultAsync(a =>
                        a.ProjectId == request.ProjectId &&
                        a.EmployeeId == item.EmployeeId &&
                        a.WeekStartDate == request.WeekStartDate);

                if (existing != null)
                {
                    // Update existing assignment
                    existing.AssignedHours = item.AssignedHours;
                    existing.Notes = item.Notes;
                    existing.ModifiedDate = DateTime.UtcNow;
                }
                else
                {
                    _context.EmployeeAssignments.Add(new EmployeeAssignment
                    {
                        ProjectId = request.ProjectId,
                        EmployeeId = item.EmployeeId,
                        WeekStartDate = request.WeekStartDate,
                        AssignedHours = item.AssignedHours,
                        Notes = item.Notes,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                    created++;
                }
            }

            await _context.SaveChangesAsync();
            return created;
        }

        public async Task<List<CalendarEventDto>> GetCalendarEventsAsync(
            DateTime startDate, DateTime endDate, int? departmentId = null, int? employeeId = null)
        {
            var query = _context.EmployeeAssignments
                .Include(a => a.Project)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Where(a => a.WeekStartDate >= startDate && a.WeekStartDate <= endDate);

            if (departmentId.HasValue)
                query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            var assignments = await query.ToListAsync();

            // Group by employee and week to calculate totals
            var employeeWeekTotals = assignments
                .GroupBy(a => new { a.EmployeeId, a.WeekStartDate })
                .ToDictionary(
                    g => (g.Key.EmployeeId, g.Key.WeekStartDate),
                    g => g.Sum(a => a.AssignedHours));

            return assignments.Select(a =>
            {
                var totalWeekHours = employeeWeekTotals.GetValueOrDefault(
                    (a.EmployeeId, a.WeekStartDate), a.AssignedHours);

                return new CalendarEventDto
                {
                    AssignmentId = a.AssignmentId,
                    ProjectId = a.ProjectId,
                    ProjectName = a.Project.ProjectName,
                    Priority = a.Project.Priority,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    DepartmentName = a.Employee.Department.DepartmentName,
                    WeekStartDate = a.WeekStartDate,
                    AssignedHours = a.AssignedHours,
                    TotalWeekHours = totalWeekHours,
                    Capacity = a.Employee.HoursPerWeek,
                    UtilizationPercentage = a.Employee.HoursPerWeek > 0
                        ? Math.Round(totalWeekHours / a.Employee.HoursPerWeek * 100, 1)
                        : 0
                };
            })
            .OrderBy(e => e.WeekStartDate)
            .ThenBy(e => e.EmployeeName)
            .ToList();
        }

        // Helper classes for stored procedure results
        private class AvailableEmployeeResult
        {
            public int EmployeeId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string JobTitle { get; set; } = string.Empty;
            public string? Skills { get; set; }
            public decimal HoursPerWeek { get; set; }
            public decimal CurrentlyAssigned { get; set; }
            public decimal AvailableHours { get; set; }
            public decimal CurrentUtilization { get; set; }
            public int ActiveProjects { get; set; }
        }

        private class TimelineResult
        {
            public int DepartmentId { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public int WeekNumber { get; set; }
            public DateTime WeekStart { get; set; }
            public decimal TotalCapacity { get; set; }
            public decimal TotalAssignedHours { get; set; }
            public decimal UtilizationPercentage { get; set; }
            public string LoadLevel { get; set; } = string.Empty;
        }
    }
}
