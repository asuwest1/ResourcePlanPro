using Microsoft.EntityFrameworkCore;
using ResourcePlanPro.API.Models;

namespace ResourcePlanPro.API.Data
{
    public class ResourcePlanProContext : DbContext
    {
        public ResourcePlanProContext(DbContextOptions<ResourcePlanProContext> options)
            : base(options)
        {
        }

        // DbSets for entities
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectDepartment> ProjectDepartments { get; set; } = null!;
        public DbSet<WeeklyLaborRequirement> WeeklyLaborRequirements { get; set; } = null!;
        public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; } = null!;

        // DbSets for views (keyless entities)
        public DbSet<EmployeeWorkloadSummary> EmployeeWorkloadSummaries { get; set; } = null!;
        public DbSet<ProjectStaffingStatus> ProjectStaffingStatuses { get; set; } = null!;
        public DbSet<DepartmentUtilization> DepartmentUtilizations { get; set; } = null!;
        public DbSet<ResourceConflict> ResourceConflicts { get; set; } = null!;

        // v1.1.0 entities
        public DbSet<ProjectTemplate> ProjectTemplates { get; set; } = null!;
        public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            
            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role)
                    .HasConversion<string>();
            });

            // Department configurations
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(e => e.DepartmentName).IsUnique();
                entity.HasOne(d => d.Manager)
                    .WithMany()
                    .HasForeignKey(d => d.ManagerUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Employee configurations
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Project configurations
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasOne(p => p.ProjectManager)
                    .WithMany(u => u.ManagedProjects)
                    .HasForeignKey(p => p.ProjectManagerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(p => p.Priority)
                    .HasConversion<string>();
                
                entity.Property(p => p.Status)
                    .HasConversion<string>();
            });

            // ProjectDepartment configurations
            modelBuilder.Entity<ProjectDepartment>(entity =>
            {
                entity.HasOne(pd => pd.Project)
                    .WithMany(p => p.ProjectDepartments)
                    .HasForeignKey(pd => pd.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pd => pd.Department)
                    .WithMany(d => d.ProjectDepartments)
                    .HasForeignKey(pd => pd.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(pd => new { pd.ProjectId, pd.DepartmentId }).IsUnique();
            });

            // WeeklyLaborRequirement configurations
            modelBuilder.Entity<WeeklyLaborRequirement>(entity =>
            {
                entity.HasOne(wlr => wlr.Project)
                    .WithMany(p => p.LaborRequirements)
                    .HasForeignKey(wlr => wlr.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(wlr => wlr.Department)
                    .WithMany()
                    .HasForeignKey(wlr => wlr.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(wlr => new { wlr.ProjectId, wlr.DepartmentId, wlr.WeekStartDate })
                    .IsUnique();
            });

            // EmployeeAssignment configurations
            modelBuilder.Entity<EmployeeAssignment>(entity =>
            {
                entity.HasOne(ea => ea.Project)
                    .WithMany(p => p.EmployeeAssignments)
                    .HasForeignKey(ea => ea.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ea => ea.Employee)
                    .WithMany(e => e.Assignments)
                    .HasForeignKey(ea => ea.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ea => new { ea.ProjectId, ea.EmployeeId, ea.WeekStartDate })
                    .IsUnique();
            });

            // Configure keyless entities for views
            modelBuilder.Entity<EmployeeWorkloadSummary>().HasNoKey().ToView(null);
            modelBuilder.Entity<ProjectStaffingStatus>().HasNoKey().ToView(null);
            modelBuilder.Entity<DepartmentUtilization>().HasNoKey().ToView(null);
            modelBuilder.Entity<ResourceConflict>().HasNoKey().ToView(null);

            // v1.1.0 entity configurations
            modelBuilder.Entity<ProjectTemplate>(entity =>
            {
                entity.HasOne(pt => pt.CreatedBy)
                    .WithMany()
                    .HasForeignKey(pt => pt.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NotificationLog>(entity =>
            {
                entity.HasIndex(n => n.CreatedDate);
                entity.HasIndex(n => n.Status);
            });
        }
    }
}
