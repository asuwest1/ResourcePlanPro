# CLAUDE.md

## Project Overview

ResourcePlan Pro is an enterprise labor resource planning system for project managers to allocate labor resources across projects, departments, and employees. It features 12-week forward-looking capacity planning, real-time conflict detection, and dashboard analytics.

## Tech Stack

- **Backend:** .NET 6.0 / ASP.NET Core Web API, Entity Framework Core 6.0
- **Frontend:** Vanilla HTML5/CSS3/JavaScript (ES6+) — no framework, no build step
- **Database:** SQL Server 2019
- **Auth:** JWT Bearer tokens with role-based access control
- **Deployment:** Windows Server 2019 / IIS 10

## Project Structure

```
Backend/           .NET 6.0 Web API
  Controllers/     11 controllers (Auth, Projects, Resources, Employees, Departments, DashboardEmployees, Health, Export, Notifications, Reports, Templates)
  Services/        10 services (Auth, Project, Resource, Employee, DashboardEmployee, Export, Notification, Reporting, SkillMatching, Template)
  Models/          Entities.cs (10 table entities + 4 view/query models), DTOs.cs (37 DTOs/request models)
  Data/            EF Core DbContext (ResourcePlanProContext)
  Middleware/      Global error handling (ErrorHandlingMiddleware)
  Utilities/       Helper classes (DateTimeHelper, ValidationHelper, CalculationHelper, StringHelper)
  Program.cs       Application entry point
Frontend/          Static HTML/CSS/JS
  js/              11 JS modules (config, auth, api, dashboard, projects, project-detail, project-form, employees, reports, calendar, login)
  pages/           8 HTML pages (employees, departments, project-create/detail/edit, projects, reports, calendar)
  css/styles.css   All styles (~1790 lines)
  login.html       Login page
  index.html       Dashboard
Database/          SQL Server scripts (run in order: 01_Create, 02_SampleData, 03_ViewsAndProcs, 04_V110_Migration)
*.ps1, *.bat       Root-level scripts (Deploy.ps1, Start-Dev.ps1, Test-API.ps1, Setup-Database.bat)
*.md               Root-level docs (README, INSTALL, DEPLOYMENT, QUICKSTART, CHANGELOG, etc.)
```

## Build & Run

### Backend

```bash
cd Backend
dotnet restore
dotnet build
dotnet run
# Runs on https://localhost:7001
# Swagger UI available at /swagger (development only)
```

### Frontend

```bash
cd Frontend
python -m http.server 8080
# OR: npx http-server -p 8080
# Accessible at http://localhost:8080
```

### Database Setup

```bash
sqlcmd -S localhost -i Database/01_CreateDatabase.sql
sqlcmd -S localhost -d ResourcePlanPro -i Database/02_SampleData.sql
sqlcmd -S localhost -d ResourcePlanPro -i Database/03_ViewsAndProcedures.sql
sqlcmd -S localhost -d ResourcePlanPro -i Database/04_V110_Migration.sql
```

## Testing

There is no automated test suite (no xUnit, NUnit, or Jest). Testing is manual:

- **Swagger UI:** `/swagger` endpoint for API testing in development
- **PowerShell script:** `Test-API.ps1` (root level) for API endpoint testing
- **Sample data:** 150+ records loaded via `Database/02_SampleData.sql`
- **Demo credentials:** See `Database/02_SampleData.sql` or `Documentation/QUICKSTART.md` for test user accounts

## Linting & Formatting

No explicit linting or formatting tools are configured (no ESLint, Prettier, EditorConfig, or StyleCop).

## Code Conventions

### C# (Backend)

- Namespace: `ResourcePlanPro.API.*`
- PascalCase for classes/methods, camelCase for parameters/locals
- Layered architecture: Controllers → Services → Data (DbContext)
- All data access is async/await with EF Core
- Dependency injection via built-in ASP.NET Core DI (`AddScoped`)
- Global error handling middleware catches exceptions and returns structured error responses
- `[Authorize]` attribute on protected endpoints; roles: Admin, ProjectManager, DepartmentManager, Viewer

### JavaScript (Frontend)

- Module/object pattern (e.g., `const Auth = { ... }`, `const API = { ... }`)
- camelCase for variables/functions
- Fetch API for HTTP calls with JWT token in Authorization header
- State stored in LocalStorage (token, user) and in-memory per page
- Toast notifications for user feedback (`Utils.showToast`)

### CSS

- CSS custom properties for theming (`--primary-color`, `--success-color`, etc.)
- Kebab-case class names (`.top-nav`, `.card-header`, `.status-green`)
- Responsive design with media queries

### SQL

- PascalCase for table/column names
- 10 tables: Users, Departments, Employees, Projects, ProjectDepartments, WeeklyLaborRequirements, EmployeeAssignments, AuditLog, ProjectTemplates, NotificationLogs
- FK constraints, cascade delete for parent-child, indexes on FKs and frequently queried columns

## Configuration

- **Backend config:** `Backend/appsettings.json` (dev), `Backend/appsettings.Production.json` (prod)
- **Frontend config:** `Frontend/js/config.js` — `CONFIG.API_BASE_URL` defaults to `https://localhost:7001/api`
- **CORS origins:** localhost ports 5000, 8080, 3000 (configured in appsettings.json)
- **JWT:** 8-hour token expiration, secret key must be 32+ characters

## Security Notes

- **Password hashing:** PBKDF2 with random salt (100k iterations, SHA256). Legacy SHA256-only hashes are supported for verification during migration.
- **JWT secrets:** Development key is in `appsettings.json` — must be replaced via environment variable or secrets vault in production.
- **Security headers:** CSP, HSTS, X-Frame-Options (DENY), X-Content-Type-Options, Referrer-Policy, and Permissions-Policy are set in both middleware and `web.config`.
- **CORS:** Restricted to specific origins, methods (`GET/POST/PUT/DELETE`), and headers (`Content-Type/Authorization/Accept`).
- **Input validation:** `weekCount` parameters are bounded (1–52), `minAvailableHours` must be non-negative, and stored procedures validate parameters.
- **Frontend:** No inline `onclick` handlers — all event binding uses `addEventListener`. `escapeHtml()` used for all user-generated content rendered via innerHTML. JWT token expiration is checked client-side.
- **Demo credentials** are only in `Database/02_SampleData.sql` and documentation — removed from the login page HTML.

## Key Architectural Notes

- Frontend has zero npm dependencies and no build pipeline
- Backend uses EF Core with explicit eager loading (no lazy loading)
- Database has 4 views and 4 stored procedures for analytics/reporting
- HTTPS enforced in production; CORS configured for allowed origins
- `.gitignore` covers build outputs, IDE files, logs, and database files
