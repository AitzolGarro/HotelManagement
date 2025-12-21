# Hotel Reservation System - SQLite Demo Launcher
# This script runs the hotel reservation system with SQLite for easy client demonstrations

Write-Host "Hotel Reservation System - SQLite Demo" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""

# Check if the application exists
$appPath = ".\HotelReservationSystem\bin\Debug\net8.0\HotelReservationSystem.exe"
if (-not (Test-Path $appPath)) {
    Write-Host "Application not found at: $appPath" -ForegroundColor Red
    Write-Host "Please ensure the application is built first." -ForegroundColor Yellow
    exit 1
}

# Copy SQLite configuration
$configSource = ".\HotelReservationSystem\appsettings.json"
$configDest = ".\HotelReservationSystem\bin\Debug\net8.0\appsettings.json"

if (Test-Path $configSource) {
    Copy-Item $configSource $configDest -Force
    Write-Host "SQLite configuration applied" -ForegroundColor Green
} else {
    Write-Host "Configuration file not found, using defaults" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Starting Hotel Reservation System with SQLite..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Demo Features Available:" -ForegroundColor Cyan
Write-Host "   • Complete hotel and room management" -ForegroundColor Gray
Write-Host "   • Full reservation system with calendar" -ForegroundColor Gray
Write-Host "   • Real-time notifications via SignalR" -ForegroundColor Gray
Write-Host "   • Dashboard with analytics and KPIs" -ForegroundColor Gray
Write-Host "   • User authentication and authorization" -ForegroundColor Gray
Write-Host "   • Comprehensive reporting system" -ForegroundColor Gray
Write-Host "   • SQLite database (no SQL Server required)" -ForegroundColor Gray
Write-Host ""
Write-Host "Website will be available at:" -ForegroundColor Cyan
Write-Host "   • https://localhost:7001 (HTTPS)" -ForegroundColor White
Write-Host "   • http://localhost:5000 (HTTP)" -ForegroundColor White
Write-Host ""
Write-Host "Perfect for client demonstrations!" -ForegroundColor Yellow
Write-Host "   • No database server installation required" -ForegroundColor Gray
Write-Host "   • Portable SQLite database file" -ForegroundColor Gray
Write-Host "   • Easy to switch back to SQL Server for production" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Set environment to use SQLite
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DatabaseProvider = "Sqlite"
$env:ConnectionStrings__SqliteConnection = "Data Source=hotel_reservation_demo.db"

# Start the application
try {
    & $appPath
}
catch {
    Write-Host "Failed to start application: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   • Ensure .NET 8.0 Runtime is installed" -ForegroundColor Gray
    Write-Host "   • Check if ports 5000/7001 are available" -ForegroundColor Gray
    Write-Host "   • Verify application files are not corrupted" -ForegroundColor Gray
}