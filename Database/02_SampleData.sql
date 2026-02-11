-- Sample Data for ResourcePlanPro
-- ============================================

USE ResourcePlanPro;
GO

-- ============================================
-- Insert Sample Users
-- Note: Password is 'Password123!' hashed with SHA256
-- ============================================
INSERT INTO Users (Username, PasswordHash, Email, FirstName, LastName, Role, IsActive) VALUES
('jsmith', 'EF797C8118F02DFB649607DD5D3F8C7623048C9C063D532CC95C5ED7A898A64F', 'john.smith@company.com', 'John', 'Smith', 'Admin', 1),
('mchen', 'EF797C8118F02DFB649607DD5D3F8C7623048C9C063D532CC95C5ED7A898A64F', 'michael.chen@company.com', 'Michael', 'Chen', 'ProjectManager', 1),
('sjohnson', 'EF797C8118F02DFB649607DD5D3F8C7623048C9C063D532CC95C5ED7A898A64F', 'sarah.johnson@company.com', 'Sarah', 'Johnson', 'ProjectManager', 1),
('erodriguez', 'EF797C8118F02DFB649607DD5D3F8C7623048C9C063D532CC95C5ED7A898A64F', 'emily.rodriguez@company.com', 'Emily', 'Rodriguez', 'DepartmentManager', 1),
('dkim', 'EF797C8118F02DFB649607DD5D3F8C7623048C9C063D532CC95C5ED7A898A64F', 'david.kim@company.com', 'David', 'Kim', 'Viewer', 1);
GO

-- ============================================
-- Insert Departments
-- ============================================
INSERT INTO Departments (DepartmentName, Description, ManagerUserId, IsActive) VALUES
('Engineering', 'Software Engineering and Development', 2, 1),
('UX Design', 'User Experience and Interface Design', 4, 1),
('QA Testing', 'Quality Assurance and Testing', NULL, 1),
('Marketing', 'Marketing and Communications', NULL, 1),
('Operations', 'IT Operations and Infrastructure', NULL, 1),
('Data Analytics', 'Business Intelligence and Data Science', NULL, 1);
GO

-- ============================================
-- Insert Employees
-- ============================================
INSERT INTO Employees (FirstName, LastName, Email, DepartmentId, JobTitle, HoursPerWeek, Skills, HireDate) VALUES
-- Engineering Department
('Sarah', 'Johnson', 'sarah.johnson@company.com', 1, 'Senior Developer', 40, 'Frontend, React, JavaScript', '2020-03-15'),
('Michael', 'Chen', 'michael.chen@company.com', 1, 'Lead Engineer', 40, 'Backend, Python, Architecture', '2019-01-10'),
('Alex', 'Martinez', 'alex.martinez@company.com', 1, 'Developer', 40, 'Backend, Python, Django', '2021-06-01'),
('Jennifer', 'Lee', 'jennifer.lee@company.com', 1, 'Full Stack Developer', 40, 'Full Stack, Node.js, React', '2020-09-20'),
('Robert', 'Taylor', 'robert.taylor@company.com', 1, 'Frontend Developer', 40, 'Frontend, Vue.js, CSS', '2021-02-14'),
('Chris', 'Anderson', 'chris.anderson@company.com', 1, 'Developer', 40, 'Full Stack, .NET, C#', '2020-11-05'),
('Jessica', 'Brown', 'jessica.brown@company.com', 1, 'Senior Developer', 40, 'Backend, Java, Spring', '2019-08-30'),
('Daniel', 'Wilson', 'daniel.wilson@company.com', 1, 'Developer', 40, 'Mobile, iOS, Swift', '2021-04-18'),

-- UX Design Department
('Emily', 'Rodriguez', 'emily.rodriguez@company.com', 2, 'UX Designer', 40, 'UI/UX, Figma, Adobe XD', '2020-05-10'),
('Amanda', 'Garcia', 'amanda.garcia@company.com', 2, 'Senior UX Designer', 40, 'UI/UX, User Research, Prototyping', '2019-07-15'),
('Ryan', 'Thomas', 'ryan.thomas@company.com', 2, 'Visual Designer', 40, 'Visual Design, Branding', '2021-01-20'),
('Lisa', 'Moore', 'lisa.moore@company.com', 2, 'UX Researcher', 40, 'User Research, Testing', '2020-10-05'),

