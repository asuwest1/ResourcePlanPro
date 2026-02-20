using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models;

namespace ResourcePlanPro.API.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportProjectsToCsvAsync(int? projectId = null);
        Task<byte[]> ExportEmployeesToCsvAsync(int? departmentId = null);
        Task<byte[]> ExportAssignmentsToCsvAsync(int? projectId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<byte[]> ExportConflictsToCsvAsync();
        Task<byte[]> ExportResourceTimelineToCsvAsync(DateTime? startDate = null, int weekCount = 12);
    }

    public class ExportService : IExportService
    {
        private readonly ResourcePlanProContext _context;

        public ExportService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportProjectsToCsvAsync(int? projectId = null)
        {
            var query = _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.ProjectDepartments)
                    .ThenInclude(pd => pd.Department)
                .Where(p => p.IsActive);

            if (projectId.HasValue)
                query = query.Where(p => p.ProjectId == projectId.Value);

            var projects = await query.OrderBy(p => p.ProjectName).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Project ID,Project Name,Description,Project Manager,Start Date,End Date,Priority,Status,Departments");

            foreach (var p in projects)
            {
                var managerName = p.ProjectManager != null
                    ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                    : "";
                var departments = string.Join("; ", p.ProjectDepartments.Select(pd => pd.Department.DepartmentName));

                sb.AppendLine($"{p.ProjectId},{EscapeCsv(p.ProjectName)},{EscapeCsv(p.Description ?? "")},{EscapeCsv(managerName)},{p.StartDate:yyyy-MM-dd},{p.EndDate:yyyy-MM-dd},{p.Priority},{p.Status},{EscapeCsv(departments)}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportEmployeesToCsvAsync(int? departmentId = null)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            var employees = await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Employee ID,First Name,Last Name,Email,Department,Job Title,Hours/Week,Skills,Active,Hire Date");

            foreach (var e in employees)
            {
                sb.AppendLine($"{e.EmployeeId},{EscapeCsv(e.FirstName)},{EscapeCsv(e.LastName)},{EscapeCsv(e.Email)},{EscapeCsv(e.Department.DepartmentName)},{EscapeCsv(e.JobTitle)},{e.HoursPerWeek},{EscapeCsv(e.Skills ?? "")},{(e.IsActive ? "Yes" : "No")},{e.HireDate:yyyy-MM-dd}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportAssignmentsToCsvAsync(int? projectId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.EmployeeAssignments
                .Include(a => a.Project)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(a => a.ProjectId == projectId.Value);
            if (startDate.HasValue)
                query = query.Where(a => a.WeekStartDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.WeekStartDate <= endDate.Value);

            var assignments = await query
                .OrderBy(a => a.WeekStartDate)
                .ThenBy(a => a.Project.ProjectName)
                .ThenBy(a => a.Employee.LastName)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Assignment ID,Project,Employee,Department,Week Start,Assigned Hours,Notes");

            foreach (var a in assignments)
            {
                var empName = $"{a.Employee.FirstName} {a.Employee.LastName}";
                sb.AppendLine($"{a.AssignmentId},{EscapeCsv(a.Project.ProjectName)},{EscapeCsv(empName)},{EscapeCsv(a.Employee.Department.DepartmentName)},{a.WeekStartDate:yyyy-MM-dd},{a.AssignedHours},{EscapeCsv(a.Notes ?? "")}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportConflictsToCsvAsync()
        {
            // Get overallocated employees
            var conflicts = await _context.EmployeeAssignments
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Include(a => a.Project)
                .Where(a => a.WeekStartDate >= DateTime.Today)
                .GroupBy(a => new { a.EmployeeId, a.WeekStartDate })
                .Select(g => new
                {
                    g.Key.EmployeeId,
                    EmployeeName = g.First().Employee.FirstName + " " + g.First().Employee.LastName,
                    DepartmentName = g.First().Employee.Department.DepartmentName,
                    g.Key.WeekStartDate,
                    TotalHours = g.Sum(x => x.AssignedHours),
                    Capacity = g.First().Employee.HoursPerWeek,
                    Projects = string.Join("; ", g.Select(x => x.Project.ProjectName).Distinct())
                })
                .Where(x => x.TotalHours > x.Capacity)
                .OrderBy(x => x.WeekStartDate)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Employee,Department,Week Start,Total Assigned Hours,Capacity,Over-allocation,Projects");

            foreach (var c in conflicts)
            {
                var overallocation = c.TotalHours - c.Capacity;
                sb.AppendLine($"{EscapeCsv(c.EmployeeName)},{EscapeCsv(c.DepartmentName)},{c.WeekStartDate:yyyy-MM-dd},{c.TotalHours},{c.Capacity},{overallocation},{EscapeCsv(c.Projects)}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportResourceTimelineToCsvAsync(DateTime? startDate = null, int weekCount = 12)
        {
            var start = startDate ?? GetWeekStartDate(DateTime.Today);
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .Include(d => d.Employees.Where(e => e.IsActive))
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            var sb = new StringBuilder();
            var header = "Department";
            for (int i = 0; i < weekCount; i++)
            {
                header += $",{start.AddDays(i * 7):MMM dd}";
            }
            sb.AppendLine(header);

            // Pre-fetch all assignment data in a single query to avoid N+1
            var endDate = start.AddDays(weekCount * 7);
            var assignmentsByDeptWeek = await _context.EmployeeAssignments
                .Where(a => a.WeekStartDate >= start && a.WeekStartDate < endDate)
                .GroupBy(a => new { a.Employee.DepartmentId, a.WeekStartDate })
                .Select(g => new { g.Key.DepartmentId, g.Key.WeekStartDate, Total = g.Sum(x => x.AssignedHours) })
                .ToListAsync();
            var assignmentLookup = assignmentsByDeptWeek.ToDictionary(
                x => (x.DepartmentId, x.WeekStartDate), x => x.Total);

            foreach (var dept in departments)
            {
                var capacity = dept.Employees.Sum(e => e.HoursPerWeek);
                var row = EscapeCsv(dept.DepartmentName);

                for (int i = 0; i < weekCount; i++)
                {
                    var weekStart = start.AddDays(i * 7);
                    var assigned = assignmentLookup.GetValueOrDefault((dept.DepartmentId, weekStart), 0);

                    var utilization = capacity > 0 ? Math.Round(assigned / capacity * 100, 1) : 0;
                    row += $",{utilization}%";
                }

                sb.AppendLine(row);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
                return field ?? string.Empty;

            // Prevent CSV formula injection: defang fields starting with formula-trigger characters
            if ("=+-@\t\r".Contains(field[0]))
                field = "'" + field;

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        private static DateTime GetWeekStartDate(DateTime date)
        {
            var diff = date.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return date.AddDays(-diff).Date;
        }
    }
}
