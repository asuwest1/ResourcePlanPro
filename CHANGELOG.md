# Changelog
All notable changes to ResourcePlan Pro will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-11

### Initial Release

#### Added - Backend API
- Complete .NET 6.0 Web API with RESTful endpoints
- JWT-based authentication and authorization
- Role-based access control (Admin, ProjectManager, DepartmentManager, Viewer)
- SQL Server 2019 database with comprehensive schema
- Entity Framework Core data access layer
- Swagger/OpenAPI documentation
- CORS configuration for cross-origin requests
- Error handling middleware
- Health check endpoints
- 30+ API endpoints covering all functionality

#### Added - Frontend
- Responsive HTML5/CSS3/JavaScript web interface
- Login page with JWT authentication
- Dashboard with project cards, timeline, and quick statistics
- Project management pages (list, detail, create, edit)
- Labor planning grid with editable hours
- Resource assignment with drag-and-drop interface
- Employee directory with filtering
- Department overview with utilization metrics
- Reports and conflicts page with priority visualization
- Real-time API integration
- Toast notification system
- Mobile-responsive design

#### Added - Database
- 7 core tables (Users, Departments, Employees, Projects, ProjectDepartments, WeeklyLaborRequirements, EmployeeAssignments)
- 1 audit table for change tracking
- 4 database views for analytics
- 4 stored procedures for complex queries
- Comprehensive indexes for performance
- Foreign key relationships with cascade rules
- Sample data for immediate testing

#### Added - Features
- Department-first labor planning methodology
- Weekly hour allocation by department
- Employee assignment with capacity checking
- Automated conflict detection (over-allocation, understaffing)
- 12-week resource timeline with heat map visualization
- Project staffing status indicators
- Dashboard with real-time metrics
- Quick statistics (active projects, available employees, conflicts)
- Resource utilization tracking
- Project priority management
- Status tracking (Planning, Active, OnHold, Completed)

#### Added - Documentation
- README.md with comprehensive overview
- DEPLOYMENT.md with production deployment guide
- QUICKSTART.md for rapid setup
- INSTALL.md with step-by-step installation
- API documentation via Swagger
- Inline code comments
- Database schema documentation

#### Added - Deployment Tools
- Automated PowerShell deployment script (Deploy.ps1)
- Development startup script (Start-Dev.ps1)
- API testing script (Test-API.ps1)
- IIS web.config for production hosting
- Production appsettings template
- .gitignore file

#### Security
- Password hashing with SHA-256
- JWT token authentication with 8-hour expiration
- SQL injection prevention via parameterized queries
- XSS protection via input sanitization
- CSRF protection via JWT tokens
- HTTPS enforcement in production
- Security headers in web.config

#### Performance
- Database query optimization with indexes
- Async/await throughout backend
- Connection pooling in Entity Framework
- Efficient data transfer with DTOs
- Client-side caching of static resources
- IIS compression support

### Technical Specifications
- Backend: .NET 6.0 C#
- Frontend: HTML5, CSS3, Vanilla JavaScript
- Database: Microsoft SQL Server 2019
- Server: Windows Server 2019, IIS 10
- Authentication: JWT Bearer tokens
- API Architecture: RESTful
- Design Pattern: Repository/Service layer

### Known Limitations
- Currently supports single organization
- No file attachments for projects
- No real-time collaboration features
- No mobile native apps

### Browser Support
- Chrome 90+
- Firefox 88+
- Edge 90+
- Safari 14+

### System Requirements
- Windows Server 2019 or Windows 10/11
- SQL Server 2019 (any edition)
- .NET 6.0 Runtime (Hosting Bundle for production)
- IIS 10 (for production)
- 4GB RAM minimum, 8GB recommended
- 10GB disk space

### Demo Credentials
- Username: jsmith (Admin)
- Password: Password123!
- Other demo users: mchen, sjohnson, erodriguez, dkim

---

## [1.1.0] - 2026-02-20

### Added - Email Notifications for Resource Conflicts
- Backend `NotificationService` with SMTP email sending and graceful fallback to queued notifications
- `NotificationsController` with endpoints: GET history, POST send-conflict-report, POST check-conflicts
- `NotificationLog` entity for tracking notification delivery status (Pending/Sent/Queued)
- HTML-formatted email body with conflict detail tables
- Frontend notification panel in Reports page with send form and history viewer

