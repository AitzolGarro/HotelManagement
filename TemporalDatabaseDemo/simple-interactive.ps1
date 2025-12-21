# Simple Interactive Temporal Database Demo

$dbPath = "temporal_demo.db"

function Show-Menu {
    Clear-Host
    Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    INTERACTIVE TEMPORAL DATABASE DEMO                       ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📋 Choose a demo scenario:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. 👥 View Current Employees" -ForegroundColor Green
    Write-Host "2. 📈 View Employee History (John Smith)" -ForegroundColor Green  
    Write-Host "3. 💰 View Current Products" -ForegroundColor Green
    Write-Host "4. 🏆 View Customer Tiers" -ForegroundColor Green
    Write-Host "5. ⚙️  View System Configuration" -ForegroundColor Green
    Write-Host "6. 📊 View Database Statistics" -ForegroundColor Green
    Write-Host "7. 🔍 Run Custom Query" -ForegroundColor Green
    Write-Host "0. Exit" -ForegroundColor Red
    Write-Host ""
}

function Run-Query {
    param([string]$Query, [string]$Title)
    
    Write-Host ""
    Write-Host "🔄 $Title" -ForegroundColor Yellow
    Write-Host "=" * $Title.Length -ForegroundColor Yellow
    Write-Host ""
    
    $result = & "./sqlite3.exe" $dbPath $Query
    
    if ($result) {
        $result | ForEach-Object {
            Write-Host "   $_" -ForegroundColor White
        }
    } else {
        Write-Host "   No results found." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Press any key to continue..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# Check prerequisites
if (-not (Test-Path $dbPath)) {
    Write-Host "❌ Database not found: $dbPath" -ForegroundColor Red
    Write-Host "Please run the temporal database demo first." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path "./sqlite3.exe")) {
    Write-Host "❌ SQLite not found. Running main demo first..." -ForegroundColor Yellow
    & ".\TemporalDatabaseDemo\run-demo.ps1"
}

# Main loop
while ($true) {
    Show-Menu
    $choice = Read-Host "Enter your choice (0-7)"
    
    switch ($choice) {
        "1" { 
            Run-Query "SELECT EmployeeId, FirstName || ' ' || LastName AS Name, Department, Position, Salary FROM Employees WHERE IsActive = 1 ORDER BY Department;" "Current Active Employees"
        }
        "2" { 
            Run-Query "SELECT FirstName || ' ' || LastName AS Name, Department, Position, Salary, ValidFrom, ValidTo FROM (SELECT * FROM Employees WHERE FirstName = 'John' AND LastName = 'Smith' UNION ALL SELECT EmployeeId, FirstName, LastName, Email, Department, Position, Salary, HireDate, IsActive, ValidFrom, ValidTo FROM EmployeesHistory WHERE FirstName = 'John' AND LastName = 'Smith') ORDER BY ValidFrom;" "John Smith Career History"
        }
        "3" { 
            Run-Query "SELECT ProductName, Category, Price, StockQuantity, CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Discontinued' END AS Status FROM Products ORDER BY Category;" "Current Product Catalog"
        }
        "4" { 
            Run-Query "SELECT FirstName || ' ' || LastName AS Name, CustomerTier, City || ', ' || State AS Location FROM Customers ORDER BY CustomerTier;" "Customer Tiers"
        }
        "5" { 
            Run-Query "SELECT ConfigKey, ConfigValue, Description FROM SystemConfig WHERE IsActive = 1 ORDER BY Category;" "System Configuration"
        }
        "6" { 
            Run-Query "SELECT 'Current Records' AS Metric, (SELECT COUNT(*) FROM Employees) + (SELECT COUNT(*) FROM Products) + (SELECT COUNT(*) FROM Customers) + (SELECT COUNT(*) FROM SystemConfig) AS Value UNION ALL SELECT 'Historical Records', (SELECT COUNT(*) FROM EmployeesHistory) + (SELECT COUNT(*) FROM ProductsHistory) + (SELECT COUNT(*) FROM CustomersHistory) + (SELECT COUNT(*) FROM SystemConfigHistory);" "Database Statistics"
        }
        "7" { 
            Write-Host ""
            Write-Host "Available tables: Employees, Products, Customers, SystemConfig" -ForegroundColor Yellow
            Write-Host "History tables: EmployeesHistory, ProductsHistory, CustomersHistory, SystemConfigHistory" -ForegroundColor Yellow
            Write-Host ""
            $customQuery = Read-Host "Enter your SQL query"
            if ($customQuery) {
                Run-Query $customQuery "Custom Query Results"
            }
        }
        "0" { 
            Write-Host ""
            Write-Host "Thank you for exploring temporal databases! 🚀" -ForegroundColor Green
            exit 0 
        }
        default { 
            Write-Host "Invalid choice. Please enter 0-7." -ForegroundColor Red
            Start-Sleep 2
        }
    }
}