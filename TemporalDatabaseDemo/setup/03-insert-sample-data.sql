-- Temporal Database Demo - Insert Sample Data
-- This script inserts initial sample data into temporal tables

USE TemporalDemo;
GO

PRINT 'Inserting sample data into temporal tables...';

-- =====================================================
-- 1. INSERT EMPLOYEES DATA
-- =====================================================

INSERT INTO dbo.Employees (FirstName, LastName, Email, Department, Position, Salary, HireDate, IsActive)
VALUES 
    ('John', 'Smith', 'john.smith@company.com', 'Engineering', 'Software Developer', 75000.00, '2020-01-15', 1),
    ('Sarah', 'Johnson', 'sarah.johnson@company.com', 'Marketing', 'Marketing Manager', 85000.00, '2019-03-20', 1),
    ('Michael', 'Brown', 'michael.brown@company.com', 'Engineering', 'Senior Developer', 95000.00, '2018-06-10', 1),
    ('Emily', 'Davis', 'emily.davis@company.com', 'HR', 'HR Specialist', 65000.00, '2021-02-01', 1),
    ('David', 'Wilson', 'david.wilson@company.com', 'Sales', 'Sales Representative', 55000.00, '2020-09-15', 1),
    ('Lisa', 'Anderson', 'lisa.anderson@company.com', 'Engineering', 'DevOps Engineer', 88000.00, '2019-11-05', 1),
    ('Robert', 'Taylor', 'robert.taylor@company.com', 'Finance', 'Financial Analyst', 70000.00, '2020-04-12', 1),
    ('Jennifer', 'Martinez', 'jennifer.martinez@company.com', 'Marketing', 'Content Specialist', 58000.00, '2021-07-20', 1);

PRINT 'Employees data inserted.';

-- =====================================================
-- 2. INSERT PRODUCTS DATA
-- =====================================================

INSERT INTO dbo.Products (ProductName, Category, Price, Cost, StockQuantity, Description, IsActive)
VALUES 
    ('Wireless Headphones', 'Electronics', 199.99, 120.00, 150, 'Premium wireless headphones with noise cancellation', 1),
    ('Smartphone Case', 'Electronics', 29.99, 15.00, 500, 'Protective case for smartphones', 1),
    ('Laptop Stand', 'Office', 79.99, 45.00, 75, 'Adjustable aluminum laptop stand', 1),
    ('Coffee Mug', 'Kitchen', 14.99, 8.00, 200, 'Ceramic coffee mug with company logo', 1),
    ('Desk Lamp', 'Office', 89.99, 50.00, 100, 'LED desk lamp with adjustable brightness', 1),
    ('Water Bottle', 'Sports', 24.99, 12.00, 300, 'Stainless steel insulated water bottle', 1),
    ('Notebook Set', 'Office', 19.99, 10.00, 250, 'Set of 3 premium notebooks', 1),
    ('Wireless Mouse', 'Electronics', 49.99, 25.00, 180, 'Ergonomic wireless mouse', 1);

PRINT 'Products data inserted.';

-- =====================================================
-- 3. INSERT CUSTOMERS DATA
-- =====================================================

INSERT INTO dbo.Customers (FirstName, LastName, Email, Phone, Address, City, State, ZipCode, CustomerTier, IsActive)
VALUES 
    ('Alice', 'Cooper', 'alice.cooper@email.com', '555-0101', '123 Main St', 'New York', 'NY', '10001', 'Gold', 1),
    ('Bob', 'Miller', 'bob.miller@email.com', '555-0102', '456 Oak Ave', 'Los Angeles', 'CA', '90210', 'Silver', 1),
    ('Carol', 'White', 'carol.white@email.com', '555-0103', '789 Pine Rd', 'Chicago', 'IL', '60601', 'Bronze', 1),
    ('Daniel', 'Green', 'daniel.green@email.com', '555-0104', '321 Elm St', 'Houston', 'TX', '77001', 'Bronze', 1),
    ('Eva', 'Black', 'eva.black@email.com', '555-0105', '654 Maple Dr', 'Phoenix', 'AZ', '85001', 'Silver', 1),
    ('Frank', 'Blue', 'frank.blue@email.com', '555-0106', '987 Cedar Ln', 'Philadelphia', 'PA', '19101', 'Gold', 1),
    ('Grace', 'Red', 'grace.red@email.com', '555-0107', '147 Birch Way', 'San Antonio', 'TX', '78201', 'Bronze', 1),
    ('Henry', 'Gray', 'henry.gray@email.com', '555-0108', '258 Spruce St', 'San Diego', 'CA', '92101', 'Silver', 1);

