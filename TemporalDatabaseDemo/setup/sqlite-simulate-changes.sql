-- SQLite Temporal Demo - Simulate Changes Over Time
-- This script simulates realistic data changes to demonstrate temporal features

SELECT 'Starting data change simulation at: ' || CURRENT_TIMESTAMP;

-- Add small delays between operations to create distinct timestamps
-- Note: SQLite doesn't have WAITFOR DELAY, so we'll use different approaches

-- =====================================================
-- SCENARIO 1: EMPLOYEE SALARY INCREASES AND PROMOTIONS
-- =====================================================

SELECT '=== SCENARIO 1: Employee Career Changes ===';

-- John Smith gets a promotion and salary increase
UPDATE Employees 
SET Position = 'Senior Software Developer', 
    Salary = 85000.00,
    Department = 'Engineering'
WHERE FirstName = 'John' AND LastName = 'Smith';

SELECT 'John Smith promoted to Senior Developer with salary increase.';

-- Sarah Johnson moves to a different department
UPDATE Employees 
SET Department = 'Product Management', 
    Position = 'Product Manager',
    Salary = 95000.00
WHERE FirstName = 'Sarah' AND LastName = 'Johnson';

SELECT 'Sarah Johnson transferred to Product Management.';

-- Michael Brown gets a significant raise
UPDATE Employees 
SET Salary = 110000.00,
    Position = 'Lead Developer'
WHERE FirstName = 'Michael' AND LastName = 'Brown';

SELECT 'Michael Brown promoted to Lead Developer.';

-- =====================================================
-- SCENARIO 2: PRODUCT PRICE CHANGES AND UPDATES
-- =====================================================

SELECT '=== SCENARIO 2: Product Catalog Changes ===';

-- Wireless Headphones price increase due to demand
UPDATE Products 
SET Price = 229.99,
    Description = 'Premium wireless headphones with advanced noise cancellation and 30-hour battery'
WHERE ProductName = 'Wireless Headphones';

SELECT 'Wireless Headphones price increased and description updated.';

-- Laptop Stand goes on sale
UPDATE Products 
SET Price = 59.99,
    StockQuantity = 125
WHERE ProductName = 'Laptop Stand';

SELECT 'Laptop Stand price reduced for sale.';

-- New product variant added (simulated as update)
UPDATE Products 
SET ProductName = 'Smartphone Case Pro',
    Price = 39.99,
    Description = 'Enhanced protective case with built-in screen protector'
WHERE ProductName = 'Smartphone Case';

SELECT 'Smartphone Case upgraded to Pro version.';

-- =====================================================
-- SCENARIO 3: CUSTOMER TIER UPGRADES AND ADDRESS CHANGES
-- =====================================================

SELECT '=== SCENARIO 3: Customer Profile Updates ===';

-- Alice Cooper moves and gets tier upgrade
UPDATE Customers 
SET Address = '789 Premium Blvd',
    City = 'Manhattan',
    ZipCode = '10002',
    CustomerTier = 'Platinum'
WHERE FirstName = 'Alice' AND LastName = 'Cooper';

SELECT 'Alice Cooper moved and upgraded to Platinum tier.';

-- Bob Miller updates contact information
UPDATE Customers 
SET Phone = '555-0199',
    Email = 'bob.miller.new@email.com',
    CustomerTier = 'Gold'
WHERE FirstName = 'Bob' AND LastName = 'Miller';

SELECT 'Bob Miller updated contact info and tier upgraded.';

-- Carol White changes address
UPDATE Customers 
SET Address = '456 New Street',
    City = 'Springfield',
    State = 'IL',
    ZipCode = '62701'
WHERE FirstName = 'Carol' AND LastName = 'White';

SELECT 'Carol White moved to new address.';

-- =====================================================
-- SCENARIO 4: SYSTEM CONFIGURATION CHANGES
-- =====================================================

SELECT '=== SCENARIO 4: System Configuration Updates ===';

-- Security policy update - increase max login attempts
UPDATE SystemConfig 
SET ConfigValue = '3'
WHERE ConfigKey = 'MAX_LOGIN_ATTEMPTS';

SELECT 'Security policy updated: Max login attempts reduced to 3.';

-- Session timeout increased for better user experience
UPDATE SystemConfig 
SET ConfigValue = '45'
WHERE ConfigKey = 'SESSION_TIMEOUT';

SELECT 'Session timeout increased to 45 minutes.';

-- Enable maintenance mode
UPDATE SystemConfig 
SET ConfigValue = 'true'
WHERE ConfigKey = 'MAINTENANCE_MODE';

SELECT 'Maintenance mode enabled.';

-- Disable maintenance mode (simulate quick toggle)
UPDATE SystemConfig 
SET ConfigValue = 'false'
WHERE ConfigKey = 'MAINTENANCE_MODE';

SELECT 'Maintenance mode disabled.';

-- Update API rate limit
UPDATE SystemConfig 
SET ConfigValue = '1500'
WHERE ConfigKey = 'API_RATE_LIMIT';

SELECT 'API rate limit increased to 1500 requests per hour.';

-- =====================================================
-- SCENARIO 5: EMPLOYEE STATUS CHANGES
-- =====================================================

SELECT '=== SCENARIO 5: Employee Status Changes ===';

-- Jennifer Martinez leaves the company
UPDATE Employees 
SET IsActive = 0
WHERE FirstName = 'Jennifer' AND LastName = 'Martinez';

SELECT 'Jennifer Martinez marked as inactive (left company).';

-- David Wilson gets another raise
UPDATE Employees 
SET Salary = 62000.00,
    Position = 'Senior Sales Representative'
WHERE FirstName = 'David' AND LastName = 'Wilson';

SELECT 'David Wilson promoted to Senior Sales Representative.';

-- =====================================================
-- SCENARIO 6: PRODUCT DISCONTINUATION
-- =====================================================

SELECT '=== SCENARIO 6: Product Lifecycle Changes ===';

-- Coffee Mug discontinued
UPDATE Products 
SET IsActive = 0,
    StockQuantity = 0,
    Description = 'DISCONTINUED - Ceramic coffee mug with company logo'
WHERE ProductName = 'Coffee Mug';

SELECT 'Coffee Mug product discontinued.';

-- Water Bottle gets a price increase
UPDATE Products 
SET Price = 29.99,
    Description = 'Premium stainless steel insulated water bottle - Now with improved insulation!'
WHERE ProductName = 'Water Bottle';

SELECT 'Water Bottle price increased with product improvements.';

-- =====================================================
-- SUMMARY OF CHANGES
-- =====================================================

SELECT '=== CHANGE SIMULATION COMPLETED ===';
SELECT 'Final timestamp: ' || CURRENT_TIMESTAMP;

SELECT 'Changes simulated:';
SELECT '- Employee promotions, salary increases, and department transfers';
SELECT '- Product price changes, descriptions updates, and discontinuations';
SELECT '- Customer address changes and tier upgrades';
SELECT '- System configuration policy updates';
SELECT '- Employee status changes (departures)';

SELECT 'All changes are now tracked in history tables!';
SELECT 'Use temporal-like queries to see the complete audit trail.';

-- Show summary of history records created
SELECT 'History Records Summary:';
SELECT 'EmployeesHistory' AS TableName, COUNT(*) AS HistoryRecords FROM EmployeesHistory
UNION ALL
SELECT 'ProductsHistory', COUNT(*) FROM ProductsHistory
UNION ALL
SELECT 'CustomersHistory', COUNT(*) FROM CustomersHistory
UNION ALL
SELECT 'SystemConfigHistory', COUNT(*) FROM SystemConfigHistory;