# ResourcePlan Pro - Development Startup Script
# Starts both Backend API and Frontend for local development

param(
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory=$false)]
    [int]$ApiPort = 5001,
    
    [Parameter(Mandatory=$false)]
    [int]$WebPort = 8080
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "ResourcePlan Pro - Development Startup" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Start Backend API
Write-Host "[1/2] Starting Backend API..." -ForegroundColor Yellow
$backendPath = Join-Path $scriptPath "Backend"

if (Test-Path $backendPath) {
    Push-Location $backendPath
    
    if (-not $SkipBuild) {
        Write-Host "  Building backend..." -ForegroundColor Gray
        dotnet build | Out-Null
    }
    
    Write-Host "  Starting API on port $ApiPort..." -ForegroundColor Gray
    $apiJob = Start-Job -ScriptBlock {
        param($path, $port)
        Set-Location $path
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:ASPNETCORE_URLS = "https://localhost:$port;http://localhost:$($port+1000)"
        dotnet run
    } -ArgumentList $backendPath, $ApiPort
    
    Pop-Location
    Write-Host "  ✓ API started (Job ID: $($apiJob.Id))" -ForegroundColor Green
} else {
    Write-Error "Backend path not found: $backendPath"
    exit 1
}

Start-Sleep -Seconds 3

# Start Frontend Server
Write-Host ""
Write-Host "[2/2] Starting Frontend Server..." -ForegroundColor Yellow
$frontendPath = Join-Path $scriptPath "Frontend"

if (Test-Path $frontendPath) {
    # Check if Python is available for simple HTTP server
    $pythonCmd = $null
    if (Get-Command python3 -ErrorAction SilentlyContinue) {
        $pythonCmd = "python3"
    } elseif (Get-Command python -ErrorAction SilentlyContinue) {
        $pythonCmd = "python"
    }
    
    if ($pythonCmd) {
        Write-Host "  Starting web server on port $WebPort..." -ForegroundColor Gray
        $webJob = Start-Job -ScriptBlock {
            param($path, $port, $cmd)
            Set-Location $path
            & $cmd -m http.server $port
        } -ArgumentList $frontendPath, $WebPort, $pythonCmd
        
        Write-Host "  ✓ Frontend started (Job ID: $($webJob.Id))" -ForegroundColor Green
    } else {
        Write-Warning "  ! Python not found. Please serve frontend manually."
        Write-Host "    You can use: cd Frontend && python -m http.server $WebPort" -ForegroundColor Yellow
    }
} else {
    Write-Error "Frontend path not found: $frontendPath"
}

# Display URLs
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Services Started" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend API:  https://localhost:$ApiPort" -ForegroundColor White
Write-Host "API Docs:     https://localhost:$ApiPort/swagger" -ForegroundColor White
if ($pythonCmd) {
    Write-Host "Frontend:     http://localhost:$WebPort" -ForegroundColor White
}
Write-Host ""
Write-Host "Demo Credentials:" -ForegroundColor Yellow
Write-Host "  Username: jsmith" -ForegroundColor White
Write-Host "  Password: Password123!" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Gray
Write-Host ""

# Keep script running and monitor jobs
try {
    while ($true) {
        Start-Sleep -Seconds 2
        
        # Check if API job is still running
        $apiState = Get-Job -Id $apiJob.Id -ErrorAction SilentlyContinue
        if ($apiState -and $apiState.State -ne "Running") {
            Write-Host ""
            Write-Warning "API job stopped unexpectedly"
            break
        }
        
        # Check if web job is still running (if started)
        if ($webJob) {
            $webState = Get-Job -Id $webJob.Id -ErrorAction SilentlyContinue
            if ($webState -and $webState.State -ne "Running") {
                Write-Host ""
                Write-Warning "Frontend job stopped unexpectedly"
                break
            }
        }
    }
}
finally {
    Write-Host ""
    Write-Host "Stopping services..." -ForegroundColor Yellow
    
    # Stop all jobs
    Get-Job | Stop-Job
    Get-Job | Remove-Job
    
    Write-Host "All services stopped." -ForegroundColor Green
}
