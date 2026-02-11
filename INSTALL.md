# ResourcePlan Pro - Installation Guide
Complete step-by-step installation instructions for Windows Server 2019

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Quick Start (Development)](#quick-start-development)
3. [Production Deployment](#production-deployment)
4. [Manual Installation](#manual-installation)
5. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- **Windows Server 2019** or Windows 10/11
- **SQL Server 2019** (Standard, Express, or Developer Edition)
- **.NET 6.0 SDK** for development OR **.NET 6.0 Hosting Bundle** for production
- **IIS 10** (for production deployment)
- **PowerShell 5.1+** (included in Windows)

### Optional Tools
- **SQL Server Management Studio (SSMS)** 18.0 or later
- **Visual Studio 2022** or **VS Code** (for development)
- **Postman** or similar (for API testing)

### Download Links
- .NET 6.0 SDK: https://dotnet.microsoft.com/download/dotnet/6.0
- .NET 6.0 Hosting Bundle: https://dotnet.microsoft.com/download/dotnet/6.0 (Windows Hosting Bundle)
- SQL Server 2019: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- SSMS: https://aka.ms/ssmsfullsetup

---

## Quick Start (Development)

### 1. Clone or Extract Files
```powershell
# Navigate to your projects folder
cd C:\Projects
# Extract the ResourcePlanPro folder here
```

### 2. Configure Database Connection
Edit `Backend\appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ResourcePlanPro;Integrated Security=true;TrustServerCertificate=true"
  }
}
```

### 3. Create Database
```powershell
cd C:\Projects\ResourcePlanPro

# Run database scripts
sqlcmd -S localhost -E -i "Database\01_CreateDatabase.sql"
sqlcmd -S localhost -E -i "Database\02_SampleData.sql"
sqlcmd -S localhost -E -i "Database\03_ViewsAndProcedures.sql"
```

### 4. Start Development Environment
```powershell
# Option A: Use automated startup script
.\Start-Dev.ps1

# Option B: Start manually
# Terminal 1 - Backend API
cd Backend
dotnet run

# Terminal 2 - Frontend (requires Python)
cd Frontend
python -m http.server 8080
```

### 5. Access Application
- **Frontend**: http://localhost:8080
- **API**: https://localhost:5001
- **API Docs**: https://localhost:5001/swagger
- **Username**: jsmith
- **Password**: Password123!

---

## Production Deployment

### Automated Deployment

The easiest way to deploy to production is using the automated PowerShell script:

```powershell
# Open PowerShell as Administrator
cd C:\ResourcePlanPro

# Run deployment script
.\Deploy.ps1 -SqlServer "YOUR_SERVER" -DatabaseName "ResourcePlanPro"

# With SQL Authentication
.\Deploy.ps1 -SqlServer "YOUR_SERVER" -SqlUsername "sa" -SqlPassword "YourPassword"
```

**Script Actions:**
1. Verifies prerequisites (IIS, .NET Hosting Bundle, SQL Server)
2. Creates and populates database
3. Builds and publishes Backend API
4. Deploys Frontend files
5. Configures IIS websites and application pools
6. Generates secure JWT key
7. Restarts IIS

### Post-Deployment Steps

#### 1. Configure SSL Certificate
```powershell
# In IIS Manager:
# 1. Select your website
# 2. Click "Bindings" → "Add"
# 3. Type: https, Port: 443
# 4. Select your SSL certificate
# 5. Click OK
```

#### 2. Update Frontend API URL
Edit `C:\inetpub\ResourcePlanProWeb\js\config.js`:
```javascript
const API_BASE_URL = 'https://yourdomain.com/api';
```

#### 3. Configure Firewall
```powershell
# Allow HTTPS traffic
New-NetFirewallRule -DisplayName "ResourcePlanPro HTTPS" `
    -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

#### 4. Update CORS Settings
Edit `C:\inetpub\ResourcePlanProAPI\appsettings.json`:
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "https://yourdomain.com"
    ]
  }
}
```

#### 5. Restart IIS
```powershell
iisreset
```

---

## Manual Installation

### Step 1: Install SQL Server

1. Download SQL Server 2019
2. Run installer, select "Basic" installation
3. Note the server name (usually `localhost` or computer name)

### Step 2: Install .NET Hosting Bundle

1. Download .NET 6.0 Hosting Bundle
2. Run installer
3. Restart computer

### Step 3: Enable IIS

```powershell
# Run as Administrator
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationDevelopment
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HealthAndDiagnostics
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Security
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Performance
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerManagementTools
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionStatic
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIExtensions
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIFilter
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45

# Restart
Restart-Computer
```

### Step 4: Create Database

1. Open SQL Server Management Studio (SSMS)
2. Connect to your SQL Server instance
3. Open and execute `Database\01_CreateDatabase.sql`
4. Open and execute `Database\02_SampleData.sql`
5. Open and execute `Database\03_ViewsAndProcedures.sql`

### Step 5: Build Backend API

```powershell
cd Backend
dotnet restore
dotnet publish -c Release -o C:\inetpub\ResourcePlanProAPI
```

### Step 6: Configure Backend

1. Edit `C:\inetpub\ResourcePlanProAPI\appsettings.json`
2. Update ConnectionStrings → DefaultConnection
3. Update JwtSettings → SecretKey (use a secure random string)
4. Update CorsSettings → AllowedOrigins

### Step 7: Deploy Frontend

```powershell
# Copy frontend files
xcopy Frontend\* C:\inetpub\ResourcePlanProWeb\ /E /I /Y
```

### Step 8: Configure IIS

#### Create Application Pool for API
```powershell
Import-Module WebAdministration

