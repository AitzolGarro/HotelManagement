-- Temporal Database Demo - SQLite Version
-- This script creates temporal-like tables using SQLite triggers for history tracking

-- Enable foreign keys
PRAGMA foreign_keys = ON;

-- =====================================================
-- 1. EMPLOYEES TABLE WITH HISTORY TRACKING
-- =====================================================

-- Drop existing tables if they exist
DROP TABLE IF EXISTS EmployeesHistory;
DROP TABLE IF EXISTS Employees;

-- Create main Employees table
CREATE TABLE Employees (
    EmployeeId INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Department TEXT NOT NULL,
    Position TEXT NOT NULL,
    Salary DECIMAL(10,2) NOT NULL,
    HireDate DATE NOT NULL,
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME DEFAULT CURRENT_TIMESTAMP,
    ValidTo DATETIME DEFAULT '9999-12-31 23:59:59'
);

-- Create history table for Employees
CREATE TABLE EmployeesHistory (
    HistoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Department TEXT NOT NULL,
    Position TEXT NOT NULL,
    Salary DECIMAL(10,2) NOT NULL,
    HireDate DATE NOT NULL,
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME NOT NULL,
    ValidTo DATETIME NOT NULL,
    OperationType TEXT NOT NULL -- 'INSERT', 'UPDATE', 'DELETE'
);

-- Trigger to track INSERT operations
CREATE TRIGGER trg_employees_insert_history
AFTER INSERT ON Employees
BEGIN
    INSERT INTO EmployeesHistory (
        EmployeeId, FirstName, LastName, Email, Department, Position, 
        Salary, HireDate, IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.EmployeeId, NEW.FirstName, NEW.LastName, NEW.Email, NEW.Department, NEW.Position,
        NEW.Salary, NEW.HireDate, NEW.IsActive, NEW.ValidFrom, NEW.ValidTo, 'INSERT'
    );
END;

-- Trigger to track UPDATE operations
CREATE TRIGGER trg_employees_update_history
AFTER UPDATE ON Employees
BEGIN
    -- Close the previous record
    UPDATE EmployeesHistory 
    SET ValidTo = CURRENT_TIMESTAMP 
    WHERE EmployeeId = NEW.EmployeeId AND ValidTo = '9999-12-31 23:59:59';
    
    -- Insert new record
    INSERT INTO EmployeesHistory (
        EmployeeId, FirstName, LastName, Email, Department, Position, 
        Salary, HireDate, IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.EmployeeId, NEW.FirstName, NEW.LastName, NEW.Email, NEW.Department, NEW.Position,
        NEW.Salary, NEW.HireDate, NEW.IsActive, CURRENT_TIMESTAMP, '9999-12-31 23:59:59', 'UPDATE'
    );
    
    -- Update the main table's ValidFrom
    UPDATE Employees SET ValidFrom = CURRENT_TIMESTAMP WHERE EmployeeId = NEW.EmployeeId;
END;

-- =====================================================
-- 2. PRODUCTS TABLE WITH HISTORY TRACKING
-- =====================================================

DROP TABLE IF EXISTS ProductsHistory;
DROP TABLE IF EXISTS Products;

CREATE TABLE Products (
    ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductName TEXT NOT NULL,
    Category TEXT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Cost DECIMAL(10,2) NOT NULL,
    StockQuantity INTEGER DEFAULT 0,
    Description TEXT,
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME DEFAULT CURRENT_TIMESTAMP,
    ValidTo DATETIME DEFAULT '9999-12-31 23:59:59'
);

CREATE TABLE ProductsHistory (
    HistoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    ProductName TEXT NOT NULL,
    Category TEXT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Cost DECIMAL(10,2) NOT NULL,
    StockQuantity INTEGER DEFAULT 0,
    Description TEXT,
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME NOT NULL,
    ValidTo DATETIME NOT NULL,
    OperationType TEXT NOT NULL
);

CREATE TRIGGER trg_products_insert_history
AFTER INSERT ON Products
BEGIN
    INSERT INTO ProductsHistory (
        ProductId, ProductName, Category, Price, Cost, StockQuantity, 
        Description, IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.ProductId, NEW.ProductName, NEW.Category, NEW.Price, NEW.Cost, NEW.StockQuantity,
        NEW.Description, NEW.IsActive, NEW.ValidFrom, NEW.ValidTo, 'INSERT'
    );
END;

CREATE TRIGGER trg_products_update_history
AFTER UPDATE ON Products
BEGIN
    UPDATE ProductsHistory 
    SET ValidTo = CURRENT_TIMESTAMP 
    WHERE ProductId = NEW.ProductId AND ValidTo = '9999-12-31 23:59:59';
    
    INSERT INTO ProductsHistory (
        ProductId, ProductName, Category, Price, Cost, StockQuantity, 
        Description, IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.ProductId, NEW.ProductName, NEW.Category, NEW.Price, NEW.Cost, NEW.StockQuantity,
        NEW.Description, NEW.IsActive, CURRENT_TIMESTAMP, '9999-12-31 23:59:59', 'UPDATE'
    );
    
    UPDATE Products SET ValidFrom = CURRENT_TIMESTAMP WHERE ProductId = NEW.ProductId;
