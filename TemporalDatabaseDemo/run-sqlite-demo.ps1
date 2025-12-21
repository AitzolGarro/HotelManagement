# Temporal Database Demo - PowerShell SQLite Version
# This script runs the temporal database demo using SQLite without requiring Python

param(
    [string]$DatabasePath = "temporal_demo.db",
    [switch]$Interactive = $true,
    [switch]$Reset = $false
)

# Function to download SQLite if not available
function Get-SQLite {
    $sqliteExe = "sqlite3.exe"
    
    if (Test-Path $sqliteExe) {
        return $sqliteExe
    }
    
    Write-Host "📥 SQLite not found. Downloading SQLite..." -ForegroundColor Yellow
    
    try {
        # Download SQLite command-line tools
        $url = "https://www.sqlite.org/2024/sqlite-tools-win-x64-3450300.zip"
        $zipFile = "sqlite-tools.zip"
        
        Invoke-WebRequest -Uri $url -OutFile $zipFile -UseBasicParsing
        
        # Extract SQLite
        Expand-Archive -Path $zipFile -DestinationPath "." -Force
        
        # Find the extracted sqlite3.exe
        $extractedSqlite = Get-ChildItem -Recurse -Name "sqlite3.exe" | Select-Object -First 1
        
        if ($extractedSqlite) {
            Copy-Item $extractedSqlite -Destination $sqliteExe
            Remove-Item $zipFile -Force
            Remove-Item "sqlite-tools-*" -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "✅ SQLite downloaded successfully" -ForegroundColor Green
            return $sqliteExe
        } else {
            throw "SQLite executable not found in downloaded package"
        }
    }
    catch {
        Write-Host "❌ Failed to download SQLite: $_" -ForegroundColor Red
        Write-Host "Please download SQLite manually from https://www.sqlite.org/download.html" -ForegroundColor Yellow
        exit 1
    }
}
}