### Added - Export to CSV Functionality
- Backend `ExportService` with CSV generation for projects, employees, assignments, conflicts, and resource timelines
- `ExportController` with 5 GET endpoints returning file downloads
- Frontend export buttons on Projects list, Employees list, Project Detail, and Reports pages
- Proper CSV escaping for special characters

### Added - Advanced Reporting with Charts
- Backend `ReportingService` aggregating department utilization, project status distribution, weekly trends, skill demand, and top employee utilization
- `ReportsController` with GET endpoint supporting date range and week count parameters
- Frontend canvas-based chart visualizations (no external chart library):
  - Department utilization bar chart
  - Project status pie chart with legend
  - Weekly trend line+bar combo chart (hours + conflict counts)
  - Skill demand bar chart
  - Top employees horizontal bar list
- Tabbed Reports page layout (Conflicts, Charts & Analytics, Notifications)

### Added - Project Templates
- Backend `TemplateService` with full CRUD, create-from-project, and create-project-from-template
- `TemplatesController` with 6 endpoints including POST from-project/{id} and POST create-project
- `ProjectTemplate` entity with JSON-serialized department IDs and default hours
- Frontend template selection dropdown on Project Create page with auto-fill of priority, description, dates, and departments

### Added - Bulk Employee Assignment
- Backend `BulkCreateAssignmentsAsync` in `ResourceService` for creating/updating multiple assignments in one request
- POST `api/resources/assignments/bulk` endpoint in `ResourcesController`
- Frontend bulk assignment panel on Project Detail page with multi-select checkboxes and hour inputs

### Added - Resource Calendar View
- Backend `GetCalendarEventsAsync` in `ResourceService` returning assignment data with employee utilization
- GET `api/resources/calendar` endpoint in `ResourcesController`
- New `calendar.html` page with employee-by-week grid layout
- `calendar.js` with prev/next/today navigation, department/employee filters, and color-coded utilization cells (green/yellow/red)
- Calendar legend showing utilization thresholds
- Calendar link added to navigation on all pages

### Added - Skills-Based Matching
- Backend `SkillMatchingService` with employee-to-skill matching, match percentage calculation, and sorting by best match then availability
- POST `api/resources/skill-match` and GET `api/resources/skills` endpoints
- Frontend skill matching panel on Project Detail page with skill tag filters and match results table
- Quick-assign capability from skill match results

### Changed
- Extended `api.js` with new API modules: templates, notifications, reports, exports, and additional resource endpoints
- Updated `project-detail.js` with template save, bulk assignment, skill matching, and export functionality
- Updated `project-form.js` with template loading and applying
- Updated `projects.js` and `employees.js` with export button handlers
- Rewrote `reports.js` with tabbed layout, canvas charts, and notification management
- Added ~340 lines of CSS for v1.1.0 feature styles (tabs, calendar, charts, skill tags, modals)
- Updated `Program.cs` with DI registrations for all new services (interface + implementation)
- Added `SmtpSettings` configuration section to `appsettings.json`

### Database
- New `ProjectTemplates` table for storing reusable project templates
- New `NotificationLogs` table for tracking notification delivery
- Migration script: `Database/04_V110_Migration.sql`

---

## Future Enhancements (Roadmap)

### Version 1.2.0 (Planned)
- [ ] Mobile native apps (iOS/Android)
- [ ] Real-time collaboration via SignalR
- [ ] File attachments for projects
- [ ] Comment threads on assignments
- [ ] Activity audit log viewer
- [ ] Multi-organization support
- [ ] Advanced search and filters

### Version 2.0.0 (Planned)
- [ ] Machine learning for resource predictions
- [ ] Budget and cost tracking
- [ ] Time tracking integration
- [ ] Project portfolio management
- [ ] Gantt chart visualization
- [ ] Resource capacity planning AI
- [ ] Integration with Microsoft Project

---

## Upgrade Notes

This is the initial release. No upgrade path is needed.

---

## Breaking Changes

None - this is the initial release.

---

## Contributors

- Primary Developer: Solutions Architect
- Technology Stack: Microsoft Enterprise Platform
- Development Period: February 2026

---

## Support

For bug reports, feature requests, or questions:
- Review the documentation in the repository
- Check the troubleshooting section in INSTALL.md
- Refer to the API documentation at /swagger

---

**Note**: This is a production-ready application suitable for enterprise deployment on Windows Server 2019 environments with appropriate security hardening and operational procedures.
