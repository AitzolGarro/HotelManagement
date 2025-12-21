-- Temporal Database Demo - Create Temporal Tables
-- This script creates various temporal tables for different demo scenarios

USE TemporalDemo;
GO

-- =====================================================
-- 1. EMPLOYEE MANAGEMENT TEMPORAL TABLE
-- =====================================================

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Employees SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE IF EXISTS dbo.Employees;
    DROP TABLE IF EXISTS dbo.EmployeesHistory;
END
GO

-- Create Employees temporal table
CREATE TABLE dbo.Employees
(
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Department NVARCHAR(50) NOT NULL,
    Position NVARCHAR(50) NOT NULL,
    Salary DECIMAL(10,2) NOT NULL,
    HireDate DATE NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Temporal columns (system-managed)
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.EmployeesHistory));

PRINT 'Employees temporal table created.';

-- =====================================================
-- 2. PRODUCT CATALOG TEMPORAL TABLE
-- =====================================================

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Products SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE IF EXISTS dbo.Products;
    DROP TABLE IF EXISTS dbo.ProductsHistory;
END
GO

-- Create Products temporal table
CREATE TABLE dbo.Products
(
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Cost DECIMAL(10,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    Description NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Temporal columns
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ProductsHistory));

PRINT 'Products temporal table created.';

-- =====================================================
-- 3. CUSTOMER DATA TEMPORAL TABLE
-- =====================================================

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Customers SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE IF EXISTS dbo.Customers;
    DROP TABLE IF EXISTS dbo.CustomersHistory;
END
GO

-- Create Customers temporal table
CREATE TABLE dbo.Customers
(
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Address NVARCHAR(200),
    City NVARCHAR(50),
    State NVARCHAR(50),
    ZipCode NVARCHAR(10),
    CustomerTier NVARCHAR(20) DEFAULT 'Bronze',
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Temporal columns
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.CustomersHistory));

PRINT 'Customers temporal table created.';

-- =====================================================
-- 4. ACCOUNT BALANCES TEMPORAL TABLE
-- =====================================================

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.AccountBalances', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AccountBalances SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE IF EXISTS dbo.AccountBalances;
    DROP TABLE IF EXISTS dbo.AccountBalancesHistory;
END
GO

-- Create AccountBalances temporal table
CREATE TABLE dbo.AccountBalances
(
    AccountId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
    AccountType NVARCHAR(20) NOT NULL,
    Balance DECIMAL(15,2) NOT NULL DEFAULT 0,
    CreditLimit DECIMAL(15,2) DEFAULT 0,
    InterestRate DECIMAL(5,4) DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Temporal columns
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),
    
    -- Foreign key constraint
    CONSTRAINT FK_AccountBalances_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AccountBalancesHistory));

PRINT 'AccountBalances temporal table created.';

-- =====================================================
-- 5. CONFIGURATION SETTINGS TEMPORAL TABLE
-- =====================================================

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.SystemConfig', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SystemConfig SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE IF EXISTS dbo.SystemConfig;
    DROP TABLE IF EXISTS dbo.SystemConfigHistory;
END
GO

-- Create SystemConfig temporal table
CREATE TABLE dbo.SystemConfig
(
    ConfigId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
    ConfigValue NVARCHAR(500) NOT NULL,
    Description NVARCHAR(200),
    Category NVARCHAR(50) DEFAULT 'General',
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Temporal columns
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.SystemConfigHistory));

PRINT 'SystemConfig temporal table created.';

-- =====================================================
-- VERIFY TEMPORAL TABLES CREATION
-- =====================================================

-- Query to show all temporal tables in the database
SELECT 
    t.name AS TableName,
    h.name AS HistoryTableName,
    t.temporal_type_desc AS TemporalType,
    CASE 
        WHEN t.temporal_type = 2 THEN 'System-Versioned Temporal Table'
        WHEN t.temporal_type = 1 THEN 'History Table'
        ELSE 'Regular Table'
    END AS TableType
FROM sys.tables t
LEFT JOIN sys.tables h ON t.history_table_id = h.object_id
WHERE t.temporal_type IN (1, 2)
ORDER BY t.name;

PRINT '';
PRINT 'Temporal tables setup completed successfully!';
PRINT 'Tables created: Employees, Products, Customers, AccountBalances, SystemConfig';
PRINT 'Each table has automatic history tracking enabled.';
GO