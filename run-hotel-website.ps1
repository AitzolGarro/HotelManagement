# Hotel Reservation System Website Launcher
Write-Host "Hotel Reservation System Website" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

$exePath = ".\HotelReservationSystem\bin\Debug\net8.0\HotelReservationSystem.exe"

if (Test-Path $exePath) {
    Write-Host "Found compiled application" -ForegroundColor Green
    Write-Host ""
    Write-Host "Launching website..." -ForegroundColor Yellow
    Write-Host "Website will be available at:" -ForegroundColor Cyan
    Write-Host "  https://localhost:7001" -ForegroundColor White
    Write-Host "  http://localhost:5000" -ForegroundColor White
    Write-Host ""
    Write-Host "Features available:" -ForegroundColor Cyan
    Write-Host "  - Hotel and room management" -ForegroundColor Gray
    Write-Host "  - Reservation system with calendar view" -ForegroundColor Gray
    Write-Host "  - Real-time notifications via SignalR" -ForegroundColor Gray
    Write-Host "  - Dashboard with analytics" -ForegroundColor Gray
    Write-Host "  - Booking.com integration" -ForegroundColor Gray
    Write-Host "  - Reporting and audit trails" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
    Write-Host ""
    
    # Start the application
    & $exePath
} else {
    Write-Host "Application not found at: $exePath" -ForegroundColor Red
    Write-Host ""
    Write-Host "To build the application, you need:" -ForegroundColor Yellow
    Write-Host "  1. Install .NET 8.0 SDK" -ForegroundColor Gray
    Write-Host "  2. Run: dotnet build HotelReservationSystem" -ForegroundColor Gray
    Write-Host "  3. Run: dotnet run --project HotelReservationSystem" -ForegroundColor Gray
}