# Create API App Pool
New-WebAppPool -Name "ResourcePlanProAPIPool"
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool -Name startMode -Value AlwaysRunning

# Create API Website
New-Website -Name "ResourcePlanProAPI" `
    -PhysicalPath "C:\inetpub\ResourcePlanProAPI" `
    -ApplicationPool "ResourcePlanProAPIPool" `
    -Port 443
```

#### Create Application Pool for Frontend
```powershell
# Create Web App Pool
New-WebAppPool -Name "ResourcePlanProWebPool"

# Create Web Website
New-Website -Name "ResourcePlanProWeb" `
    -PhysicalPath "C:\inetpub\ResourcePlanProWeb" `
    -ApplicationPool "ResourcePlanProWebPool" `
    -Port 443

# Set default document
Set-WebConfigurationProperty -Filter "//defaultDocument/files" `
    -PSPath "IIS:\Sites\ResourcePlanProWeb" `
    -Name "." -Value @{value="login.html"}
```

### Step 9: Set Permissions

```powershell
# Grant IIS_IUSRS read permissions
$apiPath = "C:\inetpub\ResourcePlanProAPI"
$webPath = "C:\inetpub\ResourcePlanProWeb"

icacls $apiPath /grant "IIS_IUSRS:(OI)(CI)RX" /T
icacls $webPath /grant "IIS_IUSRS:(OI)(CI)RX" /T
```

### Step 10: Restart IIS

```powershell
iisreset
```

---

## Troubleshooting

### Database Connection Issues

**Problem**: Cannot connect to SQL Server

**Solutions**:
1. Verify SQL Server is running:
   ```powershell
   Get-Service MSSQLSERVER
   ```
2. Enable TCP/IP in SQL Server Configuration Manager
3. Add firewall rule for SQL Server (port 1433)
4. Check connection string in appsettings.json

### API Not Starting

**Problem**: API returns 500 errors or won't start

**Solutions**:
1. Check Event Viewer → Windows Logs → Application
2. Enable stdout logging in web.config:
   ```xml
   <aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
   ```
3. Verify .NET 6.0 Hosting Bundle is installed
4. Check application pool is running
5. Verify ConnectionString is correct

### Frontend Cannot Reach API

**Problem**: Login fails with network error

**Solutions**:
1. Verify API is accessible: https://localhost:7001/api/health
2. Check CORS settings in appsettings.json
3. Update API_BASE_URL in Frontend\js\config.js
4. Check browser console for specific errors

### SSL Certificate Errors

**Problem**: "Your connection is not private" warnings

**Solutions**:
1. For development: Accept self-signed certificate warning
2. For production: Install valid SSL certificate in IIS
3. Update Frontend config to use https:// for API calls

### Database Migration Errors

**Problem**: Tables or procedures not found

**Solutions**:
1. Verify all three SQL scripts ran successfully
2. Check if ResourcePlanPro database exists
3. Verify user has appropriate permissions:
   ```sql
   USE ResourcePlanPro;
   EXEC sp_addrolemember 'db_datareader', 'YourUser';
   EXEC sp_addrolemember 'db_datawriter', 'YourUser';
   ```

### Performance Issues

**Problem**: Slow page loads or API responses

**Solutions**:
1. Check SQL Server performance
2. Verify database indexes exist (see 01_CreateDatabase.sql)
3. Enable IIS compression
4. Check network latency between frontend and API
5. Review database query execution plans

---

## Verification

After installation, verify everything works:

### 1. Test Database
```sql
USE ResourcePlanPro;
SELECT COUNT(*) FROM Projects;  -- Should return sample projects
SELECT COUNT(*) FROM Employees; -- Should return sample employees
```

### 2. Test API
```powershell
# Test health endpoint
Invoke-RestMethod -Uri "https://localhost:7001/api/health"

# Test login
$body = @{username="jsmith"; password="Password123!"} | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7001/api/auth/login" `
    -Method Post -Body $body -ContentType "application/json"
```

### 3. Test Frontend
1. Open browser to https://localhost:443
2. Login with jsmith / Password123!
3. Verify dashboard loads with sample data
4. Click on a project to view details

---

## Support

For issues or questions:
1. Check the [README.md](README.md) for general information
2. Review [DEPLOYMENT.md](DEPLOYMENT.md) for deployment details
3. See [QUICKSTART.md](QUICKSTART.md) for quick reference
4. Check application logs in Event Viewer (Windows Logs → Application)

---

## Security Checklist

Before going to production:

- [ ] Change all default passwords
- [ ] Generate new JWT secret key (minimum 32 characters)
- [ ] Install valid SSL certificate
- [ ] Configure firewall rules
- [ ] Enable SQL Server encryption
- [ ] Set up database backups
- [ ] Configure log retention
- [ ] Review CORS allowed origins
- [ ] Enable HTTPS-only mode
- [ ] Set up monitoring and alerts
- [ ] Document administrative procedures
- [ ] Create disaster recovery plan

---

**Version**: 1.0.0  
**Last Updated**: February 2026
