# ResourcePlan Pro - Labor Resource Planning System

A comprehensive enterprise-grade web application for project managers to plan and allocate labor resources across projects, departments, and employees.

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [System Architecture](#system-architecture)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [API Documentation](#api-documentation)
- [Database Schema](#database-schema)
- [Security](#security)
- [Troubleshooting](#troubleshooting)

## Overview

ResourcePlan Pro is a four-tier enterprise application designed for Windows Server environments that enables project managers to:
- Define weekly labor hour requirements by department
- Assign specific employees to project weeks
- Track resource utilization and conflicts
- Generate comprehensive reports and analytics

## Features

### Core Functionality
- **Project Management**: Create and manage projects with timeline tracking
- **Department-First Planning**: Define labor needs by department before employee assignment
- **Resource Allocation**: Assign employees to projects with real-time conflict detection
- **Dashboard Analytics**: Visual representation of resource utilization
- **Conflict Resolution**: Automated detection and resolution suggestions
- **Timeline Visualization**: 12-week forward-looking resource capacity planning

### Technical Features
- JWT-based authentication and authorization
- Role-based access control (Admin, ProjectManager, DepartmentManager, Viewer)
- RESTful API architecture
- Responsive web interface
- Real-time validation and feedback
- Comprehensive audit logging

## Technology Stack

### Backend
- **.NET 6.0** - Web API framework
- **Entity Framework Core 6.0** - ORM and data access
- **SQL Server 2019** - Database engine
- **JWT Bearer Authentication** - Security
- **Swagger/OpenAPI** - API documentation

### Frontend
- **HTML5/CSS3** - Markup and styling
- **Vanilla JavaScript** - Client-side logic
- **Responsive Design** - Mobile-friendly interface
- **RESTful API consumption** - AJAX/Fetch API

### Infrastructure
- **Windows Server 2019** - Hosting platform
- **IIS 10** - Web server
- **SQL Server 2019** - Database server

## System Architecture

### Four-Tier Architecture

```
┌─────────────────────────────────────┐
│     Client Layer (Web Browser)      │
│  HTML5, CSS3, JavaScript            │
└──────────────┬──────────────────────┘
               │ HTTPS/REST
┌──────────────▼──────────────────────┐
│    Application Layer (.NET API)     │
│  Controllers, Services, Auth        │
└──────────────┬──────────────────────┘
               │ EF Core
┌──────────────▼──────────────────────┐
│     Data Layer (SQL Server)         │
│  Tables, Views, Stored Procedures   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│    Integration Layer (Optional)     │
│  Email Services, Export Services    │
└─────────────────────────────────────┘
```

### Key Components

**Backend Services:**
- AuthService: Authentication and JWT token management
- ProjectService: Project lifecycle management
- ResourceService: Labor requirements and assignments
- DashboardService: Analytics and reporting
- EmployeeService: Employee data management

**Database Objects:**
- 7 core tables (Users, Departments, Employees, Projects, etc.)
- 4 views for reporting (vw_EmployeeWorkloadSummary, etc.)
- 4 stored procedures for complex queries

## Installation

### Prerequisites
- Windows Server 2019 or later
- SQL Server 2019 or later
- .NET 6.0 Runtime or SDK
- IIS 10 with ASP.NET Core Hosting Bundle
- Modern web browser (Chrome, Firefox, Edge)

### Database Setup

1. **Create Database:**
```bash
sqlcmd -S localhost -i Database/01_CreateDatabase.sql
```

2. **Load Sample Data:**
```bash
sqlcmd -S localhost -d ResourcePlanPro -i Database/02_SampleData.sql
```

3. **Create Views and Procedures:**
```bash
sqlcmd -S localhost -d ResourcePlanPro -i Database/03_ViewsAndProcedures.sql
```

### Backend API Setup

1. **Update Connection String:**
Edit `Backend/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ResourcePlanPro;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

2. **Build the API:**
```bash
cd Backend
dotnet restore
dotnet build
```

3. **Run the API:**
```bash
dotnet run
```

The API will start on `https://localhost:7001` by default.

4. **Deploy to IIS:**
```bash
dotnet publish -c Release
# Copy contents of bin/Release/net6.0/publish to IIS website directory
```

### Frontend Setup

1. **Update API Configuration:**
Edit `Frontend/js/config.js`:
```javascript
const CONFIG = {
    API_BASE_URL: 'https://your-api-server/api',
    // ... other settings
};
```

2. **Deploy to IIS:**
- Copy Frontend folder contents to IIS website directory
- Ensure Anonymous Authentication is enabled
- Set default document to `login.html`

## Configuration

### Security Settings

**JWT Configuration** (`appsettings.json`):
```json
{
  "JwtSettings": {
    "SecretKey": "CHANGE_THIS_TO_A_SECURE_KEY_MINIMUM_32_CHARACTERS",
    "Issuer": "ResourcePlanPro",
    "Audience": "ResourcePlanProUsers",
    "ExpirationMinutes": 480
  }
}
```

**CORS Settings:**
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "https://your-frontend-domain.com"
    ]
  }
}
```

### User Roles

- **Admin**: Full system access, user management
- **ProjectManager**: Create/manage projects, assign resources
- **DepartmentManager**: View all projects using department resources
- **Viewer**: Read-only access to all data

## Usage

### Login
Default credentials for demo:
- Username: `jsmith`
- Password: `Password123!`

### Create a Project

1. Navigate to Dashboard
2. Click "+ New Project"
3. Fill in project details:
   - Name, Description
   - Start and End dates
   - Project Manager
   - Priority level
4. Select departments involved
5. Click "Create Project"

### Define Labor Requirements

1. Open project details
2. Navigate to "Plan Hours" tab
3. Click on grid cells to enter required hours
4. Grid shows:
   - Departments (rows)
   - Weeks (columns)
   - Required hours (cell values)
5. System calculates totals automatically
6. Click "Save Changes"

### Assign Employees

1. Navigate to "Assign Resources" tab
2. Select week and department
3. System displays:
   - Required hours for that week/department
   - Available employees with capacity
4. Drag employees or click "Assign"
5. Enter hours for assignment
6. System validates against:
   - Employee availability
   - Over-allocation limits
7. Click "Save Assignment"

### View Conflicts

1. Dashboard shows conflict summary
2. Click "View All Conflicts"
3. System displays:
   - Over-allocated employees
   - Understaffed projects
   - Priority level
4. Click conflict for resolution suggestions
5. Apply suggested solution or adjust manually

## API Documentation

### Authentication

**POST /api/auth/login**
```json
Request:
{
  "username": "string",
  "password": "string"
}

Response:
{
  "success": true,
  "token": "jwt_token_here",
  "user": {
    "userId": 1,
    "username": "jsmith",
    "role": "ProjectManager"
  }
}
```

### Projects

**GET /api/projects** - Get all projects
**GET /api/projects/{id}** - Get project by ID
**POST /api/projects** - Create new project
**PUT /api/projects/{id}** - Update project
**DELETE /api/projects/{id}** - Delete project

### Resources

**GET /api/resources/requirements?projectId={id}** - Get labor requirements
**POST /api/resources/requirements** - Save labor requirement
**GET /api/resources/available-employees** - Get available employees
**POST /api/resources/assignments** - Create assignment
**GET /api/resources/timeline** - Get resource timeline

Full API documentation available at `/swagger` when running in development mode.

## Database Schema

### Core Tables

**Users** - System users and authentication
**Departments** - Organizational departments
**Employees** - Employee records and department assignment
**Projects** - Project master records
**ProjectDepartments** - Many-to-many project/department link
**WeeklyLaborRequirements** - Required hours by project/department/week
**EmployeeAssignments** - Employee assignments to projects by week

### Key Views

**vw_EmployeeWorkloadSummary** - Employee utilization by week
**vw_ProjectStaffingStatus** - Project staffing gaps
**vw_DepartmentUtilization** - Department capacity usage
**vw_ResourceConflicts** - Over-allocated employees

## Security

### Authentication
- JWT token-based authentication
- Tokens expire after 8 hours (configurable)
- Passwords hashed with SHA-256
- HTTPS required for production

### Authorization
- Role-based access control
- API endpoints protected with [Authorize] attribute
- User claims validated on each request

### Best Practices
1. Change default JWT secret key
2. Use strong passwords (12+ characters, mixed case, numbers, symbols)
3. Enable SSL/TLS in production
4. Restrict CORS to specific domains
5. Regular security audits of audit log

## Troubleshooting

### Common Issues

**Cannot connect to database:**
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure database exists and user has permissions

**API returns 401 Unauthorized:**
- Check if JWT token is expired
- Verify token is included in Authorization header
- Ensure user account is active

**Projects not loading:**
- Check browser console for errors
- Verify API is running and accessible
- Check CORS configuration

**Over-allocation not detecting:**
- Verify WeeklyLaborRequirements are set
- Check EmployeeAssignments are created correctly
- Run sp_GetConflictSummary to test

### Logs

- **API Logs**: Check IIS logs and application logs in Event Viewer
- **Database Logs**: Check SQL Server error logs
- **Browser Logs**: Use F12 developer tools console

## Support

For issues or questions:
1. Check this README
2. Review API documentation at /swagger
3. Check database views and stored procedures
4. Contact system administrator

## License

Copyright © 2026 ResourcePlan Pro. All rights reserved.

---

**Version**: 1.0.0  
**Last Updated**: February 2026
