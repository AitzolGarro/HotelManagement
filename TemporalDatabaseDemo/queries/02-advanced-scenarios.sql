-- Temporal Database Demo - Advanced Scenarios
-- This script demonstrates complex real-world temporal query scenarios

USE TemporalDemo;
GO

PRINT '=== ADVANCED TEMPORAL SCENARIOS ===';
PRINT '';

-- =====================================================
-- SCENARIO 1: COMPLIANCE AND AUDIT REPORTING
-- =====================================================

PRINT 'SCENARIO 1: COMPLIANCE AND AUDIT REPORTING';
PRINT '==========================================';

-- SOX Compliance: Show all salary changes above $10,000 in the last year
DECLARE @OneYearAgo DATETIME2 = DATEADD(YEAR, -1, GETDATE());

WITH SalaryChanges AS (
    SELECT 
        EmployeeId,
        FirstName + ' ' + LastName AS FullName,
        Salary,
        ValidFrom,
        ValidTo,
        LAG(Salary) OVER (PARTITION BY EmployeeId ORDER BY ValidFrom) AS PreviousSalary
    FROM dbo.Employees FOR SYSTEM_TIME ALL
    WHERE ValidFrom >= @OneYearAgo
)
SELECT 
    FullName,
    FORMAT(PreviousSalary, 'C') AS OldSalary,
    FORMAT(Salary, 'C') AS NewSalary,
    FORMAT(Salary - PreviousSalary, 'C') AS SalaryIncrease,
    FORMAT((Salary - PreviousSalary) / PreviousSalary * 100, 'N2') + '%' AS PercentIncrease,
    ValidFrom AS ChangeDate,
    'Requires Approval Review' AS ComplianceNote
FROM SalaryChanges
WHERE PreviousSalary IS NOT NULL 
  AND (Salary - PreviousSalary) >= 10000
ORDER BY (Salary - PreviousSalary) DESC;

PRINT 'High-value salary changes requiring compliance review shown above.';
PRINT '';

-- GDPR Compliance: Track all customer data changes for data subject requests
SELECT 
    c.CustomerId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    c.Email,
    c.Phone,
    c.Address + ', ' + c.City + ', ' + c.State + ' ' + c.ZipCode AS FullAddress,
    c.ValidFrom AS RecordValidFrom,
    c.ValidTo AS RecordValidTo,
    CASE 
        WHEN c.ValidTo = '9999-12-31 23:59:59.9999999' THEN 'Current Record'
        ELSE 'Historical Record'
    END AS RecordStatus,
    DATEDIFF(DAY, c.ValidFrom, ISNULL(NULLIF(c.ValidTo, '9999-12-31 23:59:59.9999999'), GETDATE())) AS DaysActive
FROM dbo.Customers FOR SYSTEM_TIME ALL c
WHERE c.FirstName = 'Alice' AND c.LastName = 'Cooper' -- Data subject request
ORDER BY c.ValidFrom;

PRINT 'Complete data history for Alice Cooper (GDPR data subject request) shown above.';
PRINT '';

-- =====================================================
-- SCENARIO 2: FINANCIAL AUDIT AND RECONCILIATION
-- =====================================================

PRINT 'SCENARIO 2: FINANCIAL AUDIT AND RECONCILIATION';
PRINT '===============================================';

-- Monthly account balance reconciliation
DECLARE @MonthStart DATETIME2 = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
DECLARE @MonthEnd DATETIME2 = EOMONTH(GETDATE());

WITH MonthlyBalances AS (
    SELECT 
        ab.CustomerId,
        c.FirstName + ' ' + c.LastName AS CustomerName,
        ab.AccountNumber,
        ab.AccountType,
        ab.Balance,
        ab.ValidFrom,
        ab.ValidTo,
        ROW_NUMBER() OVER (PARTITION BY ab.AccountId ORDER BY ab.ValidFrom DESC) as rn
    FROM dbo.AccountBalances FOR SYSTEM_TIME BETWEEN @MonthStart AND @MonthEnd ab
    INNER JOIN dbo.Customers c ON ab.CustomerId = c.CustomerId
)
SELECT 
    CustomerName,
    AccountNumber,
    AccountType,
    FORMAT(Balance, 'C') AS MonthEndBalance,
    ValidFrom AS LastChangeDate,
    DATEDIFF(DAY, ValidFrom, GETDATE()) AS DaysSinceLastChange
FROM MonthlyBalances
WHERE rn = 1 -- Most recent change in the month
ORDER BY CustomerName, AccountType;

