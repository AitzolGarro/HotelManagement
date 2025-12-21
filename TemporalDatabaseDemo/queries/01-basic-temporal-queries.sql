-- Temporal Database Demo - Basic Temporal Queries
-- This script demonstrates fundamental temporal query patterns

USE TemporalDemo;
GO

PRINT '=== BASIC TEMPORAL QUERIES DEMONSTRATION ===';
PRINT '';

-- =====================================================
-- 1. CURRENT STATE QUERIES (Standard SQL)
-- =====================================================

PRINT '1. CURRENT STATE QUERIES';
PRINT '========================';

-- Show current employees
SELECT 
    EmployeeId,
    FirstName + ' ' + LastName AS FullName,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary,
    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS Status
FROM dbo.Employees
ORDER BY Department, LastName;

PRINT 'Current employees shown above.';
PRINT '';

-- Show current product prices
SELECT 
    ProductName,
    Category,
    FORMAT(Price, 'C') AS CurrentPrice,
    StockQuantity,
    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Discontinued' END AS Status
FROM dbo.Products
ORDER BY Category, ProductName;

PRINT 'Current product catalog shown above.';
PRINT '';

-- =====================================================
-- 2. POINT-IN-TIME QUERIES (FOR SYSTEM_TIME AS OF)
-- =====================================================

PRINT '2. POINT-IN-TIME QUERIES';
PRINT '========================';

-- Declare a point in time (adjust this to a time between your data changes)
DECLARE @PointInTime DATETIME2 = DATEADD(MINUTE, -30, GETDATE());

PRINT 'Querying data as it existed at: ' + CONVERT(VARCHAR, @PointInTime, 120);
PRINT '';

-- Show employees as they were 30 minutes ago
SELECT 
    EmployeeId,
    FirstName + ' ' + LastName AS FullName,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary,
    'Historical Data' AS Note
FROM dbo.Employees FOR SYSTEM_TIME AS OF @PointInTime
ORDER BY Department, LastName;

PRINT 'Employee data as of ' + CONVERT(VARCHAR, @PointInTime, 120) + ' shown above.';
PRINT '';

-- Show product prices as they were 30 minutes ago
SELECT 
    ProductName,
    Category,
    FORMAT(Price, 'C') AS PriceAtTime,
    StockQuantity,
    'Historical Data' AS Note
FROM dbo.Products FOR SYSTEM_TIME AS OF @PointInTime
ORDER BY Category, ProductName;

PRINT 'Product prices as of ' + CONVERT(VARCHAR, @PointInTime, 120) + ' shown above.';
PRINT '';

-- =====================================================
-- 3. CHANGE TRACKING QUERIES (FOR SYSTEM_TIME ALL)
-- =====================================================

PRINT '3. COMPLETE CHANGE HISTORY';
PRINT '==========================';

-- Show all changes to John Smith's record
SELECT 
    FirstName + ' ' + LastName AS FullName,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary,
    ValidFrom,
    ValidTo,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59.9999999' THEN 'Current'
        ELSE 'Historical'
    END AS RecordStatus
FROM dbo.Employees FOR SYSTEM_TIME ALL
WHERE FirstName = 'John' AND LastName = 'Smith'
ORDER BY ValidFrom;

PRINT 'Complete history for John Smith shown above.';
PRINT '';

-- Show all price changes for Wireless Headphones
SELECT 
    ProductName,
    FORMAT(Price, 'C') AS Price,
    StockQuantity,
    Description,
    ValidFrom,
    ValidTo,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59.9999999' THEN 'Current'
        ELSE 'Historical'
    END AS RecordStatus
FROM dbo.Products FOR SYSTEM_TIME ALL
WHERE ProductName LIKE '%Wireless Headphones%' OR ProductName LIKE '%Headphones%'
ORDER BY ValidFrom;

PRINT 'Complete price history for Wireless Headphones shown above.';
PRINT '';

-- =====================================================
-- 4. TIME RANGE QUERIES (FOR SYSTEM_TIME BETWEEN)
-- =====================================================

PRINT '4. TIME RANGE QUERIES';
PRINT '=====================';

-- Show all changes in the last hour
DECLARE @OneHourAgo DATETIME2 = DATEADD(HOUR, -1, GETDATE());
DECLARE @Now DATETIME2 = GETDATE();

PRINT 'Showing changes between ' + CONVERT(VARCHAR, @OneHourAgo, 120) + ' and ' + CONVERT(VARCHAR, @Now, 120);
PRINT '';

-- Employee changes in the last hour
SELECT 
    FirstName + ' ' + LastName AS FullName,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary,
    ValidFrom,
    ValidTo,
    'Recent Change' AS Note
FROM dbo.Employees FOR SYSTEM_TIME BETWEEN @OneHourAgo AND @Now
ORDER BY ValidFrom DESC;

PRINT 'Employee changes in the last hour shown above.';
PRINT '';

-- System configuration changes in the last hour
SELECT 
    ConfigKey,
    ConfigValue,
    Description,
    ValidFrom,
    ValidTo,
    'Config Change' AS Note
