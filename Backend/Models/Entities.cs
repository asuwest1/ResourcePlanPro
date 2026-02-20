using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcePlanPro.API.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "Viewer";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        // Navigation properties
        public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    }

    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ManagerUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ManagerUserId")]
        public virtual User? Manager { get; set; }
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<ProjectDepartment> ProjectDepartments { get; set; } = new List<ProjectDepartment>();
    }

    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursPerWeek { get; set; } = 40.00m;

        [StringLength(500)]
        public string? Skills { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime HireDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;
        public virtual ICollection<EmployeeAssignment> Assignments { get; set; } = new List<EmployeeAssignment>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }

    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int ProjectManagerId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProjectManagerId")]
        public virtual User ProjectManager { get; set; } = null!;
        public virtual ICollection<ProjectDepartment> ProjectDepartments { get; set; } = new List<ProjectDepartment>();
        public virtual ICollection<WeeklyLaborRequirement> LaborRequirements { get; set; } = new List<WeeklyLaborRequirement>();
        public virtual ICollection<EmployeeAssignment> EmployeeAssignments { get; set; } = new List<EmployeeAssignment>();
    }

    public class ProjectDepartment
    {
        [Key]
        public int ProjectDepartmentId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;
    }

    public class WeeklyLaborRequirement
    {
        [Key]
        public int RequirementId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime WeekStartDate { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal RequiredHours { get; set; } = 0;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;
    }

    public class EmployeeAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime WeekStartDate { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal AssignedHours { get; set; } = 0;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    // View Models for stored procedures
    public class EmployeeWorkloadSummary
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public decimal StandardHours { get; set; }
        public DateTime? WeekStartDate { get; set; }
        public decimal TotalAssignedHours { get; set; }
        public decimal AvailableHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int ActiveProjectCount { get; set; }
    }

    public class ProjectStaffingStatus
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string ProjectManager { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal RequiredHours { get; set; }
        public decimal AssignedHours { get; set; }
        public decimal HoursGap { get; set; }
        public decimal StaffingPercentage { get; set; }
        public string StaffingStatus { get; set; } = string.Empty;
    }

    public class DepartmentUtilization
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime? WeekStartDate { get; set; }
        public int TotalEmployees { get; set; }
        public decimal TotalCapacity { get; set; }
        public decimal TotalAssignedHours { get; set; }
        public decimal AvailableCapacity { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    public class ResourceConflict
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public decimal StandardHours { get; set; }
        public decimal TotalAssignedHours { get; set; }
        public decimal OverallocationHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int ProjectCount { get; set; }
        public string Projects { get; set; } = string.Empty;
    }

    // v1.1.0 Entities

    public class ProjectTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        public int DurationWeeks { get; set; } = 12;

        public string? DepartmentIds { get; set; }

        public string? DefaultHoursJson { get; set; }

        public int CreatedByUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;
    }

    public class NotificationLog
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public int? RelatedEntityId { get; set; }

        [StringLength(100)]
        public string? RelatedEntityType { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? SentDate { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}