PRINT 'Month-end account balance reconciliation shown above.';
PRINT '';

-- Detect suspicious account activity (multiple large changes)
WITH AccountActivity AS (
    SELECT 
        ab.AccountId,
        ab.CustomerId,
        c.FirstName + ' ' + c.LastName AS CustomerName,
        ab.AccountNumber,
        ab.Balance,
        ab.ValidFrom,
        LAG(ab.Balance) OVER (PARTITION BY ab.AccountId ORDER BY ab.ValidFrom) AS PreviousBalance,
        COUNT(*) OVER (PARTITION BY ab.AccountId) AS TotalChanges
    FROM dbo.AccountBalances FOR SYSTEM_TIME ALL ab
    INNER JOIN dbo.Customers c ON ab.CustomerId = c.CustomerId
    WHERE ab.ValidFrom >= DATEADD(DAY, -30, GETDATE()) -- Last 30 days
)
SELECT 
    CustomerName,
    AccountNumber,
    FORMAT(PreviousBalance, 'C') AS PreviousBalance,
    FORMAT(Balance, 'C') AS NewBalance,
    FORMAT(ABS(Balance - PreviousBalance), 'C') AS ChangeAmount,
    ValidFrom AS ChangeDate,
    TotalChanges AS TotalChangesLast30Days,
    CASE 
        WHEN ABS(Balance - PreviousBalance) > 5000 THEN 'High Value Change'
        WHEN TotalChanges > 5 THEN 'High Frequency Changes'
        ELSE 'Normal Activity'
    END AS RiskLevel
FROM AccountActivity
WHERE PreviousBalance IS NOT NULL
  AND (ABS(Balance - PreviousBalance) > 5000 OR TotalChanges > 5)
ORDER BY ABS(Balance - PreviousBalance) DESC;

PRINT 'Suspicious account activity analysis shown above.';
PRINT '';

-- =====================================================
-- SCENARIO 3: BUSINESS INTELLIGENCE AND TRENDS
-- =====================================================

PRINT 'SCENARIO 3: BUSINESS INTELLIGENCE AND TRENDS';
PRINT '=============================================';

-- Employee retention analysis: Track department changes
WITH DepartmentHistory AS (
    SELECT 
        EmployeeId,
        FirstName + ' ' + LastName AS FullName,
        Department,
        ValidFrom,
        ValidTo,
        LAG(Department) OVER (PARTITION BY EmployeeId ORDER BY ValidFrom) AS PreviousDepartment,
        ROW_NUMBER() OVER (PARTITION BY EmployeeId ORDER BY ValidFrom) AS ChangeSequence
    FROM dbo.Employees FOR SYSTEM_TIME ALL
)
SELECT 
    FullName,
    PreviousDepartment AS FromDepartment,
    Department AS ToDepartment,
    ValidFrom AS TransferDate,
    ChangeSequence - 1 AS TransferNumber, -- Subtract 1 because first record is hire, not transfer
    CASE 
        WHEN ChangeSequence = 1 THEN 'Initial Hire'
        WHEN PreviousDepartment != Department THEN 'Department Transfer'
        ELSE 'Role Change Within Department'
    END AS ChangeType
FROM DepartmentHistory
WHERE ChangeSequence > 1 -- Exclude initial hires
  AND PreviousDepartment IS NOT NULL
ORDER BY TransferDate DESC;

PRINT 'Employee department transfer history shown above.';
PRINT '';

-- Product pricing strategy analysis
WITH PriceHistory AS (
    SELECT 
        ProductId,
        ProductName,
        Category,
        Price,
        ValidFrom,
        ValidTo,
        LAG(Price) OVER (PARTITION BY ProductId ORDER BY ValidFrom) AS PreviousPrice,
        FIRST_VALUE(Price) OVER (PARTITION BY ProductId ORDER BY ValidFrom ROWS UNBOUNDED PRECEDING) AS OriginalPrice
    FROM dbo.Products FOR SYSTEM_TIME ALL
)
SELECT 
    ProductName,
    Category,
    FORMAT(OriginalPrice, 'C') AS LaunchPrice,
    FORMAT(PreviousPrice, 'C') AS PreviousPrice,
    FORMAT(Price, 'C') AS CurrentPrice,
    FORMAT(Price - PreviousPrice, 'C') AS LastPriceChange,
    FORMAT(Price - OriginalPrice, 'C') AS TotalPriceChange,
    FORMAT((Price - OriginalPrice) / OriginalPrice * 100, 'N2') + '%' AS TotalPriceChangePercent,
    ValidFrom AS LastChangeDate,
    CASE 
        WHEN Price > PreviousPrice THEN 'Price Increase'
        WHEN Price < PreviousPrice THEN 'Price Decrease'
        ELSE 'No Change'
    END AS LastChangeDirection
