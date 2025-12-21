-- SQLite Temporal Demo - Temporal-like Queries
-- This script demonstrates temporal query patterns using SQLite history tables

SELECT '╔══════════════════════════════════════════════════════════════════════════════╗';
SELECT '║                    SQLITE TEMPORAL QUERIES DEMONSTRATION                    ║';
SELECT '║                                                                              ║';
SELECT '║  This demo shows temporal-like functionality using SQLite triggers and       ║';
SELECT '║  history tables to track data changes over time.                            ║';
SELECT '╚══════════════════════════════════════════════════════════════════════════════╝';

-- =====================================================
-- 1. CURRENT STATE QUERIES (Standard SQL)
-- =====================================================

SELECT '🔍 1. CURRENT STATE QUERIES';
SELECT '============================';

-- Show current employees
SELECT 
    EmployeeId,
    FirstName || ' ' || LastName AS FullName,
    Department,
    Position,
    '$' || PRINTF('%.2f', Salary) AS Salary,
    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS Status
FROM Employees
ORDER BY Department, LastName;

SELECT 'Current employees shown above.';

-- Show current product prices
SELECT 
    ProductName,
    Category,
    '$' || PRINTF('%.2f', Price) AS CurrentPrice,
    StockQuantity,
    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Discontinued' END AS Status
FROM Products
ORDER BY Category, ProductName;

SELECT 'Current product catalog shown above.';

-- =====================================================
-- 2. COMPLETE CHANGE HISTORY (ALL RECORDS)
-- =====================================================

SELECT '📋 2. COMPLETE CHANGE HISTORY';
SELECT '=============================';

-- Show complete history for John Smith
SELECT 
    FirstName || ' ' || LastName AS FullName,
    Department,
    Position,
    '$' || PRINTF('%.2f', Salary) AS Salary,
    ValidFrom,
    ValidTo,
    OperationType,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59' THEN '🟢 Current'
        ELSE '📜 Historical'
    END AS Status
FROM (
    -- Current record
    SELECT FirstName, LastName, Department, Position, Salary, ValidFrom, ValidTo, 'CURRENT' as OperationType
    FROM Employees 
    WHERE FirstName = 'John' AND LastName = 'Smith'
    
    UNION ALL
    
    -- Historical records
    SELECT FirstName, LastName, Department, Position, Salary, ValidFrom, ValidTo, OperationType
    FROM EmployeesHistory 
    WHERE FirstName = 'John' AND LastName = 'Smith'
) AS AllRecords
ORDER BY ValidFrom;

SELECT 'Complete history for John Smith shown above.';

-- Show price history for Wireless Headphones
SELECT 
    ProductName,
    '$' || PRINTF('%.2f', Price) AS Price,
    ValidFrom,
    ValidTo,
    OperationType,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59' THEN '🟢 Current Price'
        ELSE '📜 Historical Price'
    END AS Status
FROM (
    -- Current record
    SELECT ProductName, Price, ValidFrom, ValidTo, 'CURRENT' as OperationType
    FROM Products 
    WHERE ProductName LIKE '%Headphones%'
    
    UNION ALL
    
    -- Historical records
    SELECT ProductName, Price, ValidFrom, ValidTo, OperationType
    FROM ProductsHistory 
    WHERE ProductName LIKE '%Headphones%'
) AS AllRecords
ORDER BY ValidFrom;

SELECT 'Complete price history for Wireless Headphones shown above.';

-- =====================================================
-- 3. CHANGE DETECTION AND ANALYSIS
-- =====================================================

SELECT '🔍 3. CHANGE DETECTION AND ANALYSIS';
SELECT '====================================';

-- Salary increases analysis
WITH SalaryChanges AS (
    SELECT 
        h1.FirstName || ' ' || h1.LastName AS Name,
        h1.Salary AS OldSalary,
        h2.Salary AS NewSalary,
        h2.ValidFrom AS ChangeDate
    FROM EmployeesHistory h1
    JOIN EmployeesHistory h2 ON h1.EmployeeId = h2.EmployeeId
    WHERE h1.ValidTo = h2.ValidFrom
      AND h2.Salary > h1.Salary
      AND h2.OperationType = 'UPDATE'
)
SELECT 
    Name,
    '$' || PRINTF('%.2f', OldSalary) AS OldSalary,
    '$' || PRINTF('%.2f', NewSalary) AS NewSalary,
    '$' || PRINTF('%.2f', NewSalary - OldSalary) AS Increase,
    PRINTF('%.1f', (NewSalary - OldSalary) / OldSalary * 100) || '%' AS PercentIncrease,
    ChangeDate,
    '🎉 Salary Increase!' AS Note
FROM SalaryChanges
ORDER BY (NewSalary - OldSalary) DESC;

SELECT 'Salary increases analysis shown above.';

