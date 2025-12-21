# Interactive Temporal Database Demo
# This script provides an interactive menu to explore temporal database features

param(
    [string]$DatabasePath = "temporal_demo.db"
)

function Show-MainMenu {
    Clear-Host
    Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    INTERACTIVE TEMPORAL DATABASE DEMO                       ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📋 Choose a demo scenario:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. 👥 Employee Management Demo" -ForegroundColor Green
    Write-Host "2. 💰 Product Pricing Demo" -ForegroundColor Green  
    Write-Host "3. 🏆 Customer Tier Demo" -ForegroundColor Green
    Write-Host "4. ⚙️  System Config Demo" -ForegroundColor Green
    Write-Host "5. 🕐 Time Travel Queries" -ForegroundColor Green
    Write-Host "6. 📊 Business Intelligence" -ForegroundColor Green
    Write-Host "7. 🛡️  Compliance Audit" -ForegroundColor Green
    Write-Host "8. 🔍 Custom SQL Query" -ForegroundColor Green
    Write-Host "9. 📈 View All Data" -ForegroundColor Green
    Write-Host "0. Exit" -ForegroundColor Red
    Write-Host ""
}

function Execute-Query {
    param([string]$Query, [string]$Description)
    
    Write-Host "🔄 $Description..." -ForegroundColor Yellow
    Write-Host ""
    
    $result = & "./sqlite3.exe" $DatabasePath $Query
    
    if ($result) {
        $result | ForEach-Object {
            Write-Host "   $_" -ForegroundColor Gray
        }
    } else {
        Write-Host "   No results returned." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Press any key to continue..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function Show-EmployeeDemo {
    Clear-Host
    Write-Host "👥 EMPLOYEE MANAGEMENT DEMO" -ForegroundColor Cyan
    Write-Host "===========================" -ForegroundColor Cyan
    Write-Host ""
    
    # Current employees
    Execute-Query "SELECT EmployeeId, FirstName || ' ' || LastName AS Name, Department, Position, '$' || PRINTF('%.2f', Salary) AS Salary FROM Employees WHERE IsActive = 1 ORDER BY Department;" "Current Active Employees"
    
    # John Smith's complete history
    Execute-Query "SELECT FirstName || ' ' || LastName AS Name, Department, Position, '$' || PRINTF('%.2f', Salary) AS Salary, ValidFrom, CASE WHEN ValidTo = '9999-12-31 23:59:59' THEN 'Current' ELSE ValidTo END AS ValidTo FROM (SELECT * FROM Employees WHERE FirstName = 'John' AND LastName = 'Smith' UNION ALL SELECT EmployeeId, FirstName, LastName, Email, Department, Position, Salary, HireDate, IsActive, ValidFrom, ValidTo FROM EmployeesHistory WHERE FirstName = 'John' AND LastName = 'Smith') ORDER BY ValidFrom;" "John Smith's Complete Career History"
    
    # Salary increases
    Execute-Query "WITH SalaryChanges AS (SELECT h1.FirstName || ' ' || h1.LastName AS Name, h1.Salary AS OldSalary, h2.Salary AS NewSalary, h2.ValidFrom AS ChangeDate FROM EmployeesHistory h1 JOIN EmployeesHistory h2 ON h1.EmployeeId = h2.EmployeeId WHERE h1.ValidTo = h2.ValidFrom AND h2.Salary > h1.Salary) SELECT Name, '$' || PRINTF('%.2f', OldSalary) AS OldSalary, '$' || PRINTF('%.2f', NewSalary) AS NewSalary, '$' || PRINTF('%.2f', NewSalary - OldSalary) AS Increase, ChangeDate FROM SalaryChanges ORDER BY (NewSalary - OldSalary) DESC;" "Recent Salary Increases"
}

function Show-ProductDemo {
    Clear-Host
    Write-Host "💰 PRODUCT PRICING DEMO" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    Write-Host ""
    
    # Current products
    Execute-Query "SELECT ProductName, Category, '$' || PRINTF('%.2f', Price) AS Price, StockQuantity, CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Discontinued' END AS Status FROM Products ORDER BY Category;" "Current Product Catalog"
    
    # Price history for Wireless Headphones
    Execute-Query "SELECT ProductName, '$' || PRINTF('%.2f', Price) AS Price, ValidFrom, CASE WHEN ValidTo = '9999-12-31 23:59:59' THEN 'Current' ELSE ValidTo END AS ValidTo FROM (SELECT * FROM Products WHERE ProductName LIKE '%Headphones%' UNION ALL SELECT * FROM ProductsHistory WHERE ProductName LIKE '%Headphones%') ORDER BY ValidFrom;" "Wireless Headphones Price History"
    
    # All price changes
    Execute-Query "WITH PriceChanges AS (SELECT h1.ProductName, h1.Price AS OldPrice, h2.Price AS NewPrice, h2.ValidFrom AS ChangeDate FROM ProductsHistory h1 JOIN ProductsHistory h2 ON h1.ProductId = h2.ProductId WHERE h1.ValidTo = h2.ValidFrom AND h2.Price != h1.Price) SELECT ProductName, '$' || PRINTF('%.2f', OldPrice) AS OldPrice, '$' || PRINTF('%.2f', NewPrice) AS NewPrice, CASE WHEN NewPrice > OldPrice THEN 'Increase' ELSE 'Decrease' END AS ChangeType, ChangeDate FROM PriceChanges ORDER BY ABS(NewPrice - OldPrice) DESC;" "All Product Price Changes"
}

function Show-CustomerDemo {
    Clear-Host
    Write-Host "🏆 CUSTOMER TIER DEMO" -ForegroundColor Cyan
    Write-Host "=====================" -ForegroundColor Cyan
    Write-Host ""
    
    # Current customers
    Execute-Query "SELECT FirstName || ' ' || LastName AS Name, CustomerTier, City || ', ' || State AS Location FROM Customers ORDER BY CustomerTier, LastName;" "Current Customers by Tier"
    
    # Customer tier progression
    Execute-Query "WITH TierChanges AS (SELECT h1.FirstName || ' ' || h1.LastName AS CustomerName, h1.CustomerTier AS FromTier, h2.CustomerTier AS ToTier, h2.ValidFrom AS UpgradeDate FROM CustomersHistory h1 JOIN CustomersHistory h2 ON h1.CustomerId = h2.CustomerId WHERE h1.ValidTo = h2.ValidFrom AND h1.CustomerTier != h2.CustomerTier) SELECT CustomerName, FromTier || ' → ' || ToTier AS Progression, UpgradeDate FROM TierChanges ORDER BY UpgradeDate DESC;" "Customer Tier Progression History"
    
    # Alice Cooper's complete history
    Execute-Query "SELECT FirstName || ' ' || LastName AS Name, CustomerTier, City, ValidFrom, CASE WHEN ValidTo = '9999-12-31 23:59:59' THEN 'Current' ELSE ValidTo END AS ValidTo FROM (SELECT * FROM Customers WHERE FirstName = 'Alice' AND LastName = 'Cooper' UNION ALL SELECT * FROM CustomersHistory WHERE FirstName = 'Alice' AND LastName = 'Cooper') ORDER BY ValidFrom;" "Alice Cooper's Complete History"
}

function Show-ConfigDemo {
    Clear-Host
    Write-Host "⚙️ SYSTEM CONFIGURATION DEMO" -ForegroundColor Cyan
    Write-Host "=============================" -ForegroundColor Cyan
    Write-Host ""
    
    # Current configuration
    Execute-Query "SELECT ConfigKey, ConfigValue, Description FROM SystemConfig WHERE IsActive = 1 ORDER BY Category, ConfigKey;" "Current System Configuration"
    
    # Security config changes
    Execute-Query "SELECT ConfigKey, ConfigValue, ValidFrom, CASE WHEN ValidTo = '9999-12-31 23:59:59' THEN 'Current' ELSE ValidTo END AS ValidTo FROM (SELECT ConfigKey, ConfigValue, ValidFrom, ValidTo FROM SystemConfig WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'MAINTENANCE_MODE') UNION ALL SELECT ConfigKey, ConfigValue, ValidFrom, ValidTo FROM SystemConfigHistory WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'MAINTENANCE_MODE')) ORDER BY ConfigKey, ValidFrom;" "Security Configuration History"
}

function Show-TimeTravelDemo {
    Clear-Host
    Write-Host "🕐 TIME TRAVEL QUERIES DEMO" -ForegroundColor Cyan
    Write-Host "===========================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Enter a timestamp to travel back to (format: YYYY-MM-DD HH:MM:SS)" -ForegroundColor Yellow
    Write-Host "Or press Enter to use 30 minutes ago" -ForegroundColor Gray
    $timeInput = Read-Host "Timestamp"
    
    if ([string]::IsNullOrWhiteSpace($timeInput)) {
        $timePoint = (Get-Date).AddMinutes(-30).ToString("yyyy-MM-dd HH:mm:ss")
    } else {
        $timePoint = $timeInput
    }
    
    Write-Host "🕐 Traveling back to: $timePoint" -ForegroundColor Cyan
    Write-Host ""
    
    # This is a simplified version since SQLite doesn't have built-in temporal queries
    # We'll show the closest historical records
    Execute-Query "SELECT 'Employee' AS TableType, FirstName || ' ' || LastName AS Name, Department, Position, '$' || PRINTF('%.2f', Salary) AS Value FROM EmployeesHistory WHERE ValidFrom <= '$timePoint' AND ValidTo > '$timePoint' UNION ALL SELECT 'Product', ProductName, Category, '', '$' || PRINTF('%.2f', Price) FROM ProductsHistory WHERE ValidFrom <= '$timePoint' AND ValidTo > '$timePoint' ORDER BY TableType, Name;" "Data as it existed at $timePoint"
}

function Show-BusinessIntelligence {
    Clear-Host
    Write-Host "📊 BUSINESS INTELLIGENCE DEMO" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host ""
    
    # Department analysis
    Execute-Query "SELECT Department, COUNT(*) AS EmployeeCount, '$' || PRINTF('%.2f', AVG(Salary)) AS AvgSalary, '$' || PRINTF('%.2f', MIN(Salary)) AS MinSalary, '$' || PRINTF('%.2f', MAX(Salary)) AS MaxSalary FROM Employees WHERE IsActive = 1 GROUP BY Department ORDER BY AvgSalary DESC;" "Department Salary Analysis"
    
    # Customer tier distribution
    Execute-Query "SELECT CustomerTier, COUNT(*) AS CustomerCount, ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Customers), 1) || '%' AS Percentage FROM Customers GROUP BY CustomerTier ORDER BY CustomerCount DESC;" "Customer Tier Distribution"
    
    # Product category analysis
    Execute-Query "SELECT Category, COUNT(*) AS ProductCount, '$' || PRINTF('%.2f', AVG(Price)) AS AvgPrice, '$' || PRINTF('%.2f', MIN(Price)) AS MinPrice, '$' || PRINTF('%.2f', MAX(Price)) AS MaxPrice FROM Products WHERE IsActive = 1 GROUP BY Category ORDER BY AvgPrice DESC;" "Product Category Analysis"
    
    # Change frequency analysis
    Execute-Query "SELECT 'Employees' AS TableName, COUNT(*) AS TotalChanges FROM EmployeesHistory WHERE OperationType = 'UPDATE' UNION ALL SELECT 'Products', COUNT(*) FROM ProductsHistory WHERE OperationType = 'UPDATE' UNION ALL SELECT 'Customers', COUNT(*) FROM CustomersHistory WHERE OperationType = 'UPDATE' UNION ALL SELECT 'SystemConfig', COUNT(*) FROM SystemConfigHistory WHERE OperationType = 'UPDATE' ORDER BY TotalChanges DESC;" "Change Frequency by Table"
}

function Show-ComplianceAudit {
    Clear-Host
    Write-Host "🛡️ COMPLIANCE AUDIT DEMO" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ""
    
    # Recent changes audit
    Execute-Query "SELECT 'Employee' AS TableType, FirstName || ' ' || LastName AS RecordIdentifier, 'Salary: $' || PRINTF('%.2f', Salary) || ', Position: ' || Position AS ChangeDetails, ValidFrom AS ChangeTimestamp FROM EmployeesHistory WHERE OperationType = 'UPDATE' UNION ALL SELECT 'Product', ProductName, 'Price: $' || PRINTF('%.2f', Price) || ', Stock: ' || StockQuantity, ValidFrom FROM ProductsHistory WHERE OperationType = 'UPDATE' UNION ALL SELECT 'Customer', FirstName || ' ' || LastName, 'Tier: ' || CustomerTier || ', City: ' || City, ValidFrom FROM CustomersHistory WHERE OperationType = 'UPDATE' ORDER BY ChangeTimestamp DESC LIMIT 10;" "Recent Changes Audit Trail"
    
    # Security configuration audit
    Execute-Query "SELECT ConfigKey AS SecuritySetting, ConfigValue AS Value, ValidFrom AS ChangedAt, OperationType FROM SystemConfigHistory WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'MAINTENANCE_MODE') ORDER BY ValidFrom DESC;" "Security Configuration Audit"
    
    # Data quality check
    Execute-Query "SELECT 'Employee' AS TableType, h1.FirstName || ' ' || h1.LastName AS RecordIdentifier, 'Salary changed from $' || PRINTF('%.2f', h1.Salary) || ' to $' || PRINTF('%.2f', h2.Salary) AS ChangeDescription, h1.ValidFrom AS FirstChangeTime, h2.ValidFrom AS CorrectionTime FROM EmployeesHistory h1 JOIN EmployeesHistory h2 ON h1.EmployeeId = h2.EmployeeId WHERE h1.ValidTo = h2.ValidFrom AND (JULIANDAY(h2.ValidFrom) - JULIANDAY(h1.ValidFrom)) * 24 * 60 <= 5 AND h1.Salary != h2.Salary;" "Quick Corrections (Potential Data Entry Errors)"
}

function Show-CustomQuery {
    Clear-Host
    Write-Host "🔍 CUSTOM SQL QUERY" -ForegroundColor Cyan
    Write-Host "===================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Available tables:" -ForegroundColor Yellow
    Write-Host "  • Employees, EmployeesHistory" -ForegroundColor Gray
    Write-Host "  • Products, ProductsHistory" -ForegroundColor Gray
    Write-Host "  • Customers, CustomersHistory" -ForegroundColor Gray
    Write-Host "  • SystemConfig, SystemConfigHistory" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Enter your SQL query (or press Enter to cancel):" -ForegroundColor Yellow
    $query = Read-Host "SQL"
    
    if (![string]::IsNullOrWhiteSpace($query)) {
        Execute-Query $query "Executing custom query"
    }
}

function Show-AllData {
    Clear-Host
    Write-Host "📈 ALL DATA OVERVIEW" -ForegroundColor Cyan
    Write-Host "====================" -ForegroundColor Cyan
    Write-Host ""
    
    # Summary statistics
    Execute-Query "SELECT 'Current Records' AS Metric, (SELECT COUNT(*) FROM Employees) + (SELECT COUNT(*) FROM Products) + (SELECT COUNT(*) FROM Customers) + (SELECT COUNT(*) FROM SystemConfig) AS Value UNION ALL SELECT 'Historical Records', (SELECT COUNT(*) FROM EmployeesHistory) + (SELECT COUNT(*) FROM ProductsHistory) + (SELECT COUNT(*) FROM CustomersHistory) + (SELECT COUNT(*) FROM SystemConfigHistory) UNION ALL SELECT 'Total Changes', (SELECT COUNT(*) FROM EmployeesHistory WHERE OperationType = 'UPDATE') + (SELECT COUNT(*) FROM ProductsHistory WHERE OperationType = 'UPDATE') + (SELECT COUNT(*) FROM CustomersHistory WHERE OperationType = 'UPDATE') + (SELECT COUNT(*) FROM SystemConfigHistory WHERE OperationType = 'UPDATE');" "Database Statistics"
    
    # Table overview
    Execute-Query "SELECT name AS TableName, CASE WHEN name LIKE '%History' THEN 'History Table' ELSE 'Current Table' END AS TableType FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY TableType, TableName;" "All Tables in Database"
}

# Main execution loop
if (-not (Test-Path $DatabasePath)) {
    Write-Host "❌ Database not found: $DatabasePath" -ForegroundColor Red
    Write-Host "Please run the temporal database demo first to create the database." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path "./sqlite3.exe")) {
    Write-Host "❌ SQLite not found. Please run the main demo first to download SQLite." -ForegroundColor Red
    exit 1
}

while ($true) {
    Show-MainMenu
    $choice = Read-Host "Enter your choice (0-9)"
    
    switch ($choice) {
        "1" { Show-EmployeeDemo }
        "2" { Show-ProductDemo }
        "3" { Show-CustomerDemo }
        "4" { Show-ConfigDemo }
        "5" { Show-TimeTravelDemo }
        "6" { Show-BusinessIntelligence }
        "7" { Show-ComplianceAudit }
        "8" { Show-CustomQuery }
        "9" { Show-AllData }
        "0" { 
            Write-Host "Thank you for exploring temporal databases! 🚀" -ForegroundColor Green
            exit 0 
        }
        default { 
            Write-Host "Invalid choice. Please enter a number between 0-9." -ForegroundColor Red
            Start-Sleep 2
        }
    }
}