FROM PriceHistory
WHERE PreviousPrice IS NOT NULL
  AND ValidTo = '9999-12-31 23:59:59.9999999' -- Current records only
ORDER BY ABS(Price - OriginalPrice) DESC;

PRINT 'Product pricing strategy analysis shown above.';
PRINT '';

-- =====================================================
-- SCENARIO 4: SYSTEM CONFIGURATION AUDIT
-- =====================================================

PRINT 'SCENARIO 4: SYSTEM CONFIGURATION AUDIT';
PRINT '======================================';

-- Track critical security configuration changes
WITH SecurityConfigChanges AS (
    SELECT 
        ConfigKey,
        ConfigValue,
        Description,
        ValidFrom,
        ValidTo,
        LAG(ConfigValue) OVER (PARTITION BY ConfigKey ORDER BY ValidFrom) AS PreviousValue
    FROM dbo.SystemConfig FOR SYSTEM_TIME ALL
    WHERE ConfigKey IN ('MAX_LOGIN_ATTEMPTS', 'SESSION_TIMEOUT', 'PASSWORD_MIN_LENGTH', 'MAINTENANCE_MODE')
)
SELECT 
    ConfigKey,
    Description,
    PreviousValue AS OldValue,
    ConfigValue AS NewValue,
    ValidFrom AS ChangeTimestamp,
    CASE 
        WHEN ConfigKey = 'MAX_LOGIN_ATTEMPTS' AND CAST(ConfigValue AS INT) < CAST(PreviousValue AS INT) THEN 'Security Tightened'
        WHEN ConfigKey = 'SESSION_TIMEOUT' AND CAST(ConfigValue AS INT) > CAST(PreviousValue AS INT) THEN 'User Experience Improved'
        WHEN ConfigKey = 'MAINTENANCE_MODE' AND ConfigValue = 'true' THEN 'System Maintenance Started'
        WHEN ConfigKey = 'MAINTENANCE_MODE' AND ConfigValue = 'false' THEN 'System Maintenance Ended'
        ELSE 'Configuration Updated'
    END AS ChangeImpact,
    DATEDIFF(MINUTE, LAG(ValidFrom) OVER (PARTITION BY ConfigKey ORDER BY ValidFrom), ValidFrom) AS MinutesSinceLastChange
FROM SecurityConfigChanges
WHERE PreviousValue IS NOT NULL
  AND PreviousValue != ConfigValue
ORDER BY ValidFrom DESC;

PRINT 'Critical security configuration changes shown above.';
PRINT '';

-- =====================================================
-- SCENARIO 5: DATA QUALITY AND CONSISTENCY CHECKS
-- =====================================================

PRINT 'SCENARIO 5: DATA QUALITY AND CONSISTENCY CHECKS';
PRINT '================================================';

-- Find records that were corrected quickly (potential data entry errors)
WITH QuickCorrections AS (
    SELECT 
        'Employee' AS TableType,
        e1.EmployeeId AS RecordId,
        e1.FirstName + ' ' + e1.LastName AS RecordIdentifier,
        'Salary changed from ' + FORMAT(e1.Salary, 'C') + ' to ' + FORMAT(e2.Salary, 'C') AS ChangeDescription,
        e1.ValidFrom AS FirstChangeTime,
        e2.ValidFrom AS CorrectionTime,
        DATEDIFF(MINUTE, e1.ValidFrom, e2.ValidFrom) AS MinutesToCorrection
    FROM dbo.Employees FOR SYSTEM_TIME ALL e1
    INNER JOIN dbo.Employees FOR SYSTEM_TIME ALL e2 
        ON e1.EmployeeId = e2.EmployeeId 
        AND e2.ValidFrom = e1.ValidTo
    WHERE DATEDIFF(MINUTE, e1.ValidFrom, e2.ValidFrom) <= 5 -- Corrected within 5 minutes
      AND e1.ValidTo != '9999-12-31 23:59:59.9999999'
    
    UNION ALL
    
    SELECT 
        'Product' AS TableType,
        p1.ProductId AS RecordId,
        p1.ProductName AS RecordIdentifier,
        'Price changed from ' + FORMAT(p1.Price, 'C') + ' to ' + FORMAT(p2.Price, 'C') AS ChangeDescription,
        p1.ValidFrom AS FirstChangeTime,
        p2.ValidFrom AS CorrectionTime,
        DATEDIFF(MINUTE, p1.ValidFrom, p2.ValidFrom) AS MinutesToCorrection
    FROM dbo.Products FOR SYSTEM_TIME ALL p1
    INNER JOIN dbo.Products FOR SYSTEM_TIME ALL p2 
        ON p1.ProductId = p2.ProductId 
        AND p2.ValidFrom = p1.ValidTo
    WHERE DATEDIFF(MINUTE, p1.ValidFrom, p2.ValidFrom) <= 5
      AND p1.ValidTo != '9999-12-31 23:59:59.9999999'
)
SELECT 
    TableType,
    RecordIdentifier,
    ChangeDescription,
    FirstChangeTime,
    CorrectionTime,
    MinutesToCorrection,
    'Potential Data Entry Error' AS DataQualityFlag
