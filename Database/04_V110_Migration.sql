-- ResourcePlanPro v1.1.0 Migration Script
-- SQL Server 2019
-- ============================================
-- Adds: ProjectTemplates, NotificationLogs tables
-- Run after: 01_CreateDatabase.sql, 02_SampleData.sql, 03_ViewsAndProcedures.sql
-- ============================================

USE ResourcePlanPro;
GO

-- ============================================
-- ProjectTemplates Table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectTemplates')
BEGIN
    CREATE TABLE ProjectTemplates (
        TemplateId INT IDENTITY(1,1) PRIMARY KEY,
        TemplateName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Priority NVARCHAR(20) NOT NULL DEFAULT 'Medium'
            CHECK (Priority IN ('Low', 'Medium', 'High', 'Critical')),
        DurationWeeks INT NOT NULL DEFAULT 12,
        DepartmentIds NVARCHAR(MAX) NULL,
        DefaultHoursJson NVARCHAR(MAX) NULL,
        CreatedByUserId INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ProjectTemplates_Users FOREIGN KEY (CreatedByUserId)
            REFERENCES Users(UserId)
    );

    CREATE INDEX IX_ProjectTemplates_CreatedByUserId
        ON ProjectTemplates(CreatedByUserId);

    CREATE INDEX IX_ProjectTemplates_IsActive
        ON ProjectTemplates(IsActive);

    PRINT 'Created ProjectTemplates table';
END
ELSE
BEGIN
    PRINT 'ProjectTemplates table already exists - skipping';
END
GO

-- ============================================
-- NotificationLogs Table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationLogs')
BEGIN
    CREATE TABLE NotificationLogs (
        NotificationId INT IDENTITY(1,1) PRIMARY KEY,
        NotificationType NVARCHAR(50) NOT NULL,
        RecipientEmail NVARCHAR(255) NOT NULL,
        Subject NVARCHAR(500) NOT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        RelatedEntityId INT NULL,
        RelatedEntityType NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
            CHECK (Status IN ('Pending', 'Sent', 'Queued', 'Failed')),
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        SentDate DATETIME2 NULL,
        ErrorMessage NVARCHAR(1000) NULL
    );

    CREATE INDEX IX_NotificationLogs_Status
        ON NotificationLogs(Status);

    CREATE INDEX IX_NotificationLogs_CreatedDate
        ON NotificationLogs(CreatedDate DESC);

    CREATE INDEX IX_NotificationLogs_RecipientEmail
        ON NotificationLogs(RecipientEmail);

    PRINT 'Created NotificationLogs table';
END
ELSE
BEGIN
    PRINT 'NotificationLogs table already exists - skipping';
END
GO

-- ============================================
-- Add Skills column to Employees table (if not exists)
-- Used by skills-based matching feature
-- ============================================
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('Employees') AND name = 'Skills'
)
BEGIN
    ALTER TABLE Employees
        ADD Skills NVARCHAR(500) NULL;

    PRINT 'Added Skills column to Employees table';
END
ELSE
BEGIN
    PRINT 'Skills column already exists on Employees table - skipping';
END
GO

-- ============================================
-- Populate sample skills data for existing employees
-- ============================================
IF EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('Employees') AND name = 'Skills'
)
BEGIN
    -- Only update employees that don't already have skills set
    UPDATE Employees SET Skills = 'Project Management,Leadership,Agile'
    WHERE EmployeeId = 1 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'Data Analysis,SQL,Python,Machine Learning'
    WHERE EmployeeId = 2 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'Java,Spring Boot,Microservices,AWS'
    WHERE EmployeeId = 3 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'UI/UX Design,Figma,CSS,React'
    WHERE EmployeeId = 4 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'DevOps,Docker,Kubernetes,CI/CD'
    WHERE EmployeeId = 5 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'C#,.NET,Entity Framework,Azure'
    WHERE EmployeeId = 6 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'Testing,QA Automation,Selenium,JUnit'
    WHERE EmployeeId = 7 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'Business Analysis,Requirements,BPMN'
    WHERE EmployeeId = 8 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'Network Security,Firewall,Penetration Testing'
    WHERE EmployeeId = 9 AND (Skills IS NULL OR Skills = '');

    UPDATE Employees SET Skills = 'Technical Writing,Documentation,API Docs'
    WHERE EmployeeId = 10 AND (Skills IS NULL OR Skills = '');

    PRINT 'Updated sample skills data for employees';
END
GO

-- ============================================
-- Insert sample project templates
-- ============================================
IF NOT EXISTS (SELECT TOP 1 1 FROM ProjectTemplates)
BEGIN
    INSERT INTO ProjectTemplates (TemplateName, Description, Priority, DurationWeeks, DepartmentIds, DefaultHoursJson, CreatedByUserId, IsActive, CreatedDate)
    VALUES
    ('Standard Development Project', 'A standard 12-week software development project template with typical department allocations.', 'Medium', 12, '[1,2,3]', '{"entries":[{"departmentId":1,"weekNumber":1,"hours":40},{"departmentId":1,"weekNumber":2,"hours":40},{"departmentId":2,"weekNumber":3,"hours":30},{"departmentId":2,"weekNumber":4,"hours":30}]}', 1, 1, GETUTCDATE()),
    ('Quick Sprint Project', 'A short 4-week sprint project for rapid delivery.', 'High', 4, '[1,2]', '{"entries":[{"departmentId":1,"weekNumber":1,"hours":60},{"departmentId":1,"weekNumber":2,"hours":60},{"departmentId":2,"weekNumber":1,"hours":20},{"departmentId":2,"weekNumber":2,"hours":20}]}', 1, 1, GETUTCDATE()),
    ('Infrastructure Upgrade', 'Template for infrastructure upgrade projects spanning 8 weeks.', 'Critical', 8, '[3]', '{"entries":[{"departmentId":3,"weekNumber":1,"hours":40},{"departmentId":3,"weekNumber":2,"hours":40},{"departmentId":3,"weekNumber":3,"hours":40},{"departmentId":3,"weekNumber":4,"hours":40}]}', 1, 1, GETUTCDATE());

    PRINT 'Inserted sample project templates';
END
GO

PRINT '============================================';
PRINT 'v1.1.0 Migration completed successfully';
PRINT '============================================';
GO
