-- Temporal Database Demo - Simulate Data Changes Over Time
-- This script simulates realistic data changes to demonstrate temporal features

USE TemporalDemo;
GO

PRINT 'Simulating data changes over time to demonstrate temporal features...';
PRINT 'Current time: ' + CONVERT(VARCHAR, GETDATE(), 120);

-- Add a small delay to ensure different timestamps
WAITFOR DELAY '00:00:02';

-- =====================================================
-- SCENARIO 1: EMPLOYEE SALARY INCREASES AND PROMOTIONS
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 1: Employee Career Changes ===';

-- John Smith gets a promotion and salary increase
UPDATE dbo.Employees 
SET Position = 'Senior Software Developer', 
    Salary = 85000.00,
    Department = 'Engineering'
WHERE FirstName = 'John' AND LastName = 'Smith';

PRINT 'John Smith promoted to Senior Developer with salary increase.';
WAITFOR DELAY '00:00:01';

-- Sarah Johnson moves to a different department
UPDATE dbo.Employees 
SET Department = 'Product Management', 
    Position = 'Product Manager',
    Salary = 95000.00
WHERE FirstName = 'Sarah' AND LastName = 'Johnson';

PRINT 'Sarah Johnson transferred to Product Management.';
WAITFOR DELAY '00:00:01';

-- Michael Brown gets a significant raise
UPDATE dbo.Employees 
SET Salary = 110000.00,
    Position = 'Lead Developer'
WHERE FirstName = 'Michael' AND LastName = 'Brown';

PRINT 'Michael Brown promoted to Lead Developer.';
WAITFOR DELAY '00:00:01';

-- =====================================================
-- SCENARIO 2: PRODUCT PRICE CHANGES AND UPDATES
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 2: Product Catalog Changes ===';

-- Wireless Headphones price increase due to demand
UPDATE dbo.Products 
SET Price = 229.99,
    Description = 'Premium wireless headphones with advanced noise cancellation and 30-hour battery'
WHERE ProductName = 'Wireless Headphones';

PRINT 'Wireless Headphones price increased and description updated.';
WAITFOR DELAY '00:00:01';

-- Laptop Stand goes on sale
UPDATE dbo.Products 
SET Price = 59.99,
    StockQuantity = 125
WHERE ProductName = 'Laptop Stand';

PRINT 'Laptop Stand price reduced for sale.';
WAITFOR DELAY '00:00:01';

-- New product variant added (simulated as update)
UPDATE dbo.Products 
SET ProductName = 'Smartphone Case Pro',
    Price = 39.99,
    Description = 'Enhanced protective case with built-in screen protector'
WHERE ProductName = 'Smartphone Case';

PRINT 'Smartphone Case upgraded to Pro version.';
WAITFOR DELAY '00:00:01';

-- =====================================================
-- SCENARIO 3: CUSTOMER TIER UPGRADES AND ADDRESS CHANGES
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 3: Customer Profile Updates ===';

-- Alice Cooper moves and gets tier upgrade
UPDATE dbo.Customers 
SET Address = '789 Premium Blvd',
    City = 'Manhattan',
    ZipCode = '10002',
    CustomerTier = 'Platinum'
WHERE FirstName = 'Alice' AND LastName = 'Cooper';

PRINT 'Alice Cooper moved and upgraded to Platinum tier.';
WAITFOR DELAY '00:00:01';

-- Bob Miller updates contact information
UPDATE dbo.Customers 
SET Phone = '555-0199',
    Email = 'bob.miller.new@email.com',
    CustomerTier = 'Gold'
WHERE FirstName = 'Bob' AND LastName = 'Miller';

PRINT 'Bob Miller updated contact info and tier upgraded.';
WAITFOR DELAY '00:00:01';

-- Carol White changes address
UPDATE dbo.Customers 
SET Address = '456 New Street',
    City = 'Springfield',
    State = 'IL',
    ZipCode = '62701'
WHERE FirstName = 'Carol' AND LastName = 'White';

PRINT 'Carol White moved to new address.';
WAITFOR DELAY '00:00:01';

-- =====================================================
-- SCENARIO 4: ACCOUNT BALANCE CHANGES (TRANSACTIONS)
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 4: Financial Transactions ===';

-- Alice makes a large deposit
UPDATE dbo.AccountBalances 
SET Balance = Balance + 10000.00
WHERE CustomerId = 1 AND AccountType = 'Savings';

PRINT 'Alice Cooper made a $10,000 deposit to savings.';
WAITFOR DELAY '00:00:01';

-- Bob pays down credit card
UPDATE dbo.AccountBalances 
SET Balance = Balance + 500.00  -- Less negative = payment
WHERE CustomerId = 2 AND AccountType = 'Credit';

