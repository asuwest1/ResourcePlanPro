# ResourcePlan Pro - Quick Start Guide

## Getting Started in 5 Minutes

### For Development/Testing

#### 1. Database Setup (2 minutes)
```bash
# Open SQL Server Management Studio or command line
sqlcmd -S localhost -i Database/01_CreateDatabase.sql
sqlcmd -S localhost -d ResourcePlanPro -i Database/02_SampleData.sql
sqlcmd -S localhost -d ResourcePlanPro -i Database/03_ViewsAndProcedures.sql
```

#### 2. Start Backend API (1 minute)
```bash
cd Backend
dotnet run
```
API will be available at: `https://localhost:7001`

#### 3. Start Frontend (1 minute)
```bash
# Option A: Using Python
cd Frontend
python -m http.server 8080

# Option B: Using Node.js
npx http-server -p 8080

# Option C: Open directly in browser
# Open Frontend/login.html in your browser
```

#### 4. Update API URL (30 seconds)
Edit `Frontend/js/config.js`:
```javascript
API_BASE_URL: 'https://localhost:7001/api'
```

#### 5. Login and Explore (30 seconds)
- Navigate to `http://localhost:8080/login.html`
- Use demo credentials:
  - Username: `jsmith`
  - Password: `Password123!`

---

## Key Features to Try

### 1. Dashboard
- View active projects
- See resource conflicts
- Check utilization statistics
- Explore 12-week timeline

### 2. Create a Project
1. Click "+ New Project"
2. Fill in details:
   - Name: "Test Project"
   - Start Date: Today
   - End Date: 30 days from now
   - Priority: High
3. Select 2-3 departments
4. Save

### 3. Plan Labor Hours
1. Open your project
2. Click "Plan Hours" tab
3. Click grid cells to enter hours:
   - Engineering, Week 1: 40 hours
   - Design, Week 1: 20 hours
   - QA, Week 2: 30 hours
4. Save changes

### 4. Assign Employees
1. Click "Assign Resources" tab
2. Select Week 1 and Engineering
3. View available employees
4. Assign employees:
   - Sarah Johnson: 20 hours
   - Michael Chen: 20 hours
5. Watch real-time capacity calculation
6. Save assignments

### 5. View Conflicts
1. Return to Dashboard
2. Note conflict warnings appear
3. Click "View All Conflicts"
4. See over-allocated employees
5. Click conflict for suggestions
6. Apply recommended solution

---

## Default Users and Roles

| Username | Password | Role | Description |
|----------|----------|------|-------------|
| jsmith | Password123! | Admin | Full system access |
| mchen | Password123! | ProjectManager | Create and manage projects |
| sjohnson | Password123! | ProjectManager | Create and manage projects |
| erodriguez | Password123! | DepartmentManager | View department resources |
| dkim | Password123! | Viewer | Read-only access |

---

## Sample Data Included

- **6 Departments**: Engineering, UX Design, QA Testing, Marketing, Operations, Data Analytics
- **24 Employees**: Distributed across departments with various skills
- **8 Projects**: Including "Website Redesign", "ERP Migration", "Mobile App Development"
- **12 Weeks of Planning**: Pre-populated labor requirements and assignments

---

## Common Tasks

### Add a New Employee
```sql
INSERT INTO Employees (FirstName, LastName, Email, DepartmentId, JobTitle, HoursPerWeek, HireDate)
VALUES ('Jane', 'Doe', 'jane.doe@company.com', 1, 'Developer', 40, GETDATE());
```

### Change User Password
```sql
-- Password hash for "NewPassword123!"
UPDATE Users 
SET PasswordHash = 'YOUR_NEW_HASH_HERE'
WHERE Username = 'jsmith';
```

### View All Conflicts
```sql
EXEC sp_GetConflictSummary;
```

### Check Employee Workload
```sql
SELECT * FROM vw_EmployeeWorkloadSummary
WHERE EmployeeId = 1
ORDER BY WeekStartDate;
```

---

## API Endpoints Quick Reference

### Authentication
- POST `/api/auth/login` - Login
- GET `/api/auth/validate` - Validate token

### Dashboard
- GET `/api/dashboard` - Get full dashboard
- GET `/api/dashboard/stats` - Get quick stats
- GET `/api/dashboard/conflicts` - Get conflicts

### Projects
- GET `/api/projects` - List all projects
- GET `/api/projects/{id}` - Get project details
- POST `/api/projects` - Create project
- PUT `/api/projects/{id}` - Update project
- DELETE `/api/projects/{id}` - Delete project

### Resources
- GET `/api/resources/requirements?projectId={id}` - Get labor requirements
- POST `/api/resources/requirements` - Save requirement
- GET `/api/resources/available-employees` - Get available employees
- POST `/api/resources/assignments` - Create assignment
- GET `/api/resources/timeline` - Get timeline

### Employees
- GET `/api/employees` - List all employees
- GET `/api/employees/{id}` - Get employee details
- GET `/api/employees/department/{id}` - Get department employees

---

## Troubleshooting Quick Fixes

### Can't connect to database
```bash
# Check SQL Server is running
services.msc
# Look for "SQL Server (MSSQLSERVER)" - should be "Running"
```

### API won't start
```bash
# Check port is not in use
netstat -ano | findstr :7001

# Try different port
dotnet run --urls="https://localhost:5000"
```

### Frontend can't reach API
1. Check API is running (`https://localhost:7001/api/health`)
2. Update `Frontend/js/config.js` with correct URL
3. Check browser console for CORS errors
4. Verify CORS settings in API `appsettings.json`

### Login not working
1. Verify database has sample data:
   ```sql
   SELECT * FROM Users WHERE Username = 'jsmith';
   ```
2. Check API logs for errors
3. Verify password hash matches (use demo credentials first)

---

## Next Steps

1. **Customize for your organization:**
   - Add your departments
   - Import employee list
   - Create your projects

2. **Explore advanced features:**
   - Bulk import employees (CSV)
   - Export reports to Excel
   - Set up email notifications

3. **Deploy to production:**
   - Follow `DEPLOYMENT.md`
   - Configure SSL certificates
   - Set up automated backups

4. **Integrate with existing systems:**
   - Connect to Active Directory
   - Import from HR system
   - Export to project management tools

---

## Support Resources

- **Full Documentation**: README.md
- **Deployment Guide**: DEPLOYMENT.md
- **API Documentation**: https://localhost:7001/swagger
- **Database Schema**: Database/01_CreateDatabase.sql

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Alt + D | Go to Dashboard |
| Alt + P | Go to Projects |
| Alt + E | Go to Employees |
| Ctrl + N | New Project (when on Projects page) |
| Esc | Close modal/dropdown |

---

**Ready to go!** You now have a fully functional resource planning system. Start by exploring the dashboard and creating your first project!

Questions? Check README.md for detailed information or consult the API documentation at `/swagger`.
