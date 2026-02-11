-- ResourcePlanPro Database Creation Script
-- SQL Server 2019
-- ============================================

USE master;
GO

-- Drop database if exists (for development)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ResourcePlanPro')
BEGIN
    ALTER DATABASE ResourcePlanPro SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ResourcePlanPro;
END
GO

-- Create database
CREATE DATABASE ResourcePlanPro
ON PRIMARY 
(
    NAME = ResourcePlanPro_Data,
    FILENAME = 'C:\SQLData\ResourcePlanPro_Data.mdf',
    SIZE = 100MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10MB
)
LOG ON
(
    NAME = ResourcePlanPro_Log,
    FILENAME = 'C:\SQLData\ResourcePlanPro_Log.ldf',
    SIZE = 50MB,
    MAXSIZE = 1GB,
    FILEGROWTH = 10MB
);
GO

USE ResourcePlanPro;
GO

-- ============================================
-- Users Table
-- ============================================
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Admin', 'ProjectManager', 'DepartmentManager', 'Viewer')),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginDate DATETIME2 NULL,
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%@%.%')
);
GO

CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
GO

-- ============================================
-- Departments Table
-- ============================================
CREATE TABLE Departments (
    DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    ManagerUserId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Departments_ManagerUserId FOREIGN KEY (ManagerUserId) 
        REFERENCES Users(UserId)
);
GO

CREATE INDEX IX_Departments_DepartmentName ON Departments(DepartmentName);
GO

-- ============================================
-- Employees Table
-- ============================================
CREATE TABLE Employees (
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    DepartmentId INT NOT NULL,
    JobTitle NVARCHAR(100) NOT NULL,
    HoursPerWeek DECIMAL(5,2) NOT NULL DEFAULT 40.00,
    Skills NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    HireDate DATE NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Employees_DepartmentId FOREIGN KEY (DepartmentId) 
        REFERENCES Departments(DepartmentId),
    CONSTRAINT CK_Employees_HoursPerWeek CHECK (HoursPerWeek >= 0 AND HoursPerWeek <= 168),
    CONSTRAINT CK_Employees_Email CHECK (Email LIKE '%@%.%')
);
GO

CREATE INDEX IX_Employees_DepartmentId ON Employees(DepartmentId);
CREATE INDEX IX_Employees_Email ON Employees(Email);
CREATE INDEX IX_Employees_LastName ON Employees(LastName);
GO

-- ============================================
-- Projects Table
-- ============================================
CREATE TABLE Projects (
    ProjectId INT IDENTITY(1,1) PRIMARY KEY,
    ProjectName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    ProjectManagerId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Priority NVARCHAR(20) NOT NULL DEFAULT 'Medium' 
        CHECK (Priority IN ('Low', 'Medium', 'High', 'Critical')),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active' 
        CHECK (Status IN ('Planning', 'Active', 'OnHold', 'Completed', 'Cancelled')),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Projects_ProjectManagerId FOREIGN KEY (ProjectManagerId) 
        REFERENCES Users(UserId),
    CONSTRAINT CK_Projects_Dates CHECK (EndDate >= StartDate)
);
GO

CREATE INDEX IX_Projects_ProjectManagerId ON Projects(ProjectManagerId);
CREATE INDEX IX_Projects_Status ON Projects(Status);
CREATE INDEX IX_Projects_Dates ON Projects(StartDate, EndDate);
GO

-- ============================================
-- ProjectDepartments Table (Many-to-Many)
-- ============================================
CREATE TABLE ProjectDepartments (
    ProjectDepartmentId INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT NOT NULL,
    DepartmentId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ProjectDepartments_ProjectId FOREIGN KEY (ProjectId) 
        REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    CONSTRAINT FK_ProjectDepartments_DepartmentId FOREIGN KEY (DepartmentId) 
        REFERENCES Departments(DepartmentId),
    CONSTRAINT UQ_ProjectDepartments UNIQUE (ProjectId, DepartmentId)
);
GO

CREATE INDEX IX_ProjectDepartments_ProjectId ON ProjectDepartments(ProjectId);
CREATE INDEX IX_ProjectDepartments_DepartmentId ON ProjectDepartments(DepartmentId);
GO

-- ============================================
-- WeeklyLaborRequirements Table
-- ============================================
CREATE TABLE WeeklyLaborRequirements (
    RequirementId INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT NOT NULL,
    DepartmentId INT NOT NULL,
    WeekStartDate DATE NOT NULL,
    RequiredHours DECIMAL(7,2) NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_WeeklyLaborRequirements_ProjectId FOREIGN KEY (ProjectId) 
        REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    CONSTRAINT FK_WeeklyLaborRequirements_DepartmentId FOREIGN KEY (DepartmentId) 
        REFERENCES Departments(DepartmentId),
    CONSTRAINT CK_WeeklyLaborRequirements_Hours CHECK (RequiredHours >= 0),
    CONSTRAINT UQ_WeeklyLaborRequirements UNIQUE (ProjectId, DepartmentId, WeekStartDate)
);
GO

CREATE INDEX IX_WeeklyLaborRequirements_ProjectId ON WeeklyLaborRequirements(ProjectId);
CREATE INDEX IX_WeeklyLaborRequirements_DepartmentId ON WeeklyLaborRequirements(DepartmentId);
CREATE INDEX IX_WeeklyLaborRequirements_WeekStartDate ON WeeklyLaborRequirements(WeekStartDate);
GO

-- ============================================
-- EmployeeAssignments Table
-- ============================================
CREATE TABLE EmployeeAssignments (
    AssignmentId INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT NOT NULL,
    EmployeeId INT NOT NULL,
    WeekStartDate DATE NOT NULL,
    AssignedHours DECIMAL(7,2) NOT NULL DEFAULT 0,
    Notes NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_EmployeeAssignments_ProjectId FOREIGN KEY (ProjectId) 
        REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    CONSTRAINT FK_EmployeeAssignments_EmployeeId FOREIGN KEY (EmployeeId) 
        REFERENCES Employees(EmployeeId),
    CONSTRAINT CK_EmployeeAssignments_Hours CHECK (AssignedHours >= 0),
    CONSTRAINT UQ_EmployeeAssignments UNIQUE (ProjectId, EmployeeId, WeekStartDate)
);
GO

CREATE INDEX IX_EmployeeAssignments_ProjectId ON EmployeeAssignments(ProjectId);
CREATE INDEX IX_EmployeeAssignments_EmployeeId ON EmployeeAssignments(EmployeeId);
CREATE INDEX IX_EmployeeAssignments_WeekStartDate ON EmployeeAssignments(WeekStartDate);
GO

-- ============================================
-- AuditLog Table
-- ============================================
CREATE TABLE AuditLog (
    AuditId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    TableName NVARCHAR(100) NOT NULL,
    RecordId INT NOT NULL,
    Action NVARCHAR(50) NOT NULL CHECK (Action IN ('Insert', 'Update', 'Delete')),
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuditLog_UserId FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);
GO

CREATE INDEX IX_AuditLog_TableName ON AuditLog(TableName);
CREATE INDEX IX_AuditLog_Timestamp ON AuditLog(Timestamp);
CREATE INDEX IX_AuditLog_UserId ON AuditLog(UserId);
GO

-- Additional indexes for frequently filtered columns
CREATE INDEX IX_Users_IsActive ON Users(IsActive);
CREATE INDEX IX_Employees_IsActive ON Employees(IsActive);
CREATE INDEX IX_Projects_IsActive ON Projects(IsActive);
GO

PRINT 'Database schema created successfully';