FROM dbo.SystemConfig FOR SYSTEM_TIME BETWEEN @OneHourAgo AND @Now
ORDER BY ValidFrom DESC;

PRINT 'System configuration changes in the last hour shown above.';
PRINT '';

-- =====================================================
-- 5. COMPARISON QUERIES (BEFORE vs AFTER)
-- =====================================================

PRINT '5. BEFORE vs AFTER COMPARISONS';
PRINT '===============================';

-- Compare employee salaries: 1 hour ago vs now
WITH EmployeesOneHourAgo AS (
    SELECT 
        EmployeeId,
        FirstName + ' ' + LastName AS FullName,
        Salary AS OldSalary,
        Position AS OldPosition,
        Department AS OldDepartment
    FROM dbo.Employees FOR SYSTEM_TIME AS OF @OneHourAgo
),
EmployeesNow AS (
    SELECT 
        EmployeeId,
        FirstName + ' ' + LastName AS FullName,
        Salary AS NewSalary,
        Position AS NewPosition,
        Department AS NewDepartment
    FROM dbo.Employees
)
SELECT 
    n.FullName,
    o.OldDepartment,
    n.NewDepartment,
    o.OldPosition,
    n.NewPosition,
    FORMAT(o.OldSalary, 'C') AS OldSalary,
    FORMAT(n.NewSalary, 'C') AS NewSalary,
    FORMAT(n.NewSalary - o.OldSalary, 'C') AS SalaryChange,
    CASE 
        WHEN n.NewSalary > o.OldSalary THEN 'Increase'
        WHEN n.NewSalary < o.OldSalary THEN 'Decrease'
        ELSE 'No Change'
    END AS ChangeType
FROM EmployeesNow n
INNER JOIN EmployeesOneHourAgo o ON n.EmployeeId = o.EmployeeId
WHERE n.NewSalary != o.OldSalary 
   OR n.NewPosition != o.OldPosition 
   OR n.NewDepartment != o.OldDepartment
ORDER BY n.FullName;

PRINT 'Employee changes comparison (1 hour ago vs now) shown above.';
PRINT '';

-- =====================================================
-- 6. AUDIT TRAIL QUERIES
-- =====================================================

PRINT '6. AUDIT TRAIL QUERIES';
PRINT '======================';

-- Show who changed what and when (simulated - in real scenarios you'd have user context)
SELECT 
    'Employee' AS TableName,
    FirstName + ' ' + LastName AS RecordIdentifier,
    'Salary: ' + FORMAT(Salary, 'C') + ', Position: ' + Position AS ChangeDetails,
    ValidFrom AS ChangeTimestamp,
    DATEDIFF(SECOND, LAG(ValidFrom) OVER (PARTITION BY EmployeeId ORDER BY ValidFrom), ValidFrom) AS SecondsSinceLastChange
FROM dbo.Employees FOR SYSTEM_TIME ALL
WHERE ValidTo != '9999-12-31 23:59:59.9999999' -- Only historical records
ORDER BY ValidFrom DESC;

PRINT 'Recent employee change audit trail shown above.';
PRINT '';

-- =====================================================
-- 7. TEMPORAL ANALYTICS
-- =====================================================

PRINT '7. TEMPORAL ANALYTICS';
PRINT '=====================';

-- Calculate average salary over time
SELECT 
    CONVERT(DATE, ValidFrom) AS ChangeDate,
    COUNT(*) AS NumberOfChanges,
    FORMAT(AVG(Salary), 'C') AS AverageSalary,
    FORMAT(MIN(Salary), 'C') AS MinSalary,
    FORMAT(MAX(Salary), 'C') AS MaxSalary
FROM dbo.Employees FOR SYSTEM_TIME ALL
GROUP BY CONVERT(DATE, ValidFrom)
ORDER BY ChangeDate DESC;

PRINT 'Salary analytics by date shown above.';
PRINT '';

-- Product price volatility
SELECT 
    ProductName,
    COUNT(*) - 1 AS NumberOfPriceChanges, -- Subtract 1 for initial insert
    FORMAT(MIN(Price), 'C') AS LowestPrice,
    FORMAT(MAX(Price), 'C') AS HighestPrice,
    FORMAT(MAX(Price) - MIN(Price), 'C') AS PriceRange,
    CONVERT(VARCHAR, MIN(ValidFrom), 120) AS FirstRecorded,
    CONVERT(VARCHAR, MAX(ValidFrom), 120) AS LastChanged
FROM dbo.Products FOR SYSTEM_TIME ALL
GROUP BY ProductName
HAVING COUNT(*) > 1 -- Only products that have changed
ORDER BY NumberOfPriceChanges DESC;

PRINT 'Product price change analytics shown above.';
PRINT '';

PRINT '=== BASIC TEMPORAL QUERIES DEMONSTRATION COMPLETED ===';
PRINT 'These queries show the power of temporal tables for:';
PRINT '- Point-in-time analysis';
PRINT '- Change tracking and auditing';
PRINT '- Historical comparisons';
PRINT '- Trend analysis';
PRINT '- Compliance reporting';

GO