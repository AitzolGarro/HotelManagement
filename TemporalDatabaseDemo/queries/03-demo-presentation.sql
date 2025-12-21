-- Temporal Database Demo - Interactive Presentation Script
-- This script provides a step-by-step demo for presentations and training

USE TemporalDemo;
GO

PRINT '╔══════════════════════════════════════════════════════════════════════════════╗';
PRINT '║                    TEMPORAL DATABASE INTERACTIVE DEMO                       ║';
PRINT '║                                                                              ║';
PRINT '║  This demo shows the power of SQL Server temporal tables for tracking       ║';
PRINT '║  data changes over time, providing audit trails, and enabling time-travel   ║';
PRINT '║  queries for compliance and business intelligence.                          ║';
PRINT '╚══════════════════════════════════════════════════════════════════════════════╝';
PRINT '';

-- =====================================================
-- DEMO STEP 1: SHOW CURRENT STATE
-- =====================================================

PRINT '🔍 DEMO STEP 1: Current State of Our Data';
PRINT '==========================================';
PRINT '';
PRINT '👥 Current Employees:';

SELECT 
    EmployeeId,
    FirstName + ' ' + LastName AS Name,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary
FROM dbo.Employees
WHERE IsActive = 1
ORDER BY Department, LastName;

PRINT '';
PRINT '💰 Current Product Prices:';

SELECT 
    ProductName,
    Category,
    FORMAT(Price, 'C') AS Price,
    StockQuantity AS Stock
FROM dbo.Products
WHERE IsActive = 1
ORDER BY Category, ProductName;

PRINT '';
PRINT '⚙️  Current System Settings:';

SELECT 
    ConfigKey AS Setting,
    ConfigValue AS Value,
    Description
FROM dbo.SystemConfig
WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'MAINTENANCE_MODE')
ORDER BY ConfigKey;

PRINT '';
PRINT '📊 This is what we see with regular SQL queries - just the current state.';
PRINT '   But what if we want to see how this data looked yesterday? Last week?';
PRINT '   Or track who changed what and when? That''s where temporal tables shine!';
PRINT '';

-- =====================================================
-- DEMO STEP 2: TIME TRAVEL - POINT IN TIME QUERIES
-- =====================================================

PRINT '⏰ DEMO STEP 2: Time Travel - See Data as it Was Before';
PRINT '=======================================================';
PRINT '';

-- Calculate a point in time (30 minutes ago)
DECLARE @TimePoint DATETIME2 = DATEADD(MINUTE, -30, GETDATE());
PRINT '🕐 Let''s travel back to: ' + CONVERT(VARCHAR, @TimePoint, 120);
PRINT '';

PRINT '👥 Employees as they were 30 minutes ago:';

SELECT 
    EmployeeId,
    FirstName + ' ' + LastName AS Name,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary,
    '⏳ Historical Data' AS Note
FROM dbo.Employees FOR SYSTEM_TIME AS OF @TimePoint
WHERE IsActive = 1
ORDER BY Department, LastName;

PRINT '';
PRINT '💰 Product prices as they were 30 minutes ago:';

SELECT 
    ProductName,
    FORMAT(Price, 'C') AS Price,
    '⏳ Historical Data' AS Note
FROM dbo.Products FOR SYSTEM_TIME AS OF @TimePoint
WHERE IsActive = 1
ORDER BY ProductName;

PRINT '';
PRINT '✨ Notice the differences? This is the power of temporal tables!';
PRINT '   We can query data exactly as it existed at any point in time.';
PRINT '';

-- =====================================================
-- DEMO STEP 3: COMPLETE CHANGE HISTORY
-- =====================================================

PRINT '📋 DEMO STEP 3: Complete Change History - Full Audit Trail';
PRINT '==========================================================';
PRINT '';
PRINT '👤 Let''s see the complete career history of John Smith:';

SELECT 
    FirstName + ' ' + LastName AS Name,
    Department,
    Position,
    FORMAT(Salary, 'C') AS Salary,
    ValidFrom AS EffectiveFrom,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59.9999999' THEN 'Current ➤'
        ELSE CONVERT(VARCHAR, ValidTo, 120)
    END AS EffectiveTo,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59.9999999' THEN '🟢 Current'
        ELSE '📜 Historical'
    END AS Status
FROM dbo.Employees FOR SYSTEM_TIME ALL
WHERE FirstName = 'John' AND LastName = 'Smith'
ORDER BY ValidFrom;

PRINT '';
PRINT '💡 Every change is automatically tracked with timestamps!';
PRINT '   Perfect for compliance, auditing, and performance reviews.';
PRINT '';

PRINT '💰 Price history for Wireless Headphones:';

SELECT 
    ProductName,
    FORMAT(Price, 'C') AS Price,
    ValidFrom AS PriceEffectiveFrom,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59.9999999' THEN 'Current ➤'
        ELSE CONVERT(VARCHAR, ValidTo, 120)
    END AS PriceEffectiveTo,
    CASE 
        WHEN ValidTo = '9999-12-31 23:59:59.9999999' THEN '🟢 Current Price'
        ELSE '📜 Historical Price'
    END AS Status