END;

-- =====================================================
-- 3. CUSTOMERS TABLE WITH HISTORY TRACKING
-- =====================================================

DROP TABLE IF EXISTS CustomersHistory;
DROP TABLE IF EXISTS Customers;

CREATE TABLE Customers (
    CustomerId INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    City TEXT,
    State TEXT,
    ZipCode TEXT,
    CustomerTier TEXT DEFAULT 'Bronze',
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME DEFAULT CURRENT_TIMESTAMP,
    ValidTo DATETIME DEFAULT '9999-12-31 23:59:59'
);

CREATE TABLE CustomersHistory (
    HistoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    City TEXT,
    State TEXT,
    ZipCode TEXT,
    CustomerTier TEXT DEFAULT 'Bronze',
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME NOT NULL,
    ValidTo DATETIME NOT NULL,
    OperationType TEXT NOT NULL
);

CREATE TRIGGER trg_customers_insert_history
AFTER INSERT ON Customers
BEGIN
    INSERT INTO CustomersHistory (
        CustomerId, FirstName, LastName, Email, Phone, Address, City, State, 
        ZipCode, CustomerTier, IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.CustomerId, NEW.FirstName, NEW.LastName, NEW.Email, NEW.Phone, NEW.Address, NEW.City, NEW.State,
        NEW.ZipCode, NEW.CustomerTier, NEW.IsActive, NEW.ValidFrom, NEW.ValidTo, 'INSERT'
    );
END;

CREATE TRIGGER trg_customers_update_history
AFTER UPDATE ON Customers
BEGIN
    UPDATE CustomersHistory 
    SET ValidTo = CURRENT_TIMESTAMP 
    WHERE CustomerId = NEW.CustomerId AND ValidTo = '9999-12-31 23:59:59';
    
    INSERT INTO CustomersHistory (
        CustomerId, FirstName, LastName, Email, Phone, Address, City, State, 
        ZipCode, CustomerTier, IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.CustomerId, NEW.FirstName, NEW.LastName, NEW.Email, NEW.Phone, NEW.Address, NEW.City, NEW.State,
        NEW.ZipCode, NEW.CustomerTier, NEW.IsActive, CURRENT_TIMESTAMP, '9999-12-31 23:59:59', 'UPDATE'
    );
    
    UPDATE Customers SET ValidFrom = CURRENT_TIMESTAMP WHERE CustomerId = NEW.CustomerId;
END;

-- =====================================================
-- 4. SYSTEM CONFIGURATION TABLE
-- =====================================================

DROP TABLE IF EXISTS SystemConfigHistory;
DROP TABLE IF EXISTS SystemConfig;

CREATE TABLE SystemConfig (
    ConfigId INTEGER PRIMARY KEY AUTOINCREMENT,
    ConfigKey TEXT NOT NULL UNIQUE,
    ConfigValue TEXT NOT NULL,
    Description TEXT,
    Category TEXT DEFAULT 'General',
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME DEFAULT CURRENT_TIMESTAMP,
    ValidTo DATETIME DEFAULT '9999-12-31 23:59:59'
);

CREATE TABLE SystemConfigHistory (
    HistoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    ConfigId INTEGER NOT NULL,
    ConfigKey TEXT NOT NULL,
    ConfigValue TEXT NOT NULL,
    Description TEXT,
    Category TEXT DEFAULT 'General',
    IsActive INTEGER DEFAULT 1,
    ValidFrom DATETIME NOT NULL,
    ValidTo DATETIME NOT NULL,
    OperationType TEXT NOT NULL
);

CREATE TRIGGER trg_systemconfig_insert_history
AFTER INSERT ON SystemConfig
BEGIN
    INSERT INTO SystemConfigHistory (
        ConfigId, ConfigKey, ConfigValue, Description, Category, 
        IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.ConfigId, NEW.ConfigKey, NEW.ConfigValue, NEW.Description, NEW.Category,
        NEW.IsActive, NEW.ValidFrom, NEW.ValidTo, 'INSERT'
    );
END;

CREATE TRIGGER trg_systemconfig_update_history
AFTER UPDATE ON SystemConfig
BEGIN
    UPDATE SystemConfigHistory 
    SET ValidTo = CURRENT_TIMESTAMP 
    WHERE ConfigId = NEW.ConfigId AND ValidTo = '9999-12-31 23:59:59';
    
    INSERT INTO SystemConfigHistory (
        ConfigId, ConfigKey, ConfigValue, Description, Category, 
        IsActive, ValidFrom, ValidTo, OperationType
    )
    VALUES (
        NEW.ConfigId, NEW.ConfigKey, NEW.ConfigValue, NEW.Description, NEW.Category,
        NEW.IsActive, CURRENT_TIMESTAMP, '9999-12-31 23:59:59', 'UPDATE'
    );
    
    UPDATE SystemConfig SET ValidFrom = CURRENT_TIMESTAMP WHERE ConfigId = NEW.ConfigId;
END;

-- =====================================================
-- VERIFICATION
-- =====================================================

-- Show created tables
SELECT 'Tables created successfully!' as Status;

-- List all tables
SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;