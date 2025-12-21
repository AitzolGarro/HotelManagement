# Hotel Reservation System Website Launcher - Fixed Version
Write-Host "Hotel Reservation System Website (Fixed)" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

$exePath = ".\HotelReservationSystem\bin\Debug\net8.0\HotelReservationSystem.exe"
$workingDir = ".\HotelReservationSystem\bin\Debug\net8.0"

if (Test-Path $exePath) {
    Write-Host "Found compiled application" -ForegroundColor Green
    Write-Host ""
    Write-Host "Setting up environment..." -ForegroundColor Yellow
    
    # Set environment variables for proper configuration
    $env:JwtSettings__Secret = "HotelReservationSystemSecretKeyForJWTTokenGeneration2024!"
    $env:UseSqlite = "true"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    
    # Ensure Views are copied to output directory
    if (-not (Test-Path ".\HotelReservationSystem\bin\Debug\net8.0\Views")) {
        Write-Host "Copying Views to output directory..." -ForegroundColor Cyan
        Copy-Item -Path "HotelReservationSystem\Views" -Destination "HotelReservationSystem\bin\Debug\net8.0\" -Recurse -Force
    }
    
    # Database will be created automatically by Entity Framework
    Write-Host "Database will be initialized automatically by Entity Framework..." -ForegroundColor Cyan
    
    # Ensure route fix script is in place
    if (-not (Test-Path ".\HotelReservationSystem\bin\Debug\net8.0\wwwroot\js\route-fix.js")) {
        Write-Host "Setting up route fix for navigation..." -ForegroundColor Cyan
        # The route-fix.js file should already be created in the wwwroot/js directory
    }
    
    Write-Host "Launching website from correct directory..." -ForegroundColor Yellow
    Write-Host "Website will be available at:" -ForegroundColor Cyan
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
    
    # Change to the correct working directory and start the application
    Push-Location $workingDir
    try {
        & ".\HotelReservationSystem.exe"
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "Application not found at: $exePath" -ForegroundColor Red
    Write-Host ""
    Write-Host "To build the application, you need:" -ForegroundColor Yellow
    Write-Host "  1. Install .NET 8.0 SDK" -ForegroundColor Gray
    Write-Host "  2. Run: dotnet build HotelReservationSystem" -ForegroundColor Gray
    Write-Host "  3. Run: dotnet run --project HotelReservationSystem" -ForegroundColor Gray
}