-- Product price changes
WITH PriceChanges AS (
    SELECT 
        h1.ProductName,
        h1.Price AS OldPrice,
        h2.Price AS NewPrice,
        h2.ValidFrom AS ChangeDate
    FROM ProductsHistory h1
    JOIN ProductsHistory h2 ON h1.ProductId = h2.ProductId
    WHERE h1.ValidTo = h2.ValidFrom
      AND h2.Price != h1.Price
      AND h2.OperationType = 'UPDATE'
)
SELECT 
    ProductName,
    '$' || PRINTF('%.2f', OldPrice) AS OldPrice,
    '$' || PRINTF('%.2f', NewPrice) AS NewPrice,
    '$' || PRINTF('%.2f', ABS(NewPrice - OldPrice)) AS PriceChange,
    CASE 
        WHEN NewPrice > OldPrice THEN '📈 Price Increase'
        ELSE '📉 Price Decrease'
    END AS ChangeType,
    ChangeDate
FROM PriceChanges
ORDER BY ABS(NewPrice - OldPrice) DESC;

SELECT 'Product price changes shown above.';

-- =====================================================
-- 4. AUDIT TRAIL QUERIES
-- =====================================================

SELECT '🛡️ 4. AUDIT TRAIL QUERIES';
SELECT '=========================';

-- Recent changes across all tables
SELECT 'Recent Changes Audit Trail:';

SELECT 
    'Employee' AS TableType,
    FirstName || ' ' || LastName AS RecordIdentifier,
    'Salary: $' || PRINTF('%.2f', Salary) || ', Position: ' || Position AS ChangeDetails,
    ValidFrom AS ChangeTimestamp,
    OperationType
FROM EmployeesHistory
WHERE OperationType = 'UPDATE'

UNION ALL

SELECT 
    'Product' AS TableType,
    ProductName AS RecordIdentifier,
    'Price: $' || PRINTF('%.2f', Price) || ', Stock: ' || StockQuantity AS ChangeDetails,
    ValidFrom AS ChangeTimestamp,
    OperationType
FROM ProductsHistory
WHERE OperationType = 'UPDATE'

UNION ALL

SELECT 
    'Customer' AS TableType,
    FirstName || ' ' || LastName AS RecordIdentifier,
    'Tier: ' || CustomerTier || ', City: ' || City AS ChangeDetails,
    ValidFrom AS ChangeTimestamp,
    OperationType
FROM CustomersHistory
WHERE OperationType = 'UPDATE'

UNION ALL

SELECT 
    'Config' AS TableType,
    ConfigKey AS RecordIdentifier,
    'Value: ' || ConfigValue AS ChangeDetails,
    ValidFrom AS ChangeTimestamp,
    OperationType
FROM SystemConfigHistory
WHERE OperationType = 'UPDATE'

ORDER BY ChangeTimestamp DESC
LIMIT 20;

SELECT 'Recent audit trail shown above.';

-- =====================================================
-- 5. BUSINESS INTELLIGENCE SCENARIOS
-- =====================================================

SELECT '📊 5. BUSINESS INTELLIGENCE WITH TEMPORAL DATA';
SELECT '===============================================';

-- Department transfer analysis
SELECT 'Department Transfer Analysis:';

WITH Transfers AS (
    SELECT 
        h1.FirstName || ' ' || h1.LastName AS Name,
        h1.Department AS FromDept,
        h2.Department AS ToDept,
        h2.ValidFrom AS TransferDate
    FROM EmployeesHistory h1
    JOIN EmployeesHistory h2 ON h1.EmployeeId = h2.EmployeeId
    WHERE h1.ValidTo = h2.ValidFrom
      AND h1.Department != h2.Department
      AND h2.OperationType = 'UPDATE'
)
SELECT 
    Name,
    FromDept || ' ➤ ' || ToDept AS Transfer,
    TransferDate,
    '🔄 Department Change' AS Type
FROM Transfers
ORDER BY TransferDate DESC;

-- Customer tier progression
SELECT 'Customer Tier Progression:';

WITH TierChanges AS (
    SELECT 
        h1.FirstName || ' ' || h1.LastName AS CustomerName,
        h1.CustomerTier AS FromTier,
        h2.CustomerTier AS ToTier,
        h2.ValidFrom AS UpgradeDate
    FROM CustomersHistory h1
    JOIN CustomersHistory h2 ON h1.CustomerId = h2.CustomerId
    WHERE h1.ValidTo = h2.ValidFrom
      AND h1.CustomerTier != h2.CustomerTier
      AND h2.OperationType = 'UPDATE'
)
SELECT 
    CustomerName,
    FromTier || ' ➤ ' || ToTier AS Progression,
    UpgradeDate,
    CASE 
        WHEN ToTier = 'Silver' THEN '🥈 Silver Upgrade'
        WHEN ToTier = 'Gold' THEN '🥇 Gold Upgrade'  
        WHEN ToTier = 'Platinum' THEN '💎 Platinum Upgrade'
        ELSE '🔄 Tier Change'
    END AS UpgradeType
FROM TierChanges
ORDER BY UpgradeDate DESC;

-- =====================================================
-- 6. COMPLIANCE AND SECURITY AUDIT
-- =====================================================