FROM dbo.Products FOR SYSTEM_TIME ALL
WHERE ProductName LIKE '%Headphones%'
ORDER BY ValidFrom;

PRINT '';

-- =====================================================
-- DEMO STEP 4: CHANGE DETECTION AND ANALYSIS
-- =====================================================

PRINT '🔍 DEMO STEP 4: Change Detection and Analysis';
PRINT '=============================================';
PRINT '';
PRINT '📈 Salary increases in the last hour:';

WITH SalaryChanges AS (
    SELECT 
        EmployeeId,
        FirstName + ' ' + LastName AS Name,
        Salary,
        ValidFrom,
        LAG(Salary) OVER (PARTITION BY EmployeeId ORDER BY ValidFrom) AS PreviousSalary
    FROM dbo.Employees FOR SYSTEM_TIME ALL
    WHERE ValidFrom >= DATEADD(HOUR, -1, GETDATE())
)
SELECT 
    Name,
    FORMAT(PreviousSalary, 'C') AS OldSalary,
    FORMAT(Salary, 'C') AS NewSalary,
    FORMAT(Salary - PreviousSalary, 'C') AS Increase,
    FORMAT((Salary - PreviousSalary) / PreviousSalary * 100, 'N1') + '%' AS PercentIncrease,
    ValidFrom AS ChangeDate,
    '🎉 Congratulations!' AS Note
FROM SalaryChanges
WHERE PreviousSalary IS NOT NULL 
  AND Salary > PreviousSalary
ORDER BY (Salary - PreviousSalary) DESC;

PRINT '';
PRINT '⚙️  System configuration changes in the last hour:';

SELECT 
    ConfigKey AS Setting,
    ConfigValue AS NewValue,
    ValidFrom AS ChangedAt,
    DATEDIFF(SECOND, LAG(ValidFrom) OVER (PARTITION BY ConfigKey ORDER BY ValidFrom), ValidFrom) AS SecondsFromPreviousChange,
    '🔧 Config Update' AS Note
FROM dbo.SystemConfig FOR SYSTEM_TIME ALL
WHERE ValidFrom >= DATEADD(HOUR, -1, GETDATE())
  AND ValidTo != '9999-12-31 23:59:59.9999999' -- Only completed changes
ORDER BY ValidFrom DESC;

PRINT '';

-- =====================================================
-- DEMO STEP 5: BUSINESS INTELLIGENCE SCENARIOS
-- =====================================================

PRINT '📊 DEMO STEP 5: Business Intelligence with Temporal Data';
PRINT '========================================================';
PRINT '';
PRINT '🏢 Department transfer analysis:';

WITH Transfers AS (
    SELECT 
        EmployeeId,
        FirstName + ' ' + LastName AS Name,
        Department,
        ValidFrom,
        LAG(Department) OVER (PARTITION BY EmployeeId ORDER BY ValidFrom) AS PreviousDept
    FROM dbo.Employees FOR SYSTEM_TIME ALL
)
SELECT 
    Name,
    PreviousDept + ' ➤ ' + Department AS Transfer,
    ValidFrom AS TransferDate,
    '🔄 Department Change' AS Type
FROM Transfers
WHERE PreviousDept IS NOT NULL 
  AND PreviousDept != Department
ORDER BY ValidFrom DESC;

PRINT '';
PRINT '💎 Customer tier progression:';

WITH TierChanges AS (
    SELECT 
        CustomerId,
        FirstName + ' ' + LastName AS CustomerName,
        CustomerTier,
        ValidFrom,
        LAG(CustomerTier) OVER (PARTITION BY CustomerId ORDER BY ValidFrom) AS PreviousTier
    FROM dbo.Customers FOR SYSTEM_TIME ALL
)
SELECT 
    CustomerName,
    PreviousTier + ' ➤ ' + CustomerTier AS Progression,
    ValidFrom AS UpgradeDate,
    CASE 
        WHEN CustomerTier = 'Silver' THEN '🥈 Silver Tier'
        WHEN CustomerTier = 'Gold' THEN '🥇 Gold Tier'  
        WHEN CustomerTier = 'Platinum' THEN '💎 Platinum Tier'
        ELSE '🥉 ' + CustomerTier + ' Tier'
    END AS NewStatus
FROM TierChanges
WHERE PreviousTier IS NOT NULL 
  AND PreviousTier != CustomerTier
ORDER BY ValidFrom DESC;

PRINT '';

-- =====================================================
-- DEMO STEP 6: COMPLIANCE AND AUDIT SCENARIOS
-- =====================================================

PRINT '🛡️  DEMO STEP 6: Compliance and Audit Capabilities';
PRINT '==================================================';
PRINT '';
PRINT '📋 Audit trail for high-value transactions:';