-- QA Testing Department
('David', 'Kim', 'david.kim@company.com', 3, 'QA Engineer', 40, 'Manual Testing, Automation', '2020-04-12'),
('Michelle', 'White', 'michelle.white@company.com', 3, 'Senior QA Engineer', 40, 'Automation, Selenium, Performance', '2019-11-08'),
('Kevin', 'Harris', 'kevin.harris@company.com', 3, 'QA Engineer', 40, 'Manual Testing, API Testing', '2021-03-25'),
('Rachel', 'Clark', 'rachel.clark@company.com', 3, 'Test Automation Engineer', 40, 'Automation, Cypress, CI/CD', '2020-12-10'),

-- Marketing Department
('Laura', 'Lewis', 'laura.lewis@company.com', 4, 'Marketing Manager', 40, 'Content, Social Media, SEO', '2019-09-18'),
('Steven', 'Walker', 'steven.walker@company.com', 4, 'Content Strategist', 40, 'Content Writing, Strategy', '2020-07-22'),
('Nicole', 'Hall', 'nicole.hall@company.com', 4, 'Marketing Coordinator', 40, 'Campaigns, Analytics', '2021-05-15'),

-- Operations Department
('James', 'Allen', 'james.allen@company.com', 5, 'DevOps Engineer', 40, 'AWS, Docker, Kubernetes', '2020-02-28'),
('Patricia', 'Young', 'patricia.young@company.com', 5, 'System Administrator', 40, 'Windows Server, Active Directory', '2019-12-05'),
('Brian', 'King', 'brian.king@company.com', 5, 'Network Engineer', 40, 'Networking, Security, Firewall', '2020-08-14'),

-- Data Analytics Department
('Mark', 'Wright', 'mark.wright@company.com', 6, 'Data Analyst', 40, 'SQL, Power BI, Python', '2020-06-10'),
('Karen', 'Scott', 'karen.scott@company.com', 6, 'Senior Data Scientist', 40, 'Machine Learning, Python, R', '2019-10-20'),
('Andrew', 'Green', 'andrew.green@company.com', 6, 'BI Developer', 40, 'SQL Server, SSRS, Tableau', '2021-02-08');
GO

-- ============================================
-- Insert Projects
-- ============================================
INSERT INTO Projects (ProjectName, Description, ProjectManagerId, StartDate, EndDate, Priority, Status) VALUES
('Website Redesign', 'Complete redesign of company website with modern UI/UX', 2, '2026-01-07', '2026-04-30', 'High', 'Active'),
('ERP Migration', 'Migration from legacy ERP system to cloud-based solution', 3, '2026-01-14', '2026-06-30', 'Critical', 'Active'),
('Mobile App Development', 'New mobile application for customer engagement', 2, '2025-12-15', '2026-05-15', 'High', 'Active'),
('Infrastructure Upgrade', 'Server infrastructure modernization and cloud migration', 3, '2026-01-07', '2026-03-31', 'Medium', 'Active'),
('Data Warehouse Project', 'Build enterprise data warehouse for analytics', 2, '2026-02-01', '2026-07-31', 'Medium', 'Planning'),
('Customer Portal', 'Self-service customer portal development', 3, '2025-11-01', '2026-04-15', 'High', 'Active'),
('Marketing Automation', 'Implement marketing automation platform', 2, '2026-01-15', '2026-03-30', 'Low', 'Active'),
('Security Enhancement', 'Organization-wide security audit and improvements', 3, '2026-01-20', '2026-05-20', 'Critical', 'Active');
GO

-- ============================================
-- Link Projects to Departments
-- ============================================
INSERT INTO ProjectDepartments (ProjectId, DepartmentId) VALUES
-- Website Redesign
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5),
-- ERP Migration
(2, 1), (2, 3), (2, 5), (2, 6),
-- Mobile App Development
(3, 1), (3, 2), (3, 3), (3, 4), (3, 6),
-- Infrastructure Upgrade
(4, 1), (4, 5), (4, 3),
-- Data Warehouse Project
(5, 1), (5, 6), (5, 3),
-- Customer Portal
(6, 1), (6, 2), (6, 3), (6, 4),
-- Marketing Automation
(7, 1), (7, 4), (7, 6),
-- Security Enhancement
(8, 1), (8, 5), (8, 3);
GO

