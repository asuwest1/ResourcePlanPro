using System;
using System.ComponentModel.DataAnnotations;

namespace ResourcePlanPro.API.Models.DTOs
{
    // Authentication DTOs
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
    }

    // Project DTOs
    public class ProjectDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectManagerId { get; set; }
        public string ProjectManagerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int DepartmentCount { get; set; }
        public int EmployeeCount { get; set; }
        public string CurrentWeekStatus { get; set; } = string.Empty;
        public decimal CurrentWeekRequiredHours { get; set; }
        public decimal CurrentWeekAssignedHours { get; set; }
    }

    public class CreateProjectRequest
    {
        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int ProjectManagerId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Priority { get; set; } = "Medium";

        public List<int> DepartmentIds { get; set; } = new List<int>();
    }

    // Employee DTOs
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public decimal HoursPerWeek { get; set; }
        public string? Skills { get; set; }
        public bool IsActive { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    public class EmployeeAvailabilityDto
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
        public string FullName => $"{FirstName} {LastName}";
    }

    // Labor Requirements DTOs
    public class LaborRequirementDto
    {
        public int RequirementId { get; set; }
        public int ProjectId { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public decimal RequiredHours { get; set; }
        public decimal AssignedHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal StaffingPercentage { get; set; }
    }

    public class SaveLaborRequirementRequest
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [Range(0, 9999.99)]
        public decimal RequiredHours { get; set; }
    }

    public class BulkLaborRequirementRequest
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public List<LaborRequirementItem> Requirements { get; set; } = new List<LaborRequirementItem>();
    }

    public class LaborRequirementItem
    {
        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [Range(0, 9999.99)]
        public decimal RequiredHours { get; set; }
    }

    // Employee Assignment DTOs
    public class EmployeeAssignmentDto
    {
        public int AssignmentId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public decimal AssignedHours { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAssignmentRequest
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [Range(0, 168)]
        public decimal AssignedHours { get; set; }

        public string? Notes { get; set; }
    }

    // Dashboard DTOs
    public class DashboardDto
    {
        public UserDto CurrentUser { get; set; } = null!;
        public List<ProjectDto> ActiveProjects { get; set; } = new List<ProjectDto>();
        public QuickStatsDto QuickStats { get; set; } = null!;
        public List<ConflictSummaryDto> Conflicts { get; set; } = new List<ConflictSummaryDto>();
        public List<TimelineDto> Timeline { get; set; } = new List<TimelineDto>();
    }

    public class QuickStatsDto
    {
        public int ActiveProjects { get; set; }
        public int TotalEmployees { get; set; }
        public decimal AverageUtilization { get; set; }
        public int OverallocatedEmployees { get; set; }
        public int UnderstaffedProjects { get; set; }
    }

    public class ConflictSummaryDto
    {
        public string ConflictType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public decimal Variance { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AffectedProjects { get; set; }
    }

    public class TimelineDto
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

    // Department DTOs
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ManagerUserId { get; set; }
        public string? ManagerName { get; set; }
        public int EmployeeCount { get; set; }
        public bool IsActive { get; set; }
    }

    // Generic Response
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // v1.1.0 DTOs

    // Email Notification DTOs
    public class NotificationSettingsDto
    {
        public bool NotifyOnConflicts { get; set; } = true;
        public bool NotifyOnOverallocation { get; set; } = true;
        public bool NotifyOnUnderstaffing { get; set; } = true;
        public List<string> RecipientEmails { get; set; } = new List<string>();
    }

    public class NotificationLogDto
    {
        public int NotificationId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? SentDate { get; set; }
    }

    public class SendNotificationRequest
    {
        public List<string> RecipientEmails { get; set; } = new List<string>();
        public bool IncludeOverallocations { get; set; } = true;
        public bool IncludeUnderstaffing { get; set; } = true;
    }

    // Project Template DTOs
    public class ProjectTemplateDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public int DurationWeeks { get; set; }
        public List<int> DepartmentIds { get; set; } = new List<int>();
        public List<TemplateHourEntry> DefaultHours { get; set; } = new List<TemplateHourEntry>();
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class CreateTemplateRequest
    {
        [Required]
        [StringLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string Priority { get; set; } = "Medium";

        [Range(1, 52)]
        public int DurationWeeks { get; set; } = 12;

        public List<int> DepartmentIds { get; set; } = new List<int>();
        public List<TemplateHourEntry> DefaultHours { get; set; } = new List<TemplateHourEntry>();
    }

    public class TemplateHourEntry
    {
        public int DepartmentId { get; set; }
        public int WeekNumber { get; set; }
        public decimal Hours { get; set; }
    }

    public class CreateProjectFromTemplateRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int ProjectManagerId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
    }

    // Bulk Assignment DTOs
    public class BulkEmployeeAssignmentRequest
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        public List<BulkAssignmentItem> Assignments { get; set; } = new List<BulkAssignmentItem>();
    }

    public class BulkAssignmentItem
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [Range(0, 168)]
        public decimal AssignedHours { get; set; }

        public string? Notes { get; set; }
    }

    // Calendar View DTOs
    public class CalendarEventDto
    {
        public int AssignmentId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public decimal AssignedHours { get; set; }
        public decimal TotalWeekHours { get; set; }
        public decimal Capacity { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    // Skills-Based Matching DTOs
    public class SkillMatchRequest
    {
        [Required]
        public int ProjectId { get; set; }

        public int? DepartmentId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        public List<string> RequiredSkills { get; set; } = new List<string>();

        [Range(0, 168)]
        public decimal MinAvailableHours { get; set; } = 0;
    }

    public class SkillMatchResultDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new List<string>();
        public List<string> MatchedSkills { get; set; } = new List<string>();
        public int MatchScore { get; set; }
        public decimal MatchPercentage { get; set; }
        public decimal AvailableHours { get; set; }
        public decimal CurrentUtilization { get; set; }
    }

    // Advanced Reporting DTOs
    public class ReportDataDto
    {
        public List<DepartmentUtilizationChartData> DepartmentUtilization { get; set; } = new List<DepartmentUtilizationChartData>();
        public List<ProjectStatusChartData> ProjectStatusDistribution { get; set; } = new List<ProjectStatusChartData>();
        public List<WeeklyTrendData> WeeklyTrends { get; set; } = new List<WeeklyTrendData>();
        public List<EmployeeUtilizationData> TopUtilizedEmployees { get; set; } = new List<EmployeeUtilizationData>();
        public List<SkillDemandData> SkillDemand { get; set; } = new List<SkillDemandData>();
    }

    public class DepartmentUtilizationChartData
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalCapacity { get; set; }
        public decimal AssignedHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class ProjectStatusChartData
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class WeeklyTrendData
    {
        public DateTime WeekStart { get; set; }
        public decimal TotalCapacity { get; set; }
        public decimal TotalAssigned { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int ConflictCount { get; set; }
    }

    public class EmployeeUtilizationData
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public decimal HoursPerWeek { get; set; }
        public decimal AssignedHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    public class SkillDemandData
    {
        public string Skill { get; set; } = string.Empty;
        public int EmployeesWithSkill { get; set; }
        public int ProjectsRequiring { get; set; }
        public decimal DemandRatio { get; set; }
    }
}