PRINT 'Bob Miller made a $500 credit card payment.';
WAITFOR DELAY '00:00:01';

-- Daniel transfers money between accounts (simulate with balance changes)
UPDATE dbo.AccountBalances 
SET Balance = Balance - 1000.00
WHERE CustomerId = 4 AND AccountType = 'Savings';

UPDATE dbo.AccountBalances 
SET Balance = Balance + 1000.00
WHERE CustomerId = 4 AND AccountType = 'Checking';

PRINT 'Daniel Green transferred $1,000 from savings to checking.';
WAITFOR DELAY '00:00:01';

-- Frank gets interest payment
UPDATE dbo.AccountBalances 
SET Balance = Balance + 25.00
WHERE CustomerId = 6 AND AccountType = 'Checking';

PRINT 'Frank Blue received $25 interest payment.';
WAITFOR DELAY '00:00:01';

-- =====================================================
-- SCENARIO 5: SYSTEM CONFIGURATION CHANGES
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 5: System Configuration Updates ===';

-- Security policy update - increase max login attempts
UPDATE dbo.SystemConfig 
SET ConfigValue = '3'
WHERE ConfigKey = 'MAX_LOGIN_ATTEMPTS';

PRINT 'Security policy updated: Max login attempts reduced to 3.';
WAITFOR DELAY '00:00:01';

-- Session timeout increased for better user experience
UPDATE dbo.SystemConfig 
SET ConfigValue = '45'
WHERE ConfigKey = 'SESSION_TIMEOUT';

PRINT 'Session timeout increased to 45 minutes.';
WAITFOR DELAY '00:00:01';

-- Enable maintenance mode
UPDATE dbo.SystemConfig 
SET ConfigValue = 'true'
WHERE ConfigKey = 'MAINTENANCE_MODE';

PRINT 'Maintenance mode enabled.';
WAITFOR DELAY '00:00:02';

-- Disable maintenance mode
UPDATE dbo.SystemConfig 
SET ConfigValue = 'false'
WHERE ConfigKey = 'MAINTENANCE_MODE';

PRINT 'Maintenance mode disabled.';
WAITFOR DELAY '00:00:01';

-- Update API rate limit
UPDATE dbo.SystemConfig 
SET ConfigValue = '1500'
WHERE ConfigKey = 'API_RATE_LIMIT';

PRINT 'API rate limit increased to 1500 requests per hour.';
WAITFOR DELAY '00:00:01';

-- =====================================================
-- SCENARIO 6: EMPLOYEE DEPARTURES AND DEACTIVATIONS
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 6: Employee Status Changes ===';

-- Jennifer Martinez leaves the company
UPDATE dbo.Employees 
SET IsActive = 0
WHERE FirstName = 'Jennifer' AND LastName = 'Martinez';

PRINT 'Jennifer Martinez marked as inactive (left company).';
WAITFOR DELAY '00:00:01';

-- David Wilson gets another raise
UPDATE dbo.Employees 
SET Salary = 62000.00,
    Position = 'Senior Sales Representative'
WHERE FirstName = 'David' AND LastName = 'Wilson';

PRINT 'David Wilson promoted to Senior Sales Representative.';
WAITFOR DELAY '00:00:01';

-- =====================================================
-- SCENARIO 7: PRODUCT DISCONTINUATION
-- =====================================================

PRINT '';
PRINT '=== SCENARIO 7: Product Lifecycle Changes ===';

-- Coffee Mug discontinued
UPDATE dbo.Products 
SET IsActive = 0,
    StockQuantity = 0,
    Description = 'DISCONTINUED - Ceramic coffee mug with company logo'
WHERE ProductName = 'Coffee Mug';

PRINT 'Coffee Mug product discontinued.';
WAITFOR DELAY '00:00:01';

-- Water Bottle gets a price increase
UPDATE dbo.Products 
SET Price = 29.99,
    Description = 'Premium stainless steel insulated water bottle - Now with improved insulation!'
WHERE ProductName = 'Water Bottle';

PRINT 'Water Bottle price increased with product improvements.';

-- =====================================================
-- SUMMARY OF CHANGES
-- =====================================================

PRINT '';
PRINT '=== CHANGE SIMULATION COMPLETED ===';
PRINT 'Final timestamp: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';
PRINT 'Changes simulated:';
PRINT '- Employee promotions, salary increases, and department transfers';
PRINT '- Product price changes, descriptions updates, and discontinuations';
PRINT '- Customer address changes and tier upgrades';
PRINT '- Account balance changes (deposits, payments, transfers)';
PRINT '- System configuration policy updates';
PRINT '- Employee status changes (departures)';
PRINT '';
PRINT 'All changes are now tracked in temporal history tables!';
PRINT 'Use temporal queries to see the complete audit trail.';

GO