-- ============================================
-- Insert Weekly Labor Requirements
-- Generate for 12 weeks starting Jan 7, 2026
-- ============================================
DECLARE @WeekStart DATE = '2026-01-07';
DECLARE @WeekCounter INT = 0;

WHILE @WeekCounter < 12
BEGIN
    -- Website Redesign (Project 1)
    INSERT INTO WeeklyLaborRequirements (ProjectId, DepartmentId, WeekStartDate, RequiredHours)
    VALUES 
        (1, 1, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 1 AND 3 THEN 80 WHEN @WeekCounter BETWEEN 4 AND 7 THEN 60 ELSE 40 END),
        (1, 2, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 0 AND 4 THEN 40 WHEN @WeekCounter BETWEEN 5 AND 8 THEN 30 ELSE 20 END),
        (1, 3, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter < 2 THEN 0 WHEN @WeekCounter BETWEEN 2 AND 6 THEN 30 ELSE 40 END),
        (1, 4, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 0 AND 8 THEN 15 ELSE 20 END),
        (1, 5, DATEADD(WEEK, @WeekCounter, @WeekStart), 10);
    
    -- ERP Migration (Project 2)
    IF @WeekCounter >= 1
    BEGIN
        INSERT INTO WeeklyLaborRequirements (ProjectId, DepartmentId, WeekStartDate, RequiredHours)
        VALUES 
            (2, 1, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 2 AND 6 THEN 100 ELSE 60 END),
            (2, 3, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 3 AND 8 THEN 40 ELSE 20 END),
            (2, 5, DATEADD(WEEK, @WeekCounter, @WeekStart), 20),
            (2, 6, DATEADD(WEEK, @WeekCounter, @WeekStart), 30);
    END
    
    -- Mobile App Development (Project 3)
    INSERT INTO WeeklyLaborRequirements (ProjectId, DepartmentId, WeekStartDate, RequiredHours)
    VALUES 
        (3, 1, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 2 AND 7 THEN 80 ELSE 40 END),
        (3, 2, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 1 AND 5 THEN 30 ELSE 20 END),
        (3, 3, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 4 AND 10 THEN 30 ELSE 15 END);
    
    -- Infrastructure Upgrade (Project 4)
    INSERT INTO WeeklyLaborRequirements (ProjectId, DepartmentId, WeekStartDate, RequiredHours)
    VALUES 
        (4, 1, DATEADD(WEEK, @WeekCounter, @WeekStart), 20),
        (4, 5, DATEADD(WEEK, @WeekCounter, @WeekStart), CASE WHEN @WeekCounter BETWEEN 2 AND 6 THEN 40 ELSE 30 END),
        (4, 3, DATEADD(WEEK, @WeekCounter, @WeekStart), 10);
    
    SET @WeekCounter = @WeekCounter + 1;
END
GO

-- ============================================
-- Insert Sample Employee Assignments
-- ============================================
INSERT INTO EmployeeAssignments (ProjectId, EmployeeId, WeekStartDate, AssignedHours) VALUES
-- Week 1 (Jan 7, 2026) - Website Redesign
(1, 1, '2026-01-07', 20), -- Sarah Johnson
(1, 5, '2026-01-07', 20), -- Robert Taylor
(1, 9, '2026-01-07', 20), -- Emily Rodriguez
(1, 13, '2026-01-07', 12), -- David Kim
-- Week 1 - Mobile App
(3, 2, '2026-01-07', 20), -- Michael Chen
(3, 8, '2026-01-07', 20), -- Daniel Wilson

-- Week 2 (Jan 14, 2026) - Website Redesign
(1, 1, '2026-01-14', 15),
(1, 5, '2026-01-14', 20),
(1, 6, '2026-01-14', 20),
(1, 9, '2026-01-14', 20),
-- Week 2 - ERP Migration
(2, 2, '2026-01-14', 20),
(2, 3, '2026-01-14', 20),
(2, 4, '2026-01-14', 20),

-- Week 3 (Jan 21, 2026) - Website Redesign
(1, 2, '2026-01-21', 20),
(1, 1, '2026-01-21', 15),
(1, 6, '2026-01-21', 20),
(1, 9, '2026-01-21', 20),
(1, 13, '2026-01-21', 20),
-- Week 3 - Mobile App
(3, 8, '2026-01-21', 20),
(3, 3, '2026-01-21', 15);
GO

PRINT 'Sample data inserted successfully';