FROM QuickCorrections
ORDER BY MinutesToCorrection;

PRINT 'Potential data entry errors (quick corrections) shown above.';
PRINT '';

-- =====================================================
-- SCENARIO 6: TEMPORAL JOINS AND COMPLEX ANALYSIS
-- =====================================================

PRINT 'SCENARIO 6: TEMPORAL JOINS AND COMPLEX ANALYSIS';
PRINT '================================================';

-- Customer tier progression analysis with account balance correlation
WITH CustomerTierHistory AS (
    SELECT 
        CustomerId,
        FirstName + ' ' + LastName AS CustomerName,
        CustomerTier,
        ValidFrom AS TierChangeDate,
        ValidTo AS TierEndDate,
        LAG(CustomerTier) OVER (PARTITION BY CustomerId ORDER BY ValidFrom) AS PreviousTier
    FROM dbo.Customers FOR SYSTEM_TIME ALL
),
AccountBalanceAtTierChange AS (
    SELECT 
        cth.CustomerId,
        cth.CustomerName,
        cth.PreviousTier,
        cth.CustomerTier AS NewTier,
        cth.TierChangeDate,
        ab.Balance AS BalanceAtTierChange,
        ab.AccountType
    FROM CustomerTierHistory cth
    CROSS APPLY (
        SELECT TOP 1 Balance, AccountType
        FROM dbo.AccountBalances FOR SYSTEM_TIME AS OF cth.TierChangeDate ab
        WHERE ab.CustomerId = cth.CustomerId
          AND ab.AccountType = 'Checking'
        ORDER BY ab.ValidFrom DESC
    ) ab
    WHERE cth.PreviousTier IS NOT NULL
      AND cth.PreviousTier != cth.CustomerTier
)
SELECT 
    CustomerName,
    PreviousTier + ' → ' + NewTier AS TierProgression,
    FORMAT(BalanceAtTierChange, 'C') AS AccountBalanceAtUpgrade,
    TierChangeDate,
    CASE 
        WHEN NewTier = 'Silver' AND BalanceAtTierChange >= 5000 THEN 'Balance Qualified'
        WHEN NewTier = 'Gold' AND BalanceAtTierChange >= 10000 THEN 'Balance Qualified'
        WHEN NewTier = 'Platinum' AND BalanceAtTierChange >= 25000 THEN 'Balance Qualified'
        ELSE 'Manual Override or Other Criteria'
    END AS UpgradeReason
FROM AccountBalanceAtTierChange
ORDER BY TierChangeDate DESC;

PRINT 'Customer tier progression with balance correlation shown above.';
PRINT '';

PRINT '=== ADVANCED TEMPORAL SCENARIOS COMPLETED ===';
PRINT '';
PRINT 'These scenarios demonstrate:';
PRINT '- Compliance and audit reporting (SOX, GDPR)';
PRINT '- Financial reconciliation and fraud detection';
PRINT '- Business intelligence and trend analysis';
PRINT '- System configuration auditing';
PRINT '- Data quality monitoring';
PRINT '- Complex temporal joins and correlations';
PRINT '';
PRINT 'Temporal tables provide powerful capabilities for:';
PRINT '✓ Regulatory compliance';
PRINT '✓ Audit trails';
PRINT '✓ Historical analysis';
PRINT '✓ Fraud detection';
PRINT '✓ Business intelligence';
PRINT '✓ Data quality assurance';

GO