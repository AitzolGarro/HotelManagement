// Interactive Temporal Database API Demo
// This script provides a simple way to interact with the temporal database

const { execSync } = require('child_process');
const readline = require('readline');

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
});

const dbPath = 'temporal_demo.db';
const sqlitePath = './sqlite3.exe';

// Check if database exists
const fs = require('fs');
if (!fs.existsSync(dbPath)) {
    console.log('❌ Database not found. Please run the temporal database demo first.');
    process.exit(1);
}

if (!fs.existsSync(sqlitePath)) {
    console.log('❌ SQLite not found. Please run the temporal database demo first.');
    process.exit(1);
}

function executeQuery(query) {
    try {
        const result = execSync(`"${sqlitePath}" "${dbPath}" "${query}"`, { encoding: 'utf8' });
        return result.trim();
    } catch (error) {
        return `Error: ${error.message}`;
    }
}

function showMenu() {
    console.clear();
    console.log('╔══════════════════════════════════════════════════════════════════════════════╗');
    console.log('║                    INTERACTIVE TEMPORAL DATABASE API                        ║');
    console.log('╚══════════════════════════════════════════════════════════════════════════════╝');
    console.log('');
    console.log('📋 Choose a demo scenario:');
    console.log('');
    console.log('1. 👥 Current Employees');
    console.log('2. 📈 John Smith History');
    console.log('3. 💰 Product Prices');
    console.log('4. 🏆 Customer Tiers');
    console.log('5. ⚙️  System Config');
    console.log('6. 📊 Database Stats');
    console.log('7. 🔍 Custom Query');
    console.log('0. Exit');
    console.log('');
}

function runQuery(query, title) {
    console.log(`\n🔄 ${title}`);
    console.log('='.repeat(title.length));
    console.log('');
    
    const result = executeQuery(query);
    if (result) {
        console.log(result);
    } else {
        console.log('No results found.');
    }
    
    console.log('\nPress Enter to continue...');
    return new Promise(resolve => {
        rl.once('line', resolve);
    });
}

async function main() {
    while (true) {
        showMenu();
        
        const choice = await new Promise(resolve => {
            rl.question('Enter your choice (0-7): ', resolve);
        });
        
        switch (choice) {
            case '1':
                await runQuery(
                    "SELECT EmployeeId, FirstName || ' ' || LastName AS Name, Department, Position, '$' || Salary AS Salary FROM Employees WHERE IsActive = 1 ORDER BY Department;",
                    'Current Active Employees'
                );
                break;
                
            case '2':
                await runQuery(
                    "SELECT FirstName || ' ' || LastName AS Name, Department, Position, '$' || Salary AS Salary, ValidFrom, ValidTo FROM (SELECT * FROM Employees WHERE FirstName = 'John' AND LastName = 'Smith' UNION ALL SELECT EmployeeId, FirstName, LastName, Email, Department, Position, Salary, HireDate, IsActive, ValidFrom, ValidTo FROM EmployeesHistory WHERE FirstName = 'John' AND LastName = 'Smith') ORDER BY ValidFrom;",
                    'John Smith Career History'
                );
                break;
                
            case '3':
                await runQuery(
                    "SELECT ProductName, Category, '$' || Price AS Price, StockQuantity, CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Discontinued' END AS Status FROM Products ORDER BY Category;",
                    'Current Product Catalog'
                );
                break;
                
            case '4':
                await runQuery(
                    "SELECT FirstName || ' ' || LastName AS Name, CustomerTier, City || ', ' || State AS Location FROM Customers ORDER BY CustomerTier;",
                    'Customer Tiers'
                );
                break;
                
            case '5':
                await runQuery(
                    "SELECT ConfigKey, ConfigValue, Description FROM SystemConfig WHERE IsActive = 1 ORDER BY Category;",
                    'System Configuration'
                );
                break;
                
            case '6':
                await runQuery(
                    "SELECT 'Current Records' AS Metric, (SELECT COUNT(*) FROM Employees) + (SELECT COUNT(*) FROM Products) + (SELECT COUNT(*) FROM Customers) + (SELECT COUNT(*) FROM SystemConfig) AS Value UNION ALL SELECT 'Historical Records', (SELECT COUNT(*) FROM EmployeesHistory) + (SELECT COUNT(*) FROM ProductsHistory) + (SELECT COUNT(*) FROM CustomersHistory) + (SELECT COUNT(*) FROM SystemConfigHistory);",
                    'Database Statistics'
                );
                break;
                
            case '7':
                console.log('\nAvailable tables:');
                console.log('  • Employees, Products, Customers, SystemConfig');
                console.log('  • EmployeesHistory, ProductsHistory, CustomersHistory, SystemConfigHistory');
                console.log('');
                
                const customQuery = await new Promise(resolve => {
                    rl.question('Enter your SQL query: ', resolve);
                });
                
                if (customQuery.trim()) {
                    await runQuery(customQuery, 'Custom Query Results');
                }
                break;
                
            case '0':
                console.log('\nThank you for exploring temporal databases! 🚀');
                rl.close();
                return;
                
            default:
                console.log('Invalid choice. Please enter 0-7.');
                await new Promise(resolve => setTimeout(resolve, 2000));
        }
    }
}

// Start the interactive demo
main().catch(console.error);