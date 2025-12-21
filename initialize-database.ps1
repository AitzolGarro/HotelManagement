# Initialize Database for Hotel Reservation System
# This script properly initializes the SQLite database using Entity Framework

Write-Host "Hotel Reservation System - Database Initialization" -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green

# Stop any running application first
Write-Host "Please stop the running application (Ctrl+C) before running this script" -ForegroundColor Yellow
Write-Host "Press any key to continue once the application is stopped..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Remove existing database files
Write-Host "Removing existing database files..." -ForegroundColor Yellow
$dbFiles = @(
    "HotelReservationSystem\bin\Debug\net8.0\hotel_reservation_demo.db",
    "HotelReservationSystem\bin\Debug\net8.0\hotel_reservation_demo.db-shm",
    "HotelReservationSystem\bin\Debug\net8.0\hotel_reservation_demo.db-wal",
    "hotel_reservation_demo.db",
    "hotel_reservation_demo.db-shm",
    "hotel_reservation_demo.db-wal"
)

foreach ($file in $dbFiles) {
    if (Test-Path $file) {
        try {
            Remove-Item $file -Force
            Write-Host "Removed: $file" -ForegroundColor Green
        }
        catch {
            Write-Host "Could not remove: $file (may be in use)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "Database files removed. Now start the application using:" -ForegroundColor Cyan
Write-Host "  .\run-hotel-website-fixed.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Entity Framework will automatically:" -ForegroundColor Cyan
Write-Host "  1. Create the database schema" -ForegroundColor Gray
Write-Host "  2. Run migrations" -ForegroundColor Gray
Write-Host "  3. Seed demo data" -ForegroundColor Gray
Write-Host ""
Write-Host "The application should then work properly with data loaded." -ForegroundColor Green