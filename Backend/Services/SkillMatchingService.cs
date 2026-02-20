using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public interface ISkillMatchingService
    {
        Task<List<SkillMatchResultDto>> FindMatchingEmployeesAsync(SkillMatchRequest request);
        Task<List<string>> GetAllSkillsAsync();
        Task<List<string>> GetProjectRequiredSkillsAsync(int projectId);
    }

    public class SkillMatchingService : ISkillMatchingService
    {
        private readonly ResourcePlanProContext _context;

        public SkillMatchingService(ResourcePlanProContext context)
        {
            _context = context;
        }

        public async Task<List<SkillMatchResultDto>> FindMatchingEmployeesAsync(SkillMatchRequest request)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Assignments.Where(a => a.WeekStartDate == request.WeekStartDate))
                .Where(e => e.IsActive);

            if (request.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
            }

            var employees = await query.ToListAsync();

            // Get already-assigned employees for this project/week to exclude
            var existingAssignments = await _context.EmployeeAssignments
                .Where(a => a.ProjectId == request.ProjectId && a.WeekStartDate == request.WeekStartDate)
                .Select(a => a.EmployeeId)
                .ToListAsync();

            var results = new List<SkillMatchResultDto>();

            foreach (var employee in employees)
            {
                // Skip already assigned employees
                if (existingAssignments.Contains(employee.EmployeeId))
                    continue;

                var employeeSkills = ParseSkills(employee.Skills);

                // Calculate availability
                var weekAssignments = employee.Assignments
                    .Where(a => a.WeekStartDate == request.WeekStartDate)
                    .Sum(a => a.AssignedHours);
                var availableHours = employee.HoursPerWeek - weekAssignments;

                if (availableHours < request.MinAvailableHours)
                    continue;

                // Calculate skill match
                var matchedSkills = new List<string>();
                int matchScore = 0;

                if (request.RequiredSkills.Any())
                {
                    foreach (var reqSkill in request.RequiredSkills)
                    {
                        var match = employeeSkills.FirstOrDefault(es =>
                            es.Contains(reqSkill, StringComparison.OrdinalIgnoreCase) ||
                            reqSkill.Contains(es, StringComparison.OrdinalIgnoreCase));

                        if (match != null)
                        {
                            matchedSkills.Add(match);
                            matchScore++;
                        }
                    }
                }
                else
                {
                    // If no specific skills requested, all employees match
                    matchScore = employeeSkills.Count;
                    matchedSkills = employeeSkills;
                }

                var matchPercentage = request.RequiredSkills.Any()
                    ? (matchScore > 0 ? Math.Round((decimal)matchScore / request.RequiredSkills.Count * 100, 1) : 0)
                    : 100;

                var utilization = employee.HoursPerWeek > 0
                    ? Math.Round(weekAssignments / employee.HoursPerWeek * 100, 1)
                    : 0;

                results.Add(new SkillMatchResultDto
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    DepartmentName = employee.Department.DepartmentName,
                    JobTitle = employee.JobTitle,
                    Skills = employeeSkills,
                    MatchedSkills = matchedSkills,
                    MatchScore = matchScore,
                    MatchPercentage = matchPercentage,
                    AvailableHours = availableHours,
                    CurrentUtilization = utilization
                });
            }

            // Sort: best match first, then by available hours
            return results
                .OrderByDescending(r => r.MatchPercentage)
                .ThenByDescending(r => r.MatchScore)
                .ThenByDescending(r => r.AvailableHours)
                .ToList();
        }

        public async Task<List<string>> GetAllSkillsAsync()
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive && e.Skills != null)
                .Select(e => e.Skills!)
                .ToListAsync();

            var allSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var skillString in employees)
            {
                foreach (var skill in ParseSkills(skillString))
                {
                    allSkills.Add(skill);
                }
            }

            return allSkills.OrderBy(s => s).ToList();
        }

        public async Task<List<string>> GetProjectRequiredSkillsAsync(int projectId)
        {
            // Infer skills from departments assigned to the project
            var departmentIds = await _context.ProjectDepartments
                .Where(pd => pd.ProjectId == projectId && pd.IsActive)
                .Select(pd => pd.DepartmentId)
                .ToListAsync();

            var employees = await _context.Employees
                .Where(e => e.IsActive && departmentIds.Contains(e.DepartmentId) && e.Skills != null)
                .Select(e => e.Skills!)
                .ToListAsync();

            var allSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var skillString in employees)
            {
                foreach (var skill in ParseSkills(skillString))
                {
                    allSkills.Add(skill);
                }
            }

            return allSkills.OrderBy(s => s).ToList();
        }

        private static List<string> ParseSkills(string? skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
                return new List<string>();

            return skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
    }
}
