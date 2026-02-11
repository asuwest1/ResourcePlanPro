-- Views and Stored Procedures for ResourcePlanPro
-- ============================================

USE ResourcePlanPro;
GO

-- ============================================
-- View: Employee Workload Summary
-- ============================================
CREATE OR ALTER VIEW vw_EmployeeWorkloadSummary AS
SELECT 
    e.EmployeeId,
    e.FirstName,
    e.LastName,
    e.Email,
    d.DepartmentName,
    e.JobTitle,
    e.HoursPerWeek AS StandardHours,
    ea.WeekStartDate,
    ISNULL(SUM(ea.AssignedHours), 0) AS TotalAssignedHours,
    e.HoursPerWeek - ISNULL(SUM(ea.AssignedHours), 0) AS AvailableHours,
    CAST(ISNULL(SUM(ea.AssignedHours), 0) / e.HoursPerWeek * 100 AS DECIMAL(5,2)) AS UtilizationPercentage,
    COUNT(DISTINCT ea.ProjectId) AS ActiveProjectCount
FROM Employees e
INNER JOIN Departments d ON e.DepartmentId = d.DepartmentId
LEFT JOIN EmployeeAssignments ea ON e.EmployeeId = ea.EmployeeId
WHERE e.IsActive = 1
GROUP BY 
    e.EmployeeId, e.FirstName, e.LastName, e.Email, 
    d.DepartmentName, e.JobTitle, e.HoursPerWeek, ea.WeekStartDate;
GO

-- ============================================
-- View: Project Staffing Status
-- ============================================
CREATE OR ALTER VIEW vw_ProjectStaffingStatus AS
SELECT 
    p.ProjectId,
    p.ProjectName,
    p.Status,
    p.Priority,
    u.FirstName + ' ' + u.LastName AS ProjectManager,
    wlr.WeekStartDate,
    d.DepartmentName,
    wlr.RequiredHours,
    ISNULL(SUM(ea.AssignedHours), 0) AS AssignedHours,
    wlr.RequiredHours - ISNULL(SUM(ea.AssignedHours), 0) AS HoursGap,
    CASE 
        WHEN wlr.RequiredHours = 0 THEN 100
        ELSE CAST(ISNULL(SUM(ea.AssignedHours), 0) / wlr.RequiredHours * 100 AS DECIMAL(5,2))
    END AS StaffingPercentage,
    CASE 
        WHEN wlr.RequiredHours - ISNULL(SUM(ea.AssignedHours), 0) > 10 THEN 'Understaffed'
        WHEN ISNULL(SUM(ea.AssignedHours), 0) - wlr.RequiredHours > 10 THEN 'Overstaffed'
        ELSE 'Adequate'
    END AS StaffingStatus
FROM Projects p
INNER JOIN Users u ON p.ProjectManagerId = u.UserId
INNER JOIN WeeklyLaborRequirements wlr ON p.ProjectId = wlr.ProjectId
INNER JOIN Departments d ON wlr.DepartmentId = d.DepartmentId
LEFT JOIN EmployeeAssignments ea ON 
    wlr.ProjectId = ea.ProjectId AND 
    wlr.WeekStartDate = ea.WeekStartDate AND
    EXISTS (SELECT 1 FROM Employees e WHERE e.EmployeeId = ea.EmployeeId AND e.DepartmentId = wlr.DepartmentId)
WHERE p.IsActive = 1
GROUP BY 
    p.ProjectId, p.ProjectName, p.Status, p.Priority, 
    u.FirstName, u.LastName, wlr.WeekStartDate, 
    d.DepartmentName, wlr.RequiredHours;
GO

-- ============================================
-- View: Department Utilization
-- ============================================
CREATE OR ALTER VIEW vw_DepartmentUtilization AS
SELECT 
    d.DepartmentId,
    d.DepartmentName,
    ea.WeekStartDate,
    COUNT(DISTINCT e.EmployeeId) AS TotalEmployees,
    SUM(e.HoursPerWeek) AS TotalCapacity,
    ISNULL(SUM(ea.AssignedHours), 0) AS TotalAssignedHours,
    SUM(e.HoursPerWeek) - ISNULL(SUM(ea.AssignedHours), 0) AS AvailableCapacity,
    CAST(ISNULL(SUM(ea.AssignedHours), 0) / NULLIF(SUM(e.HoursPerWeek), 0) * 100 AS DECIMAL(5,2)) AS UtilizationPercentage
FROM Departments d
INNER JOIN Employees e ON d.DepartmentId = e.DepartmentId AND e.IsActive = 1
LEFT JOIN EmployeeAssignments ea ON e.EmployeeId = ea.EmployeeId
WHERE d.IsActive = 1
GROUP BY d.DepartmentId, d.DepartmentName, ea.WeekStartDate;
GO