PRINT 'Customers data inserted.';

-- =====================================================
-- 4. INSERT ACCOUNT BALANCES DATA
-- =====================================================

INSERT INTO dbo.AccountBalances (CustomerId, AccountNumber, AccountType, Balance, CreditLimit, InterestRate, IsActive)
VALUES 
    (1, 'ACC-001-2024', 'Checking', 5250.75, 0, 0.0025, 1),
    (1, 'ACC-001-2024-SAV', 'Savings', 15000.00, 0, 0.0150, 1),
    (2, 'ACC-002-2024', 'Checking', 3420.50, 0, 0.0025, 1),
    (2, 'ACC-002-2024-CC', 'Credit', -1250.00, 5000.00, 0.1899, 1),
    (3, 'ACC-003-2024', 'Checking', 8750.25, 0, 0.0025, 1),
    (4, 'ACC-004-2024', 'Checking', 2100.00, 0, 0.0025, 1),
    (4, 'ACC-004-2024-SAV', 'Savings', 25000.00, 0, 0.0200, 1),
    (5, 'ACC-005-2024', 'Checking', 4680.75, 0, 0.0025, 1),
    (6, 'ACC-006-2024', 'Checking', 12500.00, 0, 0.0025, 1),
    (6, 'ACC-006-2024-CC', 'Credit', -850.00, 10000.00, 0.1699, 1),
    (7, 'ACC-007-2024', 'Checking', 1950.50, 0, 0.0025, 1),
    (8, 'ACC-008-2024', 'Checking', 6300.25, 0, 0.0025, 1);

PRINT 'Account balances data inserted.';

-- =====================================================
-- 5. INSERT SYSTEM CONFIGURATION DATA
-- =====================================================

INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, Category, IsActive)
VALUES 
    ('MAX_LOGIN_ATTEMPTS', '5', 'Maximum number of failed login attempts before account lockout', 'Security', 1),
    ('SESSION_TIMEOUT', '30', 'Session timeout in minutes', 'Security', 1),
    ('DEFAULT_CURRENCY', 'USD', 'Default currency for the system', 'General', 1),
    ('EMAIL_NOTIFICATIONS', 'true', 'Enable email notifications', 'Notifications', 1),
    ('MAINTENANCE_MODE', 'false', 'System maintenance mode flag', 'System', 1),
    ('MAX_FILE_SIZE', '10485760', 'Maximum file upload size in bytes (10MB)', 'Upload', 1),
    ('BACKUP_RETENTION_DAYS', '90', 'Number of days to retain database backups', 'Backup', 1),
    ('API_RATE_LIMIT', '1000', 'API requests per hour per user', 'API', 1),
    ('PASSWORD_MIN_LENGTH', '8', 'Minimum password length requirement', 'Security', 1),
    ('CUSTOMER_SUPPORT_EMAIL', 'support@company.com', 'Customer support email address', 'Contact', 1);

PRINT 'System configuration data inserted.';

-- =====================================================
-- VERIFY DATA INSERTION
-- =====================================================

PRINT '';
PRINT 'Data insertion summary:';
SELECT 'Employees' AS TableName, COUNT(*) AS RecordCount FROM dbo.Employees
UNION ALL
SELECT 'Products', COUNT(*) FROM dbo.Products
UNION ALL
SELECT 'Customers', COUNT(*) FROM dbo.Customers
UNION ALL
SELECT 'AccountBalances', COUNT(*) FROM dbo.AccountBalances
UNION ALL
SELECT 'SystemConfig', COUNT(*) FROM dbo.SystemConfig;

PRINT '';
PRINT 'Sample data insertion completed successfully!';
PRINT 'All temporal tables now contain initial data with automatic history tracking.';

-- Show current timestamp for reference
SELECT GETDATE() AS CurrentTimestamp, 'Initial data load completed at this time' AS Note;

GO