-- Create demo users for Hotel Reservation System
-- This script creates users with the credentials mentioned in DEMO_INFO.txt

-- First, let's create the basic tables if they don't exist
CREATE TABLE IF NOT EXISTS AspNetUsers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName TEXT,
    NormalizedUserName TEXT,
    Email TEXT,
    NormalizedEmail TEXT,
    EmailConfirmed INTEGER DEFAULT 0,
    PasswordHash TEXT,
    SecurityStamp TEXT,
    ConcurrencyStamp TEXT,
    PhoneNumber TEXT,
    PhoneNumberConfirmed INTEGER DEFAULT 0,
    TwoFactorEnabled INTEGER DEFAULT 0,
    LockoutEnd TEXT,
    LockoutEnabled INTEGER DEFAULT 0,
    AccessFailedCount INTEGER DEFAULT 0,
    FirstName TEXT,
    LastName TEXT,
    Role INTEGER,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

-- Insert demo users with simple credentials
-- Note: These are hashed passwords for the credentials in DEMO_INFO.txt
-- admin / password123
INSERT OR REPLACE INTO AspNetUsers (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    FirstName, LastName, Role, IsActive
) VALUES (
    1, 'admin', 'ADMIN', 'admin@demo.com', 'ADMIN@DEMO.COM',
    1, 'AQAAAAEAACcQAAAAEJ4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q==',
    'DEMO-SECURITY-STAMP', 'demo-concurrency-stamp',
    'Admin', 'User', 0, 1
);

-- manager1 / password123  
INSERT OR REPLACE INTO AspNetUsers (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    FirstName, LastName, Role, IsActive
) VALUES (
    2, 'manager1', 'MANAGER1', 'manager1@demo.com', 'MANAGER1@DEMO.COM',
    1, 'AQAAAAEAACcQAAAAEJ4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q==',
    'DEMO-SECURITY-STAMP', 'demo-concurrency-stamp',
    'Manager', 'User', 1, 1
);

-- demo / password123
INSERT OR REPLACE INTO AspNetUsers (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    FirstName, LastName, Role, IsActive
) VALUES (
    3, 'demo', 'DEMO', 'demo@demo.com', 'DEMO@DEMO.COM',
    1, 'AQAAAAEAACcQAAAAEJ4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q==',
    'DEMO-SECURITY-STAMP', 'demo-concurrency-stamp',
    'Demo', 'User', 2, 1
);