-- ============================================
-- View: Resource Conflicts
-- ============================================
CREATE OR ALTER VIEW vw_ResourceConflicts AS
SELECT 
    e.EmployeeId,
    e.FirstName + ' ' + e.LastName AS EmployeeName,
    d.DepartmentName,
    ea.WeekStartDate,
    e.HoursPerWeek AS StandardHours,
    SUM(ea.AssignedHours) AS TotalAssignedHours,
    SUM(ea.AssignedHours) - e.HoursPerWeek AS OverallocationHours,
    CAST(SUM(ea.AssignedHours) / e.HoursPerWeek * 100 AS DECIMAL(5,2)) AS UtilizationPercentage,
    COUNT(DISTINCT ea.ProjectId) AS ProjectCount,
    STRING_AGG(p.ProjectName, ', ') AS Projects
FROM Employees e
INNER JOIN Departments d ON e.DepartmentId = d.DepartmentId
INNER JOIN EmployeeAssignments ea ON e.EmployeeId = ea.EmployeeId
INNER JOIN Projects p ON ea.ProjectId = p.ProjectId
WHERE e.IsActive = 1
GROUP BY 
    e.EmployeeId, e.FirstName, e.LastName, 
    d.DepartmentName, ea.WeekStartDate, e.HoursPerWeek
HAVING SUM(ea.AssignedHours) > e.HoursPerWeek;
GO

-- ============================================
-- Stored Procedure: Get Available Employees for Assignment
-- ============================================
CREATE OR ALTER PROCEDURE sp_GetAvailableEmployees
    @DepartmentId INT,
    @WeekStartDate DATE,
    @MinAvailableHours DECIMAL(5,2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate parameters
    IF @DepartmentId <= 0
    BEGIN
        RAISERROR('DepartmentId must be a positive integer', 16, 1);
        RETURN;
    END

    IF @MinAvailableHours < 0
    BEGIN
        RAISERROR('MinAvailableHours cannot be negative', 16, 1);
        RETURN;
    END

    SELECT 
        e.EmployeeId,
        e.FirstName,
        e.LastName,
        e.Email,
        e.JobTitle,
        e.Skills,
        e.HoursPerWeek,
        ISNULL(SUM(ea.AssignedHours), 0) AS CurrentlyAssigned,
        e.HoursPerWeek - ISNULL(SUM(ea.AssignedHours), 0) AS AvailableHours,
        CAST(ISNULL(SUM(ea.AssignedHours), 0) / e.HoursPerWeek * 100 AS DECIMAL(5,2)) AS CurrentUtilization,
        COUNT(ea.ProjectId) AS ActiveProjects
    FROM Employees e
    LEFT JOIN EmployeeAssignments ea ON 
        e.EmployeeId = ea.EmployeeId AND 
        ea.WeekStartDate = @WeekStartDate
    WHERE 
        e.DepartmentId = @DepartmentId AND
        e.IsActive = 1
    GROUP BY 
        e.EmployeeId, e.FirstName, e.LastName, e.Email, 
        e.JobTitle, e.Skills, e.HoursPerWeek
    HAVING e.HoursPerWeek - ISNULL(SUM(ea.AssignedHours), 0) >= @MinAvailableHours
    ORDER BY AvailableHours DESC;
END
GO

-- ============================================
-- Stored Procedure: Get Project Dashboard Data
-- ============================================
CREATE OR ALTER PROCEDURE sp_GetProjectDashboard
    @ProjectManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.ProjectId,
        p.ProjectName,
        p.Description,
        p.StartDate,
        p.EndDate,
        p.Priority,
        p.Status,
        u.FirstName + ' ' + u.LastName AS ProjectManager,
        COUNT(DISTINCT pd.DepartmentId) AS DepartmentCount,
        COUNT(DISTINCT ea.EmployeeId) AS EmployeeCount,
        (
            SELECT TOP 1 
                CASE 
                    WHEN RequiredHours - ISNULL(AssignedHours, 0) > 10 THEN 'Red'
                    WHEN ISNULL(AssignedHours, 0) - RequiredHours > 10 THEN 'Yellow'
                    ELSE 'Green'
                END
            FROM vw_ProjectStaffingStatus 
            WHERE ProjectId = p.ProjectId 
                AND WeekStartDate >= CAST(GETDATE() AS DATE)
            ORDER BY WeekStartDate
        ) AS CurrentWeekStatus,
        (
            SELECT ISNULL(SUM(RequiredHours), 0)
            FROM WeeklyLaborRequirements
            WHERE ProjectId = p.ProjectId
                AND WeekStartDate >= CAST(GETDATE() AS DATE)
                AND WeekStartDate < DATEADD(DAY, 7, CAST(GETDATE() AS DATE))
        ) AS CurrentWeekRequiredHours,
        (
            SELECT ISNULL(SUM(AssignedHours), 0)
            FROM EmployeeAssignments
            WHERE ProjectId = p.ProjectId
                AND WeekStartDate >= CAST(GETDATE() AS DATE)
                AND WeekStartDate < DATEADD(DAY, 7, CAST(GETDATE() AS DATE))
        ) AS CurrentWeekAssignedHours
    FROM Projects p
    INNER JOIN Users u ON p.ProjectManagerId = u.UserId
    LEFT JOIN ProjectDepartments pd ON p.ProjectId = pd.ProjectId
    LEFT JOIN EmployeeAssignments ea ON p.ProjectId = ea.ProjectId
    WHERE 
        p.IsActive = 1 AND
        p.Status IN ('Active', 'Planning') AND
        (@ProjectManagerId IS NULL OR p.ProjectManagerId = @ProjectManagerId)
    GROUP BY 
        p.ProjectId, p.ProjectName, p.Description, p.StartDate, 
        p.EndDate, p.Priority, p.Status, u.FirstName, u.LastName
    ORDER BY p.Priority DESC, p.ProjectName;
END
GO

-- ============================================
-- Stored Procedure: Get Resource Timeline
-- ============================================
CREATE OR ALTER PROCEDURE sp_GetResourceTimeline
    @StartDate DATE = NULL,
    @WeekCount INT = 12
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate parameters
    IF @WeekCount < 1 OR @WeekCount > 52
    BEGIN
        RAISERROR('WeekCount must be between 1 and 52', 16, 1);
        RETURN;
    END

    -- Default to current week if not specified
    IF @StartDate IS NULL
        SET @StartDate = DATEADD(DAY, 1 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS DATE));
    
    WITH WeekNumbers AS (
        SELECT 
            n AS WeekNumber,
            DATEADD(WEEK, n, @StartDate) AS WeekStart
        FROM (
            SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 
            UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7
            UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11
        ) nums
        WHERE n < @WeekCount
    )
    SELECT 
        d.DepartmentId,
        d.DepartmentName,
        wn.WeekNumber,
        wn.WeekStart,
        ISNULL(du.TotalCapacity, 0) AS TotalCapacity,
        ISNULL(du.TotalAssignedHours, 0) AS TotalAssignedHours,
        ISNULL(du.UtilizationPercentage, 0) AS UtilizationPercentage,
        CASE 
            WHEN ISNULL(du.UtilizationPercentage, 0) < 60 THEN 'Light'
            WHEN ISNULL(du.UtilizationPercentage, 0) < 85 THEN 'Medium'
            ELSE 'Heavy'
        END AS LoadLevel
    FROM WeekNumbers wn
    CROSS JOIN Departments d
    LEFT JOIN vw_DepartmentUtilization du ON 
        d.DepartmentId = du.DepartmentId AND 
        wn.WeekStart = du.WeekStartDate
    WHERE d.IsActive = 1
    ORDER BY d.DepartmentName, wn.WeekNumber;
