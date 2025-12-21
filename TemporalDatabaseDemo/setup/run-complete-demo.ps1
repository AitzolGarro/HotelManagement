# Temporal Database Demo - Complete Setup and Demo Runner
# PowerShell script to set up and run the complete temporal database demonstration

param(
    [string]$ServerInstance = "(localdb)\mssqllocaldb",
    [switch]$SkipSetup = $false,
    [switch]$InteractiveMode = $false,
    [switch]$QuickDemo = $false
)

Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    TEMPORAL DATABASE DEMO RUNNER                            ║" -ForegroundColor Cyan
Write-Host "║                                                                              ║" -ForegroundColor Cyan
Write-Host "║  This script sets up and runs a complete demonstration of SQL Server        ║" -ForegroundColor Cyan
Write-Host "║  temporal tables, showcasing time-travel queries and audit capabilities.    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Set error action preference
$ErrorActionPreference = "Stop"

# Define paths
$DemoRoot = Split-Path -Parent $PSScriptRoot
$SetupPath = Join-Path $DemoRoot "setup"
$QueriesPath = Join-Path $DemoRoot "queries"

# Define script files in execution order
$SetupScripts = @(
    "01-create-database.sql",
    "02-create-temporal-tables.sql", 
    "03-insert-sample-data.sql",
    "04-simulate-changes.sql"
)

$DemoScripts = @(
    "01-basic-temporal-queries.sql",
    "02-advanced-scenarios.sql",
    "03-demo-presentation.sql"
)