WITH BalanceChanges AS (
    SELECT 
        ab.AccountId,
        c.FirstName + ' ' + c.LastName AS CustomerName,
        ab.AccountNumber,
        ab.Balance,
        ab.ValidFrom,
        LAG(ab.Balance) OVER (PARTITION BY ab.AccountId ORDER BY ab.ValidFrom) AS PreviousBalance
    FROM dbo.AccountBalances FOR SYSTEM_TIME ALL ab
    INNER JOIN dbo.Customers c ON ab.CustomerId = c.CustomerId
)
SELECT 
    CustomerName,
    AccountNumber,
    FORMAT(PreviousBalance, 'C') AS PreviousBalance,
    FORMAT(Balance, 'C') AS NewBalance,
    FORMAT(ABS(Balance - PreviousBalance), 'C') AS ChangeAmount,
    ValidFrom AS TransactionTime,
    CASE 
        WHEN Balance > PreviousBalance THEN '💰 Deposit'
        ELSE '💸 Withdrawal/Payment'
    END AS TransactionType,
    '🔍 Audit Trail' AS Note
FROM BalanceChanges
WHERE PreviousBalance IS NOT NULL 
  AND ABS(Balance - PreviousBalance) >= 1000 -- High-value changes
ORDER BY ABS(Balance - PreviousBalance) DESC;

PRINT '';
PRINT '🔒 Security configuration audit:';

SELECT 
    ConfigKey AS SecuritySetting,
    ConfigValue AS Value,
    ValidFrom AS ChangedAt,
    CASE 
        WHEN ConfigKey = 'MAX_LOGIN_ATTEMPTS' THEN '🔐 Login Security'
        WHEN ConfigKey = 'SESSION_TIMEOUT' THEN '⏱️  Session Management'
        WHEN ConfigKey = 'MAINTENANCE_MODE' THEN '🔧 System Maintenance'
        ELSE '⚙️  System Config'
    END AS Category,
    '📊 Security Audit' AS AuditType
FROM dbo.SystemConfig FOR SYSTEM_TIME ALL
WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'MAINTENANCE_MODE')
  AND ValidTo != '9999-12-31 23:59:59.9999999' -- Historical changes only
ORDER BY ValidFrom DESC;

PRINT '';

-- =====================================================
-- DEMO CONCLUSION
-- =====================================================

PRINT '🎯 DEMO CONCLUSION: The Power of Temporal Tables';
PRINT '================================================';
PRINT '';
PRINT '✅ What we''ve demonstrated:';
PRINT '';
PRINT '   🕐 Time Travel Queries    - See data as it existed at any point in time';
PRINT '   📋 Complete Audit Trails  - Every change is automatically tracked';
PRINT '   🔍 Change Detection       - Identify what changed, when, and by how much';
PRINT '   📊 Historical Analytics   - Trend analysis and business intelligence';
PRINT '   🛡️  Compliance Support    - Meet regulatory requirements (SOX, GDPR, etc.)';
PRINT '   🔒 Security Auditing      - Track configuration and access changes';
PRINT '';
PRINT '💡 Key Benefits:';
PRINT '';
PRINT '   • Zero application changes required - SQL Server handles everything';
PRINT '   • Automatic timestamp management - no manual tracking needed';
PRINT '   • Point-in-time recovery capabilities';
PRINT '   • Simplified compliance reporting';
PRINT '   • Enhanced data forensics and debugging';
PRINT '   • Business intelligence with historical context';
PRINT '';
PRINT '🚀 Use Cases:';
PRINT '';
PRINT '   • Financial systems (audit trails, reconciliation)';
PRINT '   • Healthcare (patient record history, compliance)';
PRINT '   • E-commerce (price history, inventory tracking)';
PRINT '   • HR systems (salary history, role changes)';
PRINT '   • Configuration management (system settings, security)';
PRINT '   • Regulatory compliance (SOX, GDPR, HIPAA)';
PRINT '';
PRINT '╔══════════════════════════════════════════════════════════════════════════════╗';
PRINT '║                           DEMO COMPLETED                                     ║';
PRINT '║                                                                              ║';
PRINT '║  Temporal tables provide powerful time-based data management capabilities    ║';
PRINT '║  with minimal overhead and maximum flexibility for modern applications.      ║';
PRINT '╚══════════════════════════════════════════════════════════════════════════════╝';

-- Show final statistics
PRINT '';
PRINT '📈 Demo Statistics:';
SELECT 
    'Total Tables' AS Metric, 
    CAST(COUNT(*) AS VARCHAR) AS Value
FROM sys.tables 
WHERE temporal_type = 2

UNION ALL

SELECT 
    'Total Historical Records',
    CAST(SUM(row_count) AS VARCHAR)
FROM (
    SELECT COUNT(*) as row_count FROM dbo.EmployeesHistory
    UNION ALL SELECT COUNT(*) FROM dbo.ProductsHistory
    UNION ALL SELECT COUNT(*) FROM dbo.CustomersHistory
    UNION ALL SELECT COUNT(*) FROM dbo.AccountBalancesHistory
    UNION ALL SELECT COUNT(*) FROM dbo.SystemConfigHistory
) h

UNION ALL

SELECT 
    'Demo Duration',
    CAST(DATEDIFF(SECOND, (SELECT MIN(ValidFrom) FROM dbo.Employees FOR SYSTEM_TIME ALL), GETDATE()) AS VARCHAR) + ' seconds';

GO