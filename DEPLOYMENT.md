# ResourcePlan Pro - Deployment Guide

## Windows Server 2019 with IIS Deployment

### Step-by-Step Deployment Instructions

## 1. Server Prerequisites

### Install Required Software
1. **SQL Server 2019**
   - Download from Microsoft
   - Choose "Developer" or "Standard" edition
   - Install with default settings
   - Enable SQL Server Authentication
   - Enable TCP/IP protocol

2. **.NET 6.0 Hosting Bundle**
   - Download: https://dotnet.microsoft.com/download/dotnet/6.0
   - Install "ASP.NET Core Runtime 6.0.x - Windows Hosting Bundle"
   - Restart IIS after installation

3. **IIS Features**
   - Open Server Manager
   - Add Roles and Features
   - Install IIS with:
     * Application Development → ASP.NET 4.8
     * Application Development → WebSocket Protocol
     * Security → Windows Authentication (optional)
     * Common HTTP Features → Default Document, Static Content

## 2. Database Deployment

### Create Database
```powershell
# Connect to SQL Server
sqlcmd -S localhost -U sa -P YourPassword

# Run database scripts
:r C:\ResourcePlanPro\Database\01_CreateDatabase.sql
GO

USE ResourcePlanPro
GO

:r C:\ResourcePlanPro\Database\02_SampleData.sql
GO

:r C:\ResourcePlanPro\Database\03_ViewsAndProcedures.sql
GO
```

### Create SQL Login for Application
```sql
USE master;
GO

CREATE LOGIN ResourcePlanProApp WITH PASSWORD = 'StrongPassword123!';
GO

USE ResourcePlanPro;
GO

CREATE USER ResourcePlanProApp FOR LOGIN ResourcePlanProApp;
GO

ALTER ROLE db_datareader ADD MEMBER ResourcePlanProApp;
ALTER ROLE db_datawriter ADD MEMBER ResourcePlanProApp;
GRANT EXECUTE TO ResourcePlanProApp;
GO
```

## 3. Backend API Deployment

### Prepare API Files
1. **Build the application:**
```powershell
cd C:\ResourcePlanPro\Backend
dotnet publish -c Release -o C:\inetpub\ResourcePlanProAPI
```

2. **Update Configuration:**
Edit `C:\inetpub\ResourcePlanProAPI\appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ResourcePlanPro;User Id=ResourcePlanProApp;Password=StrongPassword123!;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "SecretKey": "GENERATE_A_SECURE_RANDOM_KEY_HERE_MINIMUM_32_CHARACTERS",
    "Issuer": "ResourcePlanPro",
    "Audience": "ResourcePlanProUsers",
    "ExpirationMinutes": 480
  },
  "AllowedHosts": "*",
  "CorsSettings": {
    "AllowedOrigins": [
      "https://your-domain.com",
      "http://localhost:8080"
    ]
  }
}
```

### Create IIS Application Pool
```powershell
Import-Module WebAdministration

# Create Application Pool
New-WebAppPool -Name "ResourcePlanProAPIPool"
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool -Name "startMode" -Value "AlwaysRunning"
```

### Create IIS Website for API
```powershell
# Create website
New-Website -Name "ResourcePlanProAPI" `
    -PhysicalPath "C:\inetpub\ResourcePlanProAPI" `
    -ApplicationPool "ResourcePlanProAPIPool" `
    -Port 443 -Ssl

# OR if not using SSL initially (not recommended for production)
New-Website -Name "ResourcePlanProAPI" `
    -PhysicalPath "C:\inetpub\ResourcePlanProAPI" `
    -ApplicationPool "ResourcePlanProAPIPool" `
    -Port 5001
```

### Configure SSL Certificate (Recommended)
```powershell
# Option 1: Self-signed certificate (for testing)
$cert = New-SelfSignedCertificate -DnsName "api.yourdomain.com" -CertStoreLocation "cert:\LocalMachine\My"
$binding = Get-WebBinding -Name "ResourcePlanProAPI" -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "My")

# Option 2: Import existing certificate
# Use IIS Manager → Server Certificates → Import
```

### Set Application Pool Identity
```powershell
# If using Integrated Security for SQL
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool `
    -Name processModel.identityType -Value 3
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool `
    -Name processModel.userName -Value "DOMAIN\ServiceAccount"
Set-ItemProperty IIS:\AppPools\ResourcePlanProAPIPool `
    -Name processModel.password -Value "Password"
```

### Verify API
1. Open browser to `https://localhost/swagger`
2. Should see Swagger UI with API documentation
3. Test /api/health endpoint

## 4. Frontend Deployment

### Prepare Frontend Files
```powershell
# Copy frontend files
Copy-Item -Path "C:\ResourcePlanPro\Frontend\*" `
    -Destination "C:\inetpub\ResourcePlanProWeb" -Recurse
```

### Update Frontend Configuration
Edit `C:\inetpub\ResourcePlanProWeb\js\config.js`:
```javascript
const CONFIG = {
    API_BASE_URL: 'https://api.yourdomain.com/api',  // or 'http://localhost:5001/api'
    // ... rest of config
};
```

### Create IIS Application Pool for Frontend
```powershell
# Create Application Pool
New-WebAppPool -Name "ResourcePlanProWebPool"
```

### Create IIS Website for Frontend
```powershell
New-Website -Name "ResourcePlanProWeb" `
    -PhysicalPath "C:\inetpub\ResourcePlanProWeb" `
    -ApplicationPool "ResourcePlanProWebPool" `
    -Port 443 -Ssl

# Set default document
Set-WebConfigurationProperty -Filter "//defaultDocument/files" `
    -PSPath "IIS:\Sites\ResourcePlanProWeb" `
    -Name "." -Value @{value="login.html"}
```

