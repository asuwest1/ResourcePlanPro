using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public interface IReportingService
    {
        Task<ReportDataDto> GetReportDataAsync(DateTime? startDate = null, int weekCount = 12);
    }

    public class ReportingService : IReportingService
    {
        private readonly ResourcePlanProContext _context;

        public ReportingService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<ReportDataDto> GetReportDataAsync(DateTime? startDate = null, int weekCount = 12)
        {
            var weekStart = startDate ?? GetWeekStartDate(DateTime.Today);

            var report = new ReportDataDto
            {
                DepartmentUtilization = await GetDepartmentUtilizationAsync(weekStart),
                ProjectStatusDistribution = await GetProjectStatusDistributionAsync(),
                WeeklyTrends = await GetWeeklyTrendsAsync(weekStart, weekCount),
                TopUtilizedEmployees = await GetTopUtilizedEmployeesAsync(weekStart),
                SkillDemand = await GetSkillDemandAsync()
            };

            return report;
        }

        private async Task<List<DepartmentUtilizationChartData>> GetDepartmentUtilizationAsync(DateTime weekStart)
        {
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .Include(d => d.Employees.Where(e => e.IsActive))
                .ToListAsync();

            var result = new List<DepartmentUtilizationChartData>();

            foreach (var dept in departments)
            {
                var capacity = dept.Employees.Sum(e => e.HoursPerWeek);
                var employeeIds = dept.Employees.Select(e => e.EmployeeId).ToList();

                var assigned = await _context.EmployeeAssignments
                    .Where(a => employeeIds.Contains(a.EmployeeId)
                                && a.WeekStartDate == weekStart)
                    .SumAsync(a => a.AssignedHours);

                result.Add(new DepartmentUtilizationChartData
                {
                    DepartmentName = dept.DepartmentName,
                    TotalCapacity = capacity,
                    AssignedHours = assigned,
                    UtilizationPercentage = capacity > 0 ? Math.Round(assigned / capacity * 100, 2) : 0,
                    EmployeeCount = dept.Employees.Count
                });
            }

            return result.OrderByDescending(d => d.UtilizationPercentage).ToList();
        }

        private async Task<List<ProjectStatusChartData>> GetProjectStatusDistributionAsync()
        {
            return await _context.Projects
                .Where(p => p.IsActive)
                .GroupBy(p => p.Status)
                .Select(g => new ProjectStatusChartData
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();
        }

        private async Task<List<WeeklyTrendData>> GetWeeklyTrendsAsync(DateTime startDate, int weekCount)
        {
            var result = new List<WeeklyTrendData>();

            var allEmployees = await _context.Employees
                .Where(e => e.IsActive)
                .ToListAsync();
            var totalCapacity = allEmployees.Sum(e => e.HoursPerWeek);

            for (int i = 0; i < weekCount; i++)
            {
                var weekStart = startDate.AddDays(i * 7);

                var totalAssigned = await _context.EmployeeAssignments
                    .Where(a => a.WeekStartDate == weekStart)
                    .SumAsync(a => a.AssignedHours);

                var conflictCount = await _context.EmployeeAssignments
                    .Where(a => a.WeekStartDate == weekStart)
                    .GroupBy(a => a.EmployeeId)
                    .Select(g => new
                    {
                        EmployeeId = g.Key,
                        TotalHours = g.Sum(x => x.AssignedHours)
                    })
                    .Join(_context.Employees,
                        a => a.EmployeeId,
                        e => e.EmployeeId,
                        (a, e) => new { a.TotalHours, e.HoursPerWeek })
                    .Where(x => x.TotalHours > x.HoursPerWeek)
                    .CountAsync();

                result.Add(new WeeklyTrendData
                {
                    WeekStart = weekStart,
                    TotalCapacity = totalCapacity,
                    TotalAssigned = totalAssigned,
                    UtilizationPercentage = totalCapacity > 0
                        ? Math.Round(totalAssigned / totalCapacity * 100, 2) : 0,
                    ConflictCount = conflictCount
                });
            }

            return result;
        }

        private async Task<List<EmployeeUtilizationData>> GetTopUtilizedEmployeesAsync(DateTime weekStart)
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .Include(e => e.Department)
                .Include(e => e.Assignments.Where(a => a.WeekStartDate == weekStart))
                .OrderByDescending(e => e.Assignments.Where(a => a.WeekStartDate == weekStart).Sum(a => a.AssignedHours))
                .Take(15)
                .ToListAsync();

            return employees.Select(e =>
            {
                var assigned = e.Assignments.Sum(a => a.AssignedHours);
                return new EmployeeUtilizationData
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = $"{e.FirstName} {e.LastName}",
                    DepartmentName = e.Department.DepartmentName,
                    HoursPerWeek = e.HoursPerWeek,
                    AssignedHours = assigned,
                    UtilizationPercentage = e.HoursPerWeek > 0
                        ? Math.Round(assigned / e.HoursPerWeek * 100, 2) : 0
                };
            }).ToList();
        }

        private async Task<List<SkillDemandData>> GetSkillDemandAsync()
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive && e.Skills != null)
                .ToListAsync();

            var skillCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var emp in employees)
            {
                if (string.IsNullOrEmpty(emp.Skills)) continue;
                var skills = emp.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim());
                foreach (var skill in skills)
                {
                    if (skillCounts.ContainsKey(skill))
                        skillCounts[skill]++;
                    else
                        skillCounts[skill] = 1;
                }
            }

            // Get active project count as a general demand indicator
            var activeProjectCount = await _context.Projects
                .CountAsync(p => p.IsActive && p.Status == "Active");

            return skillCounts
                .OrderByDescending(kv => kv.Value)
                .Take(15)
                .Select(kv => new SkillDemandData
                {
                    Skill = kv.Key,
                    EmployeesWithSkill = kv.Value,
                    ProjectsRequiring = activeProjectCount,
                    DemandRatio = kv.Value > 0 ? Math.Round((decimal)activeProjectCount / kv.Value, 2) : 0
                })
                .ToList();
        }

        private static DateTime GetWeekStartDate(DateTime date)
        {
            var diff = date.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return date.AddDays(-diff).Date;
        }
    }
}
