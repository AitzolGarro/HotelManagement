# Temporal Database Demo - Simple Runner
Write-Host "Temporal Database Demo - SQLite Version" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

$dbFile = "temporal_demo.db"

# Download SQLite if needed
if (-not (Test-Path "sqlite3.exe")) {
    Write-Host "Downloading SQLite..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://www.sqlite.org/2024/sqlite-tools-win-x64-3450300.zip" -OutFile "sqlite.zip"
    Expand-Archive "sqlite.zip" -DestinationPath "." -Force
    $sqliteFiles = Get-ChildItem -Recurse -Name "sqlite3.exe"
    if ($sqliteFiles) {
        Copy-Item $sqliteFiles[0] -Destination "sqlite3.exe"
    }
    Remove-Item "sqlite.zip" -Force
    Remove-Item "sqlite-tools-*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "SQLite ready!" -ForegroundColor Green
}

Write-Host "Setting up temporal database..." -ForegroundColor Yellow

# Setup database
& "./sqlite3.exe" $dbFile ".read TemporalDatabaseDemo/setup/sqlite-setup.sql"
Write-Host "Tables created" -ForegroundColor Green

& "./sqlite3.exe" $dbFile ".read TemporalDatabaseDemo/setup/sqlite-sample-data.sql"  
Write-Host "Sample data inserted" -ForegroundColor Green

& "./sqlite3.exe" $dbFile ".read TemporalDatabaseDemo/setup/sqlite-simulate-changes.sql"
Write-Host "Changes simulated" -ForegroundColor Green

Write-Host ""
Write-Host "Running temporal queries demo..." -ForegroundColor Yellow
& "./sqlite3.exe" $dbFile ".read TemporalDatabaseDemo/queries/sqlite-temporal-queries.sql"

Write-Host ""
Write-Host "Demo completed! Database file: $dbFile" -ForegroundColor Green