# Function to execute SQL file
function Invoke-SQLiteScript {
    param(
        [string]$SqliteExe,
        [string]$DatabasePath,
        [string]$ScriptPath,
        [string]$Description
    )
    
    Write-Host "🔄 $Description..." -ForegroundColor Yellow
    
    try {
        if (-not (Test-Path $ScriptPath)) {
            throw "Script file not found: $ScriptPath"
        }
        
        # Execute the SQL script
        $result = & $SqliteExe $DatabasePath ".read `"$ScriptPath`""
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $Description completed successfully" -ForegroundColor Green
            
            # Show some output if available
            if ($result) {
                $result | ForEach-Object {
                    if ($_ -and $_.Trim()) {
                        Write-Host "   $_" -ForegroundColor Gray
                    }
                }
            }
        } else {
            throw "SQLite execution failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Host "❌ $Description failed: $_" -ForegroundColor Red
        throw
    }
}

# Function to show menu
function Show-Menu {
    Write-Host ""
    Write-Host "📋 Demo Options:" -ForegroundColor Cyan
    Write-Host "1. Run complete setup and demo"
    Write-Host "2. Run setup only"
    Write-Host "3. Run demo queries only"
    Write-Host "4. Reset database (clean start)"
    Write-Host "5. Exit"
    Write-Host ""
    
    do {
        $choice = Read-Host "Select an option (1-5)"
    } while ($choice -notmatch '^[1-5]$')
    
    return $choice
}

# Function to wait for user
function Wait-ForUser {
    param([string]$Message = "Press Enter to continue...")
    
    Write-Host ""
    Write-Host $Message -ForegroundColor Yellow
    Read-Host
}

# Main execution
try {
    # Print header
    Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    TEMPORAL DATABASE DEMO - SQLITE VERSION                  ║" -ForegroundColor Cyan
    Write-Host "║                                                                              ║" -ForegroundColor Cyan
    Write-Host "║  This demo showcases temporal database concepts using SQLite with triggers   ║" -ForegroundColor Cyan
    Write-Host "║  for automatic history tracking. No SQL Server installation required!       ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    # Handle reset option
    if ($Reset) {
        if (Test-Path $DatabasePath) {
            Remove-Item $DatabasePath -Force
            Write-Host "✅ Database $DatabasePath has been reset" -ForegroundColor Green
        } else {
            Write-Host "ℹ️  Database $DatabasePath doesn't exist" -ForegroundColor Blue
        }
        exit 0
    }
    
    # Get SQLite executable
    $sqliteExe = Get-SQLite
    
    # Define script paths
    $demoRoot = $PSScriptRoot
    $setupPath = Join-Path $demoRoot "setup"
    $queriesPath = Join-Path $demoRoot "queries"
    
    $setupScripts = @(
        @{ File = "sqlite-setup.sql"; Description = "Creating SQLite temporal tables with history tracking" },
        @{ File = "sqlite-sample-data.sql"; Description = "Inserting sample data" },
        @{ File = "sqlite-simulate-changes.sql"; Description = "Simulating data changes over time" }
    )
    
    $demoScripts = @(
        @{ File = "sqlite-temporal-queries.sql"; Description = "Running temporal query demonstrations" }
    )
    
    # Interactive menu or direct execution
    if ($Interactive) {
        $choice = Show-Menu
        
        switch ($choice) {
            "1" { $RunSetup = $true; $RunDemo = $true }
            "2" { $RunSetup = $true; $RunDemo = $false }
            "3" { $RunSetup = $false; $RunDemo = $true }
            "4" { 
                if (Test-Path $DatabasePath) {
                    Remove-Item $DatabasePath -Force
                    Write-Host "✅ Database reset successfully" -ForegroundColor Green
                } else {
                    Write-Host "ℹ️  Database doesn't exist" -ForegroundColor Blue
                }
                exit 0
            }
            "5" { 
                Write-Host "Goodbye! 👋" -ForegroundColor Green
                exit 0 
            }
        }
    } else {
        $RunSetup = $true
        $RunDemo = $true
    }
    
    # Setup Phase
    if ($RunSetup) {
        Write-Host ""
        Write-Host "🚀 SETUP PHASE: Creating temporal database and sample data" -ForegroundColor Magenta
        Write-Host "=========================================================" -ForegroundColor Magenta
        
        foreach ($script in $setupScripts) {
            $scriptPath = Join-Path $setupPath $script.File
            Invoke-SQLiteScript -SqliteExe $sqliteExe -DatabasePath $DatabasePath -ScriptPath $scriptPath -Description $script.Description
            
            if ($Interactive -and $script.File -eq "sqlite-sample-data.sql") {
                Wait-ForUser "Setup phase 1 complete. Ready to simulate changes?"
            }
        }
        
        Write-Host ""
        Write-Host "✅ Setup phase completed successfully!" -ForegroundColor Green
        Write-Host "   • SQLite database created with temporal-like functionality" -ForegroundColor Gray
        Write-Host "   • 4 main tables with automatic history tracking via triggers" -ForegroundColor Gray
        Write-Host "   • Sample data inserted and changes simulated" -ForegroundColor Gray
        
        if ($Interactive) {
            Wait-ForUser "Setup complete! Ready to run the demo?"
        }
    }
    
    # Demo Phase
    if ($RunDemo) {
        Write-Host ""
        Write-Host "🎭 DEMO PHASE: Exploring temporal database capabilities" -ForegroundColor Magenta
        Write-Host "=====================================================" -ForegroundColor Magenta
        
        # Check if database exists
        if (-not (Test-Path $DatabasePath)) {
            Write-Host "⚠️  Database doesn't exist. Running setup first..." -ForegroundColor Yellow
            foreach ($script in $setupScripts) {
                $scriptPath = Join-Path $setupPath $script.File
                Invoke-SQLiteScript -SqliteExe $sqliteExe -DatabasePath $DatabasePath -ScriptPath $scriptPath -Description $script.Description
            }
        }
        
        foreach ($script in $demoScripts) {
            $scriptPath = Join-Path $queriesPath $script.File
            Invoke-SQLiteScript -SqliteExe $sqliteExe -DatabasePath $DatabasePath -ScriptPath $scriptPath -Description $script.Description
            
            if ($Interactive) {
                Wait-ForUser "Demo section complete. Continue?"
            }
        }
        
        Write-Host ""
        Write-Host "✅ Demo phase completed successfully!" -ForegroundColor Green
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
    Write-Host "   • Compliance and security auditing capabilities" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔗 Next steps:" -ForegroundColor Cyan
    Write-Host "   • Explore the SQLite database file: $DatabasePath" -ForegroundColor Gray
    Write-Host "   • Open with any SQLite browser/tool" -ForegroundColor Gray
    Write-Host "   • Review the SQL scripts for learning" -ForegroundColor Gray
    Write-Host "   • Try your own temporal queries" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📁 Database file: $(Resolve-Path $DatabasePath -ErrorAction SilentlyContinue)" -ForegroundColor Yellow
    Write-Host "📁 Scripts location: $demoRoot" -ForegroundColor Yellow
    
}
catch {
    Write-Host ""
    Write-Host "❌ Demo execution failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "   • Ensure you have internet access (for SQLite download)" -ForegroundColor Gray
    Write-Host "   • Check file permissions in the demo directory" -ForegroundColor Gray
    Write-Host "   • Verify SQL script files exist" -ForegroundColor Gray
    
    exit 1
}

Write-Host ""
Write-Host "Thank you for exploring temporal databases! 🚀" -ForegroundColor Green