# ResourcePlan Pro - API Testing Script
# Tests all major API endpoints to verify functionality

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiBaseUrl = "https://localhost:7001/api",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "jsmith",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "Password123!"
)

$ErrorActionPreference = "Continue"
$testResults = @()

function Write-TestResult {
    param($TestName, $Success, $Message = "")
    
    $result = @{
        Test = $TestName
        Success = $Success
        Message = $Message
        Timestamp = Get-Date
    }
    
    $script:testResults += $result
    
    if ($Success) {
        Write-Host "  ✓ $TestName" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $TestName - $Message" -ForegroundColor Red
    }
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "ResourcePlan Pro - API Testing" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "API URL: $ApiBaseUrl" -ForegroundColor White
Write-Host ""

# Disable SSL certificate validation for testing
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# Test 1: Health Check
Write-Host "[1] Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/health" -Method Get -ErrorAction Stop
    Write-TestResult "Health Check" $true
}
catch {
    Write-TestResult "Health Check" $false $_.Exception.Message
}

# Test 2: Authentication - Login
Write-Host ""
Write-Host "[2] Testing Authentication..." -ForegroundColor Yellow
$token = $null
try {
    $loginBody = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -ErrorAction Stop
    
    if ($response.success -and $response.token) {
        $token = $response.token
        Write-TestResult "Login" $true
    } else {
        Write-TestResult "Login" $false "No token received"
    }
}
catch {
    Write-TestResult "Login" $false $_.Exception.Message
}

if (-not $token) {
    Write-Host ""
    Write-Host "Cannot continue testing without authentication token" -ForegroundColor Red
    exit 1
}

$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 3: Token Validation
Write-Host ""
Write-Host "[3] Testing Token Validation..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/auth/validate" -Method Get -Headers $headers -ErrorAction Stop
    Write-TestResult "Token Validation" $true
}
catch {
    Write-TestResult "Token Validation" $false $_.Exception.Message
}

# Test 4: Dashboard
Write-Host ""
Write-Host "[4] Testing Dashboard..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/dashboard" -Method Get -Headers $headers -ErrorAction Stop
    Write-TestResult "Get Dashboard" $response.success
}
catch {
    Write-TestResult "Get Dashboard" $false $_.Exception.Message
}

# Test 5: Projects
Write-Host ""
Write-Host "[5] Testing Projects..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/projects" -Method Get -Headers $headers -ErrorAction Stop
    $projectCount = $response.data.Count
    Write-TestResult "Get Projects" $response.success "Found $projectCount projects"
    
    if ($projectCount -gt 0) {
        $projectId = $response.data[0].projectId
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/projects/$projectId" -Method Get -Headers $headers -ErrorAction Stop
        Write-TestResult "Get Project by ID" $response.success
    }
}
catch {
    Write-TestResult "Get Projects" $false $_.Exception.Message
}

# Test 6: Employees
Write-Host ""
Write-Host "[6] Testing Employees..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/employees" -Method Get -Headers $headers -ErrorAction Stop
    $employeeCount = $response.data.Count
    Write-TestResult "Get Employees" $response.success "Found $employeeCount employees"
}
catch {
    Write-TestResult "Get Employees" $false $_.Exception.Message
}

# Test 7: Departments
Write-Host ""
Write-Host "[7] Testing Departments..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/departments" -Method Get -Headers $headers -ErrorAction Stop
    $deptCount = $response.data.Count
    Write-TestResult "Get Departments" $response.success "Found $deptCount departments"
}
catch {
    Write-TestResult "Get Departments" $false $_.Exception.Message
}

# Test 8: Resources - Timeline
Write-Host ""
Write-Host "[8] Testing Resources..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/resources/timeline" -Method Get -Headers $headers -ErrorAction Stop
    Write-TestResult "Get Resource Timeline" $response.success
}
catch {
    Write-TestResult "Get Resource Timeline" $false $_.Exception.Message
}

# Test 9: Conflicts
Write-Host ""
Write-Host "[9] Testing Conflicts..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/dashboard/conflicts" -Method Get -Headers $headers -ErrorAction Stop
    $conflictCount = $response.data.Count
    Write-TestResult "Get Conflicts" $response.success "Found $conflictCount conflicts"
}
catch {
    Write-TestResult "Get Conflicts" $false $_.Exception.Message
}

# Test 10: Statistics
Write-Host ""
Write-Host "[10] Testing Statistics..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/dashboard/stats" -Method Get -Headers $headers -ErrorAction Stop
    Write-TestResult "Get Statistics" $response.success
}
catch {
    Write-TestResult "Get Statistics" $false $_.Exception.Message
}

# Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $passedTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host ""

if ($failedTests -gt 0) {
    Write-Host "Failed Tests:" -ForegroundColor Red
    $testResults | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "  - $($_.Test): $($_.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

$successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 70) { "Yellow" } else { "Red" })
Write-Host ""

# Export results
$resultsPath = ".\TestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$testResults | ConvertTo-Json | Set-Content $resultsPath
Write-Host "Detailed results saved to: $resultsPath" -ForegroundColor Gray