END
GO

-- ============================================
-- Stored Procedure: Get Conflict Summary
-- ============================================
CREATE OR ALTER PROCEDURE sp_GetConflictSummary
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Overallocated Employees
    SELECT 
        'OverallocatedEmployee' AS ConflictType,
        EmployeeId AS EntityId,
        EmployeeName AS EntityName,
        DepartmentName,
        WeekStartDate,
        OverallocationHours AS Variance,
        UtilizationPercentage,
        CASE 
            WHEN OverallocationHours > 10 THEN 'High'
            WHEN OverallocationHours > 5 THEN 'Medium'
            ELSE 'Low'
        END AS Priority,
        'Employee is over-allocated by ' + CAST(OverallocationHours AS NVARCHAR(10)) + ' hours' AS Description,
        Projects AS AffectedProjects
    FROM vw_ResourceConflicts
    WHERE WeekStartDate >= CAST(GETDATE() AS DATE)
    
    UNION ALL
    
    -- Understaffed Projects
    SELECT 
        'UnderstaffedProject' AS ConflictType,
        ProjectId AS EntityId,
        ProjectName AS EntityName,
        DepartmentName,
        WeekStartDate,
        HoursGap AS Variance,
        StaffingPercentage AS UtilizationPercentage,
        CASE 
            WHEN HoursGap > 20 THEN 'High'
            WHEN HoursGap > 10 THEN 'Medium'
            ELSE 'Low'
        END AS Priority,
        'Project needs ' + CAST(HoursGap AS NVARCHAR(10)) + ' more hours' AS Description,
        NULL AS AffectedProjects
    FROM vw_ProjectStaffingStatus
    WHERE StaffingStatus = 'Understaffed'
        AND WeekStartDate >= CAST(GETDATE() AS DATE)
    
    ORDER BY Priority DESC, WeekStartDate, EntityName;
END
GO

PRINT 'Views and stored procedures created successfully';