### Configure URL Rewrite (Optional)
Install URL Rewrite module, then create web.config in Frontend folder:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <system.webServer>
        <rewrite>
            <rules>
                <rule name="Redirect to HTTPS" stopProcessing="true">
                    <match url="(.*)" />
                    <conditions>
                        <add input="{HTTPS}" pattern="^OFF$" />
                    </conditions>
                    <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
                </rule>
            </rules>
        </rewrite>
    </system.webServer>
</configuration>
```

## 5. Testing Deployment

### Test Database
```sql
-- Verify tables exist
SELECT name FROM sys.tables ORDER BY name;

-- Verify sample data
SELECT * FROM Users;
SELECT * FROM Projects;

-- Test stored procedure
EXEC sp_GetProjectDashboard NULL;
```

### Test API
```powershell
# Test health endpoint
Invoke-WebRequest -Uri "https://localhost/api/health"

# Test login
$body = @{
    username = "jsmith"
    password = "Password123!"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

### Test Frontend
1. Open browser to `https://yourdomain.com`
2. Should redirect to login.html
3. Login with demo credentials:
   - Username: `jsmith`
   - Password: `Password123!`
4. Verify dashboard loads

## 6. Security Hardening

### SSL/TLS Configuration
```powershell
# Disable weak protocols
New-Item 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Server' -Force
New-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Server' -Name 'Enabled' -Value 0 -PropertyType 'DWord'

# Enable strong protocols
New-Item 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server' -Force
New-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server' -Name 'Enabled' -Value 1 -PropertyType 'DWord'
```

### Firewall Rules
```powershell
# Allow HTTPS
New-NetFirewallRule -DisplayName "Allow HTTPS" `
    -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow

# Allow HTTP (if needed)
New-NetFirewallRule -DisplayName "Allow HTTP" `
    -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
```

### SQL Server Security
```sql
-- Enforce password policy
ALTER LOGIN ResourcePlanProApp WITH CHECK_POLICY = ON;

-- Enable transparent data encryption
USE master;
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongMasterKeyPassword!';
CREATE CERTIFICATE TDECert WITH SUBJECT = 'TDE Certificate';
USE ResourcePlanPro;
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE TDECert;
ALTER DATABASE ResourcePlanPro SET ENCRYPTION ON;
```

## 7. Monitoring and Maintenance

### Enable IIS Logging
```powershell
Set-WebConfigurationProperty -Filter '/system.applicationHost/sites/siteDefaults/logFile' `
    -Name 'logFormat' -Value 'W3C'
Set-WebConfigurationProperty -Filter '/system.applicationHost/sites/siteDefaults/logFile' `
    -Name 'directory' -Value 'C:\inetpub\logs\LogFiles'
```

### SQL Server Maintenance Plan
- Daily full backups
- Transaction log backups every 15 minutes
- Weekly index optimization
- Monthly statistics updates

### Application Monitoring
- Monitor IIS application pool health
- Monitor SQL Server connections
- Track API response times
- Monitor disk space

## 8. Troubleshooting

### Common Issues

**503 Service Unavailable**
- Check Application Pool is started
- Verify .NET Core Hosting Bundle installed
- Check application pool identity has permissions

**Connection String Errors**
- Verify SQL Server is running
- Test connection with SQL Server Management Studio
- Check firewall allows SQL connections
- Verify login credentials

**CORS Errors**
- Update CorsSettings in appsettings.json
- Add frontend URL to AllowedOrigins
- Restart IIS after changes

### Log Locations
- **IIS Logs**: C:\inetpub\logs\LogFiles
- **SQL Server Logs**: SQL Server Management Studio → Management → SQL Server Logs
- **Application Logs**: Event Viewer → Windows Logs → Application

## 9. Backup and Recovery

### Database Backup
```sql
BACKUP DATABASE ResourcePlanPro
TO DISK = 'C:\Backups\ResourcePlanPro_Full.bak'
WITH FORMAT, COMPRESSION;
```

### Application Files Backup
```powershell
# Backup IIS sites
$date = Get-Date -Format "yyyyMMdd"
Compress-Archive -Path "C:\inetpub\ResourcePlanProAPI" `
    -DestinationPath "C:\Backups\API_$date.zip"
Compress-Archive -Path "C:\inetpub\ResourcePlanProWeb" `
    -DestinationPath "C:\Backups\Web_$date.zip"
```

## 10. Scaling Considerations

### Multiple Web Servers
- Use shared SQL Server
- Implement Redis for distributed caching
- Use load balancer (Azure Load Balancer, F5, etc.)

### High Availability
- SQL Server Always On Availability Groups
- IIS Application Request Routing for load balancing
- Automated failover configuration

---

**Deployment Checklist:**
- [ ] SQL Server installed and configured
- [ ] Database created with all scripts
- [ ] .NET 6.0 Hosting Bundle installed
- [ ] IIS configured with application pools
- [ ] SSL certificates installed
- [ ] Backend API deployed and tested
- [ ] Frontend deployed and tested
- [ ] Security hardening applied
- [ ] Firewall rules configured
- [ ] Monitoring enabled
- [ ] Backup strategy implemented
- [ ] User training completed

For support, consult the README.md or contact system administrator.
