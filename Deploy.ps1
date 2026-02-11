# ResourcePlan Pro - Automated Deployment Script
# Deploys Backend API and Frontend to IIS on Windows Server

param(
    [Parameter(Mandatory=$false)]
    [string]$SqlServer = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "ResourcePlanPro",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlUsername = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlPassword = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiPath = "C:\inetpub\ResourcePlanProAPI",
    
    [Parameter(Mandatory=$false)]
    [string]$WebPath = "C:\inetpub\ResourcePlanProWeb",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiPort = "443",
    
    [Parameter(Mandatory=$false)]
    [string]$WebPort = "443"
)

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "ResourcePlan Pro - Automated Deployment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Function to test SQL connection
function Test-SqlConnection {
    param($Server, $Database, $Username, $Password)
    
    try {
        $connectionString = if ($Username) {
            "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;TrustServerCertificate=true"
        } else {
            "Server=$Server;Database=$Database;Integrated Security=true;TrustServerCertificate=true"
        }
        
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Step 1: Verify Prerequisites
Write-Host "[1/7] Checking prerequisites..." -ForegroundColor Yellow

# Check IIS
$iisInstalled = Get-WindowsFeature -Name Web-Server | Where-Object {$_.Installed -eq $true}
if (-not $iisInstalled) {
    Write-Error "IIS is not installed. Please install IIS first."
    exit 1
}
Write-Host "  ✓ IIS installed" -ForegroundColor Green

# Check .NET 6.0 Hosting Bundle
$dotnetHosting = Get-ChildItem "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" -ErrorAction SilentlyContinue
if (-not $dotnetHosting) {
    Write-Warning "  ! .NET 6.0 Hosting Bundle may not be installed"
    Write-Host "    Download from: https://dotnet.microsoft.com/download/dotnet/6.0" -ForegroundColor Yellow
} else {
    Write-Host "  ✓ .NET Hosting Bundle installed" -ForegroundColor Green
}

# Check SQL Server connection
Write-Host "  Testing SQL Server connection..." -ForegroundColor Gray
if (Test-SqlConnection -Server $SqlServer -Database "master" -Username $SqlUsername -Password $SqlPassword) {
    Write-Host "  ✓ SQL Server accessible" -ForegroundColor Green
} else {
    Write-Error "Cannot connect to SQL Server at $SqlServer"
    exit 1
}

# Step 2: Create Database
Write-Host ""
Write-Host "[2/7] Setting up database..." -ForegroundColor Yellow

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbScriptsPath = Join-Path $scriptPath "Database"

if (Test-Path $dbScriptsPath) {
    $sqlcmdArgs = if ($SqlUsername) {
        "-S $SqlServer -U $SqlUsername -P $SqlPassword"
    } else {
        "-S $SqlServer -E"
    }
    
    # Run database scripts
    Write-Host "  Creating database schema..." -ForegroundColor Gray
    $script1 = Join-Path $dbScriptsPath "01_CreateDatabase.sql"
    Invoke-Expression "sqlcmd $sqlcmdArgs -i `"$script1`""
    
    Write-Host "  Loading sample data..." -ForegroundColor Gray
    $script2 = Join-Path $dbScriptsPath "02_SampleData.sql"
    Invoke-Expression "sqlcmd $sqlcmdArgs -i `"$script2`""
    
    Write-Host "  Creating views and procedures..." -ForegroundColor Gray
    $script3 = Join-Path $dbScriptsPath "03_ViewsAndProcedures.sql"
    Invoke-Expression "sqlcmd $sqlcmdArgs -i `"$script3`""
    
    Write-Host "  ✓ Database created successfully" -ForegroundColor Green
} else {
    Write-Warning "  ! Database scripts not found at $dbScriptsPath"
}

# Step 3: Build Backend API
Write-Host ""
Write-Host "[3/7] Building Backend API..." -ForegroundColor Yellow

$backendPath = Join-Path $scriptPath "Backend"
if (Test-Path $backendPath) {
    Push-Location $backendPath
    
    Write-Host "  Restoring packages..." -ForegroundColor Gray
    dotnet restore
    
    Write-Host "  Building in Release mode..." -ForegroundColor Gray
    dotnet publish -c Release -o $ApiPath
    
    Pop-Location
    Write-Host "  ✓ Backend built and published to $ApiPath" -ForegroundColor Green
} else {
    Write-Error "Backend path not found at $backendPath"
    exit 1
}

# Step 4: Configure Backend
Write-Host ""
Write-Host "[4/7] Configuring Backend API..." -ForegroundColor Yellow

$appsettingsPath = Join-Path $ApiPath "appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    
    # Update connection string
    $connectionString = if ($SqlUsername) {
        "Server=$SqlServer;Database=$DatabaseName;User Id=$SqlUsername;Password=$SqlPassword;TrustServerCertificate=true"
    } else {
        "Server=$SqlServer;Database=$DatabaseName;Integrated Security=true;TrustServerCertificate=true"
    }
    $appsettings.ConnectionStrings.DefaultConnection = $connectionString
    
    # Generate secure JWT key if not present
    if ($appsettings.JwtSettings.SecretKey -match "ChangeInProduction|YourSecretKeyHere") {
        $bytes = New-Object byte[] 32
        $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::Create()
        $rng.GetBytes($bytes)
        $appsettings.JwtSettings.SecretKey = [Convert]::ToBase64String($bytes)
        Write-Host "  ✓ Generated secure JWT key" -ForegroundColor Green
    }
    
    $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
    Write-Host "  ✓ Configuration updated" -ForegroundColor Green
}

# Step 5: Deploy Frontend
Write-Host ""
Write-Host "[5/7] Deploying Frontend..." -ForegroundColor Yellow

$frontendPath = Join-Path $scriptPath "Frontend"
if (Test-Path $frontendPath) {
    # Create web directory
    if (-not (Test-Path $WebPath)) {
        New-Item -ItemType Directory -Path $WebPath | Out-Null
    }
    
    # Copy frontend files
    Copy-Item -Path "$frontendPath\*" -Destination $WebPath -Recurse -Force
    
    Write-Host "  ✓ Frontend deployed to $WebPath" -ForegroundColor Green
} else {
    Write-Error "Frontend path not found at $frontendPath"
    exit 1
}

# Step 6: Configure IIS
Write-Host ""
Write-Host "[6/7] Configuring IIS..." -ForegroundColor Yellow

Import-Module WebAdministration

# Create Application Pool for API
$apiPoolName = "ResourcePlanProAPIPool"
if (-not (Test-Path "IIS:\AppPools\$apiPoolName")) {
    New-WebAppPool -Name $apiPoolName | Out-Null
    Set-ItemProperty "IIS:\AppPools\$apiPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$apiPoolName" -Name startMode -Value AlwaysRunning
    Write-Host "  ✓ Created application pool: $apiPoolName" -ForegroundColor Green
}

# Create Website for API
$apiSiteName = "ResourcePlanProAPI"
if (Get-Website -Name $apiSiteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $apiSiteName
}
New-Website -Name $apiSiteName -PhysicalPath $ApiPath -ApplicationPool $apiPoolName -Port $ApiPort | Out-Null
Write-Host "  ✓ Created API website: $apiSiteName on port $ApiPort" -ForegroundColor Green

# Create Application Pool for Frontend
$webPoolName = "ResourcePlanProWebPool"
if (-not (Test-Path "IIS:\AppPools\$webPoolName")) {
    New-WebAppPool -Name $webPoolName | Out-Null
    Write-Host "  ✓ Created application pool: $webPoolName" -ForegroundColor Green
}

# Create Website for Frontend
$webSiteName = "ResourcePlanProWeb"
if (Get-Website -Name $webSiteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $webSiteName
}
New-Website -Name $webSiteName -PhysicalPath $WebPath -ApplicationPool $webPoolName -Port $WebPort | Out-Null

# Set default document
Set-WebConfigurationProperty -Filter "//defaultDocument/files" -PSPath "IIS:\Sites\$webSiteName" -Name "." -Value @{value="login.html"}
Write-Host "  ✓ Created web website: $webSiteName on port $WebPort" -ForegroundColor Green

# Restart IIS
Write-Host "  Restarting IIS..." -ForegroundColor Gray
iisreset /noforce | Out-Null
Write-Host "  ✓ IIS restarted" -ForegroundColor Green

# Step 7: Verify Deployment
Write-Host ""
Write-Host "[7/7] Verifying deployment..." -ForegroundColor Yellow

Start-Sleep -Seconds 3

# Test API health endpoint
try {
    $apiUrl = "http://localhost:$ApiPort/api/health"
    $response = Invoke-WebRequest -Uri $apiUrl -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✓ API is responding" -ForegroundColor Green
    }
}
catch {
    Write-Warning "  ! Could not reach API health endpoint"
}

# Test Frontend
try {
    $webUrl = "http://localhost:$WebPort/login.html"
    $response = Invoke-WebRequest -Uri $webUrl -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✓ Frontend is accessible" -ForegroundColor Green
    }
}
catch {
    Write-Warning "  ! Could not reach frontend"
}

# Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Endpoint: http://localhost:$ApiPort" -ForegroundColor White
Write-Host "Frontend URL: http://localhost:$WebPort" -ForegroundColor White
Write-Host ""
Write-Host "Demo Login Credentials:" -ForegroundColor Yellow
Write-Host "  Username: jsmith" -ForegroundColor White
Write-Host "  Password: Password123!" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Configure SSL certificates for HTTPS" -ForegroundColor White
Write-Host "  2. Update Frontend API URL in config.js" -ForegroundColor White
Write-Host "  3. Review security settings in web.config" -ForegroundColor White
Write-Host "  4. Configure firewall rules" -ForegroundColor White
Write-Host "  5. Set up backup schedule" -ForegroundColor White
Write-Host ""
