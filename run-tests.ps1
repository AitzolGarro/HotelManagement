# Hotel Reservation System Test Runner
# PowerShell script to run all tests with proper configuration

param(
    [string]$Configuration = "Debug",
    [string]$TestFilter = "",
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [switch]$Watch = $false
)

Write-Host "Hotel Reservation System Test Runner" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Define paths
$ProjectRoot = $PSScriptRoot
$TestProject = Join-Path $ProjectRoot "HotelReservationSystem.Tests"
$MainProject = Join-Path $ProjectRoot "HotelReservationSystem"
$CoverageFile = Join-Path $ProjectRoot "coverage.xml"
$CoverageReport = Join-Path $ProjectRoot "coverage-report"

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET version: $dotnetVersion" -ForegroundColor Cyan
} catch {
    Write-Error "dotnet CLI not found. Please install .NET SDK."
    exit 1
}

# Build the solution first
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
try {
    dotnet build $ProjectRoot --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "Build successful!" -ForegroundColor Green
} catch {
    Write-Error "Build failed: $_"
    exit 1
}

# Prepare test command
$testCommand = @("test", $TestProject, "--configuration", $Configuration, "--no-build")

# Add test filter if specified
if ($TestFilter) {
    $testCommand += "--filter"
    $testCommand += $TestFilter
    Write-Host "Running tests with filter: $TestFilter" -ForegroundColor Cyan
}

# Add coverage collection if requested
if ($Coverage) {
    Write-Host "Collecting code coverage..." -ForegroundColor Cyan
    $testCommand += "--collect:XPlat Code Coverage"
    $testCommand += "--settings"
    $testCommand += "coverlet.runsettings"
}

# Add verbose output if requested
if ($Verbose) {
    $testCommand += "--verbosity"
    $testCommand += "detailed"
}

# Add watch mode if requested
if ($Watch) {
    $testCommand += "--watch"
    Write-Host "Running in watch mode. Press Ctrl+C to exit." -ForegroundColor Cyan
}

# Run the tests
Write-Host "`nRunning tests..." -ForegroundColor Yellow
Write-Host "Command: dotnet $($testCommand -join ' ')" -ForegroundColor Gray

try {
    & dotnet @testCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nAll tests passed!" -ForegroundColor Green
        
        # Generate coverage report if coverage was collected
        if ($Coverage -and !$Watch) {
            Write-Host "`nGenerating coverage report..." -ForegroundColor Yellow
            
            # Find the coverage file
            $coverageFiles = Get-ChildItem -Path $TestProject -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending
            
            if ($coverageFiles.Count -gt 0) {
                $latestCoverage = $coverageFiles[0].FullName
                Write-Host "Coverage file found: $latestCoverage" -ForegroundColor Cyan
                
                # Try to generate HTML report using reportgenerator
                try {
                    dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null
                    reportgenerator "-reports:$latestCoverage" "-targetdir:$CoverageReport" "-reporttypes:Html"
                    Write-Host "Coverage report generated at: $CoverageReport" -ForegroundColor Green
                } catch {
                    Write-Warning "Could not generate HTML coverage report. Install reportgenerator tool: dotnet tool install --global dotnet-reportgenerator-globaltool"
                }
            } else {
                Write-Warning "No coverage file found"
            }
        }
    } else {
        Write-Host "`nSome tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Error "Test execution failed: $_"
    exit 1
}

# Display summary
Write-Host "`nTest Summary:" -ForegroundColor Yellow
Write-Host "=============" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
if ($TestFilter) {
    Write-Host "Filter: $TestFilter" -ForegroundColor Cyan
}
Write-Host "Coverage: $(if ($Coverage) { 'Enabled' } else { 'Disabled' })" -ForegroundColor Cyan
Write-Host "Verbose: $(if ($Verbose) { 'Enabled' } else { 'Disabled' })" -ForegroundColor Cyan

Write-Host "`nTest runner completed successfully!" -ForegroundColor Green