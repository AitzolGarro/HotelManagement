# Hotel Reservation System - Complete Demo Setup
# This script sets up the full hotel reservation system with SQLite for client demonstrations

Write-Host "Hotel Reservation System - Demo Setup" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

$appPath = ".\HotelReservationSystem\bin\Debug\net8.0\HotelReservationSystem.exe"
if (-not (Test-Path $appPath)) {
    Write-Host "Application executable not found" -ForegroundColor Red
    Write-Host "Expected: $appPath" -ForegroundColor Gray
    Write-Host "Please ensure the application is built first." -ForegroundColor Yellow
    exit 1
}

# Setup SQLite database
Write-Host "Setting up SQLite database..." -ForegroundColor Yellow

# Download SQLite if not available
if (-not (Test-Path "sqlite3.exe")) {
    Write-Host "Downloading SQLite..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri "https://www.sqlite.org/2024/sqlite-tools-win-x64-3450300.zip" -OutFile "sqlite-tools.zip" -UseBasicParsing
        Expand-Archive "sqlite-tools.zip" -DestinationPath "." -Force
        $sqliteFiles = Get-ChildItem -Recurse -Name "sqlite3.exe"
        if ($sqliteFiles) {
            Copy-Item $sqliteFiles[0] -Destination "sqlite3.exe"
        }
        Remove-Item "sqlite-tools.zip" -Force
        Remove-Item "sqlite-tools-*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "SQLite downloaded successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to download SQLite: $_" -ForegroundColor Red
        exit 1
    }
}

# Copy configuration files
Write-Host "Configuring application for SQLite..." -ForegroundColor Yellow

$configFiles = @(
    @{ Source = ".\HotelReservationSystem\appsettings.json"; Dest = ".\HotelReservationSystem\bin\Debug\net8.0\appsettings.json" },
    @{ Source = ".\HotelReservationSystem\appsettings.Development.json"; Dest = ".\HotelReservationSystem\bin\Debug\net8.0\appsettings.Development.json" }
)

foreach ($config in $configFiles) {
    if (Test-Path $config.Source) {
        Copy-Item $config.Source $config.Dest -Force
        Write-Host "Copied $($config.Source)" -ForegroundColor Green
    }
}

# Initialize database with schema and sample data
Write-Host "Initializing database with sample data..." -ForegroundColor Yellow

$dbPath = "hotel_reservation_demo.db"

# Remove existing database to start fresh
if (Test-Path $dbPath) {
    Remove-Item $dbPath -Force
    Write-Host "Removed existing database" -ForegroundColor Gray
}

# The application will create the database schema automatically via Entity Framework migrations
Write-Host "Database will be initialized on first run by Entity Framework" -ForegroundColor Cyan

Write-Host ""
Write-Host "Demo setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Run: .\run-hotel-demo-sqlite.ps1" -ForegroundColor White
Write-Host "   2. Open: https://localhost:7001" -ForegroundColor White
Write-Host "   3. Login with credentials from DEMO_INFO.txt" -ForegroundColor White
Write-Host ""
Write-Host "Or use the quick start: .\quick-start-demo.ps1" -ForegroundColor Yellow
Write-Host ""
Write-Host "Full demo information available in: DEMO_INFO.txt" -ForegroundColor Gray