function Test-SqlConnection {
    param([string]$ServerInstance)
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = "Server=$ServerInstance;Integrated Security=true;Connection Timeout=10"
        $connection.Open()
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Execute-SqlScript {
    param(
        [string]$ScriptPath,
        [string]$ServerInstance,
        [string]$Description
    )
    
    Write-Host "🔄 $Description..." -ForegroundColor Yellow
    
    try {
        $result = sqlcmd -S $ServerInstance -i $ScriptPath -b
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $Description completed successfully" -ForegroundColor Green
        } else {
            throw "SQL script execution failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Host "❌ $Description failed: $_" -ForegroundColor Red
        throw
    }
}

function Show-Menu {
    Write-Host ""
    Write-Host "📋 Demo Options:" -ForegroundColor Cyan
    Write-Host "1. Run complete setup and demo"
    Write-Host "2. Run setup only"
    Write-Host "3. Run demo queries only"
    Write-Host "4. Run interactive presentation"
    Write-Host "5. Exit"
    Write-Host ""
    
    do {
        $choice = Read-Host "Select an option (1-5)"
    } while ($choice -notmatch '^[1-5]$')
    
    return $choice
}

function Wait-ForUser {
    param([string]$Message = "Press any key to continue...")
    
    Write-Host ""
    Write-Host $Message -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Write-Host ""
}

# Main execution
try {
    Write-Host "🔍 Checking prerequisites..." -ForegroundColor Cyan
    
    # Check if sqlcmd is available
    try {
        $null = Get-Command sqlcmd -ErrorAction Stop
        Write-Host "✅ sqlcmd found" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ sqlcmd not found. Please install SQL Server Command Line Utilities." -ForegroundColor Red
        exit 1
    }
    
    # Test SQL Server connection
    Write-Host "🔗 Testing connection to SQL Server: $ServerInstance" -ForegroundColor Cyan
    if (Test-SqlConnection -ServerInstance $ServerInstance) {
        Write-Host "✅ SQL Server connection successful" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Cannot connect to SQL Server: $ServerInstance" -ForegroundColor Red
        Write-Host "   Please ensure SQL Server is running and accessible." -ForegroundColor Red
        exit 1
    }
    
    # Interactive mode menu
    if ($InteractiveMode) {
        $choice = Show-Menu
        
        switch ($choice) {
            "1" { $RunSetup = $true; $RunDemo = $true }
            "2" { $RunSetup = $true; $RunDemo = $false }
            "3" { $RunSetup = $false; $RunDemo = $true }
            "4" { $RunSetup = $false; $RunDemo = $true; $QuickDemo = $true }
            "5" { exit 0 }
        }
    }
    else {
        $RunSetup = -not $SkipSetup
        $RunDemo = $true
    }
    
    # Setup Phase
    if ($RunSetup) {
        Write-Host ""
        Write-Host "🚀 SETUP PHASE: Creating temporal database and sample data" -ForegroundColor Magenta
        Write-Host "=========================================================" -ForegroundColor Magenta
        
        foreach ($script in $SetupScripts) {
            $scriptPath = Join-Path $SetupPath $script
            $description = switch ($script) {
                "01-create-database.sql" { "Creating TemporalDemo database" }
                "02-create-temporal-tables.sql" { "Creating temporal tables with history tracking" }
                "03-insert-sample-data.sql" { "Inserting sample data" }
                "04-simulate-changes.sql" { "Simulating data changes over time" }
            }
            
            Execute-SqlScript -ScriptPath $scriptPath -ServerInstance $ServerInstance -Description $description
            
            if ($InteractiveMode -and $script -eq "03-insert-sample-data.sql") {
                Wait-ForUser "Setup phase 1 complete. Ready to simulate changes?"
            }
        }
        
        Write-Host ""
        Write-Host "✅ Setup phase completed successfully!" -ForegroundColor Green
        Write-Host "   • TemporalDemo database created" -ForegroundColor Gray
        Write-Host "   • 5 temporal tables with automatic history tracking" -ForegroundColor Gray
        Write-Host "   • Sample data inserted and changes simulated" -ForegroundColor Gray
        
        if ($InteractiveMode) {
            Wait-ForUser "Setup complete! Ready to run the demo?"
        }
    }
    
    # Demo Phase
    if ($RunDemo) {
        Write-Host ""
        Write-Host "🎭 DEMO PHASE: Exploring temporal database capabilities" -ForegroundColor Magenta
        Write-Host "=====================================================" -ForegroundColor Magenta
        
        if ($QuickDemo) {
            # Run only the presentation script for quick demos
            $scriptPath = Join-Path $QueriesPath "03-demo-presentation.sql"
            Execute-SqlScript -ScriptPath $scriptPath -ServerInstance $ServerInstance -Description "Running interactive presentation demo"
        }
        else {
            # Run all demo scripts
            foreach ($script in $DemoScripts) {
                $scriptPath = Join-Path $QueriesPath $script
                $description = switch ($script) {
                    "01-basic-temporal-queries.sql" { "Demonstrating basic temporal query patterns" }
                    "02-advanced-scenarios.sql" { "Exploring advanced temporal scenarios" }
                    "03-demo-presentation.sql" { "Running interactive presentation" }
                }
                
                Execute-SqlScript -ScriptPath $scriptPath -ServerInstance $ServerInstance -Description $description
                
                if ($InteractiveMode) {
                    Wait-ForUser "Demo section complete. Continue to next section?"
                }
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
    Write-Host "   • Time-travel queries (FOR SYSTEM_TIME AS OF)" -ForegroundColor Gray
    Write-Host "   • Complete audit trails (FOR SYSTEM_TIME ALL)" -ForegroundColor Gray
    Write-Host "   • Change detection and analysis" -ForegroundColor Gray
    Write-Host "   • Compliance and regulatory reporting" -ForegroundColor Gray
    Write-Host "   • Business intelligence with historical context" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔗 Next steps:" -ForegroundColor Cyan
    Write-Host "   • Connect to TemporalDemo database with SSMS" -ForegroundColor Gray
    Write-Host "   • Explore the temporal tables and history data" -ForegroundColor Gray
    Write-Host "   • Try your own temporal queries" -ForegroundColor Gray
    Write-Host "   • Review the query scripts for learning" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📁 Database: TemporalDemo on $ServerInstance" -ForegroundColor Yellow
    Write-Host "📁 Scripts location: $DemoRoot" -ForegroundColor Yellow
    
}
catch {
    Write-Host ""
    Write-Host "❌ Demo execution failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "   • Ensure SQL Server is running" -ForegroundColor Gray
    Write-Host "   • Check connection string: $ServerInstance" -ForegroundColor Gray
    Write-Host "   • Verify sqlcmd is installed and accessible" -ForegroundColor Gray
    Write-Host "   • Check SQL Server permissions" -ForegroundColor Gray
    
    exit 1
}

Write-Host ""
Write-Host "Thank you for exploring temporal databases! 🚀" -ForegroundColor Green