SELECT '🔒 6. COMPLIANCE AND SECURITY AUDIT';
SELECT '===================================';

-- Security configuration changes
SELECT 'Security Configuration Audit:';

SELECT 
    ConfigKey AS SecuritySetting,
    ConfigValue AS Value,
    ValidFrom AS ChangedAt,
    CASE 
        WHEN ConfigKey = 'MAX_LOGIN_ATTEMPTS' THEN '🔐 Login Security'
        WHEN ConfigKey = 'SESSION_TIMEOUT' THEN '⏱️ Session Management'
        WHEN ConfigKey = 'MAINTENANCE_MODE' THEN '🔧 System Maintenance'
        ELSE '⚙️ System Config'
    END AS Category,
    OperationType
FROM SystemConfigHistory
WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'MAINTENANCE_MODE')
ORDER BY ValidFrom DESC;

-- Data quality check - quick corrections (potential errors)
SELECT 'Data Quality Analysis - Quick Corrections:';

WITH QuickCorrections AS (
    SELECT 
        'Employee' AS TableType,
        h1.FirstName || ' ' || h1.LastName AS RecordIdentifier,
        'Salary changed from $' || PRINTF('%.2f', h1.Salary) || ' to $' || PRINTF('%.2f', h2.Salary) AS ChangeDescription,
        h1.ValidFrom AS FirstChangeTime,
        h2.ValidFrom AS CorrectionTime,
        (JULIANDAY(h2.ValidFrom) - JULIANDAY(h1.ValidFrom)) * 24 * 60 AS MinutesToCorrection
    FROM EmployeesHistory h1
    JOIN EmployeesHistory h2 ON h1.EmployeeId = h2.EmployeeId
    WHERE h1.ValidTo = h2.ValidFrom
      AND (JULIANDAY(h2.ValidFrom) - JULIANDAY(h1.ValidFrom)) * 24 * 60 <= 5 -- Within 5 minutes
      AND h1.Salary != h2.Salary
)
SELECT 
    TableType,
    RecordIdentifier,
    ChangeDescription,
    FirstChangeTime,
    CorrectionTime,
    PRINTF('%.1f', MinutesToCorrection) || ' minutes' AS TimeBetweenChanges,
    '⚠️ Potential Data Entry Error' AS DataQualityFlag
FROM QuickCorrections
ORDER BY MinutesToCorrection;

-- =====================================================
-- 7. TEMPORAL ANALYTICS SUMMARY
-- =====================================================

SELECT '📈 7. TEMPORAL ANALYTICS SUMMARY';
SELECT '=================================';

-- Summary statistics
SELECT 'Temporal Database Statistics:';

SELECT 'Total Current Records' AS Metric, 
       (SELECT COUNT(*) FROM Employees) + 
       (SELECT COUNT(*) FROM Products) + 
       (SELECT COUNT(*) FROM Customers) + 
       (SELECT COUNT(*) FROM SystemConfig) AS Value

UNION ALL

SELECT 'Total Historical Records',
       (SELECT COUNT(*) FROM EmployeesHistory) + 
       (SELECT COUNT(*) FROM ProductsHistory) + 
       (SELECT COUNT(*) FROM CustomersHistory) + 
       (SELECT COUNT(*) FROM SystemConfigHistory)

UNION ALL

SELECT 'Total Changes Tracked',
       (SELECT COUNT(*) FROM EmployeesHistory WHERE OperationType = 'UPDATE') + 
       (SELECT COUNT(*) FROM ProductsHistory WHERE OperationType = 'UPDATE') + 
       (SELECT COUNT(*) FROM CustomersHistory WHERE OperationType = 'UPDATE') + 
       (SELECT COUNT(*) FROM SystemConfigHistory WHERE OperationType = 'UPDATE');

-- Change frequency by table
SELECT 'Change Frequency by Table:';

SELECT 'Employees' AS TableName, COUNT(*) AS TotalChanges, 
       COUNT(CASE WHEN OperationType = 'UPDATE' THEN 1 END) AS Updates
FROM EmployeesHistory

UNION ALL

SELECT 'Products', COUNT(*), COUNT(CASE WHEN OperationType = 'UPDATE' THEN 1 END)
FROM ProductsHistory

UNION ALL

SELECT 'Customers', COUNT(*), COUNT(CASE WHEN OperationType = 'UPDATE' THEN 1 END)
FROM CustomersHistory

UNION ALL

SELECT 'SystemConfig', COUNT(*), COUNT(CASE WHEN OperationType = 'UPDATE' THEN 1 END)
FROM SystemConfigHistory;

SELECT '╔══════════════════════════════════════════════════════════════════════════════╗';
SELECT '║                           DEMO COMPLETED                                     ║';
SELECT '║                                                                              ║';
SELECT '║  SQLite temporal-like tables provide powerful audit and history tracking    ║';
SELECT '║  capabilities without requiring a full SQL Server installation.             ║';
SELECT '╚══════════════════════════════════════════════════════════════════════════════╝';

SELECT 'Demo completed at: ' || CURRENT_TIMESTAMP;