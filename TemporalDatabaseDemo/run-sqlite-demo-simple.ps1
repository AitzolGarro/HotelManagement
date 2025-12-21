# Simple Temporal Database Demo Runner
# This script demonstrates temporal database concepts using SQLite

param(
    [string]$DatabasePath = "temporal_demo.db",
    [switch]$Reset = $false
)

Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    TEMPORAL DATABASE DEMO - SQLITE VERSION                  ║" -ForegroundColor Cyan
Write-Host "║                                                                              ║" -ForegroundColor Cyan
Write-Host "║  This demo showcases temporal database concepts using SQLite with triggers   ║" -ForegroundColor Cyan
Write-Host "║  for automatic history tracking. No SQL Server installation required!       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Handle reset
if ($Reset) {
    if (Test-Path $DatabasePath) {
        Remove-Item $DatabasePath -Force
        Write-Host "✅ Database reset successfully" -ForegroundColor Green
    }
    exit 0
}

# Check if we need to download SQLite
$sqliteExe = "sqlite3.exe"
if (-not (Test-Path $sqliteExe)) {
    Write-Host "📥 Downloading SQLite..." -ForegroundColor Yellow
    try {
        $url = "https://www.sqlite.org/2024/sqlite-tools-win-x64-3450300.zip"
        Invoke-WebRequest -Uri $url -OutFile "sqlite-tools.zip" -UseBasicParsing
        Expand-Archive -Path "sqlite-tools.zip" -DestinationPath "." -Force
        $extracted = Get-ChildItem -Recurse -Name "sqlite3.exe" | Select-Object -First 1
        if ($extracted) {
            Copy-Item $extracted -Destination $sqliteExe
            Remove-Item "sqlite-tools.zip" -Force
            Remove-Item "sqlite-tools-*" -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "✅ SQLite downloaded successfully" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "❌ Failed to download SQLite. Please install SQLite manually." -ForegroundColor Red
        exit 1
    }
}

# Define script paths
$setupScripts = @(
    @{ File = "setup/sqlite-setup.sql"; Description = "Creating temporal tables" },
    @{ File = "setup/sqlite-sample-data.sql"; Description = "Inserting sample data" },
    @{ File = "setup/sqlite-simulate-changes.sql"; Description = "Simulating changes" }
)

$demoScript = "queries/sqlite-temporal-queries.sql"

# Execute setup scripts
Write-Host "🚀 SETUP PHASE" -ForegroundColor Magenta
Write-Host "===============" -ForegroundColor Magenta

foreach ($script in $setupScripts) {
    Write-Host "🔄 $($script.Description)..." -ForegroundColor Yellow
    
    if (Test-Path $script.File) {
        try {
            & $sqliteExe $DatabasePath ".read `"$($script.File)`""
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ $($script.Description) completed" -ForegroundColor Green
            } else {
                throw "SQLite execution failed"
            }
        }
        catch {
            Write-Host "❌ $($script.Description) failed: $_" -ForegroundColor Red
        }
    } else {
        Write-Host "⚠️  Script file not found: $($script.File)" -ForegroundColor Yellow
    }
}

# Execute demo queries
Write-Host ""
Write-Host "🎭 DEMO PHASE" -ForegroundColor Magenta
Write-Host "=============" -ForegroundColor Magenta

if (Test-Path $demoScript) {
    Write-Host "🔄 Running temporal query demonstrations..." -ForegroundColor Yellow
    try {
        & $sqliteExe $DatabasePath ".read `"$demoScript`""
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Demo queries completed successfully" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "❌ Demo queries failed: $_" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  Demo script not found: $demoScript" -ForegroundColor Yellow
}

# Final summary
Write-Host ""
Write-Host "🎉 TEMPORAL DATABASE DEMO COMPLETED!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""
Write-Host "📊 What was demonstrated:" -ForegroundColor Cyan
Write-Host "   • Temporal-like functionality using SQLite triggers" -ForegroundColor Gray
Write-Host "   • Complete audit trails and change tracking" -ForegroundColor Gray
Write-Host "   • Historical data analysis and reporting" -ForegroundColor Gray
Write-Host "   • Business intelligence with temporal context" -ForegroundColor Gray
Write-Host ""
Write-Host "📁 Database file created: $DatabasePath" -ForegroundColor Yellow
Write-Host "   You can open this with any SQLite browser tool" -ForegroundColor Gray
Write-Host ""
Write-Host "Thank you for exploring temporal databases! 🚀" -ForegroundColor Green