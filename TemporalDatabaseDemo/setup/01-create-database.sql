-- Temporal Database Demo - Database Setup
-- This script creates the temporal database and enables necessary features

USE master;
GO

-- Create the temporal demo database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TemporalDemo')
BEGIN
    CREATE DATABASE TemporalDemo;
    PRINT 'TemporalDemo database created successfully.';
END
ELSE
BEGIN
    PRINT 'TemporalDemo database already exists.';
END
GO

USE TemporalDemo;
GO

-- Enable temporal table features (already enabled by default in SQL Server 2016+)
-- But let's verify the database is ready for temporal tables
SELECT 
    name,
    is_temporal_history_retention_enabled,
    temporal_history_retention_days
FROM sys.databases 
WHERE name = 'TemporalDemo';

PRINT 'Database setup completed. Ready for temporal tables!';
GO