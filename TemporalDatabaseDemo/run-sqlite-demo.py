#!/usr/bin/env python3
"""
Temporal Database Demo - SQLite Version
Python script to run the complete temporal database demonstration using SQLite
No SQL Server installation required!
"""

import sqlite3
import os
import sys
import time
from pathlib import Path

class TemporalDemoRunner:
    def __init__(self, db_path="temporal_demo.db"):
        self.db_path = db_path
        self.demo_root = Path(__file__).parent
        self.setup_path = self.demo_root / "setup"
        self.queries_path = self.demo_root / "queries"
        
        # Define script files in execution order
        self.setup_scripts = [
            "sqlite-setup.sql",
            "sqlite-sample-data.sql", 
            "sqlite-simulate-changes.sql"
        ]
        
        self.demo_scripts = [
            "sqlite-temporal-queries.sql"
        ]
    
    def print_header(self):
        """Print demo header"""
        print("╔══════════════════════════════════════════════════════════════════════════════╗")
        print("║                    TEMPORAL DATABASE DEMO - SQLITE VERSION                  ║")
        print("║                                                                              ║")
        print("║  This demo showcases temporal database concepts using SQLite with triggers   ║")
        print("║  for automatic history tracking. No SQL Server installation required!       ║")
        print("╚══════════════════════════════════════════════════════════════════════════════╝")
        print()
    
    def execute_sql_file(self, file_path, description):
        """Execute SQL file and display results"""
        print(f"🔄 {description}...")
        
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                sql_content = file.read()
            
            # Connect to database
            conn = sqlite3.connect(self.db_path)
            conn.row_factory = sqlite3.Row  # Enable column access by name
            cursor = conn.cursor()
            
            # Split SQL content by statements and execute
            statements = [stmt.strip() for stmt in sql_content.split(';') if stmt.strip()]
            
            for statement in statements:
                if statement.upper().startswith('SELECT'):
                    # Execute SELECT statements and display results
                    cursor.execute(statement)
                    results = cursor.fetchall()
                    
                    if results:
                        # Check if it's a message/status select
                        if len(results) == 1 and len(results[0]) == 1:
                            # Single value result - likely a message
                            print(f"   {results[0][0]}")
                        else:
                            # Tabular results
                            if len(results) <= 20:  # Only show small result sets
                                headers = [description[0] for description in cursor.description]
                                
                                # Print headers
                                header_line = " | ".join(f"{header:15}" for header in headers)
                                print(f"   {header_line}")
                                print(f"   {'-' * len(header_line)}")
                                
                                # Print rows
                                for row in results:
                                    row_line = " | ".join(f"{str(value):15}" for value in row)
                                    print(f"   {row_line}")
                                print()
                            else:
                                print(f"   Query returned {len(results)} rows")
                else:
                    # Execute non-SELECT statements
                    cursor.execute(statement)
            
            conn.commit()
            conn.close()
            
            print(f"✅ {description} completed successfully")
            
        except Exception as e:
            print(f"❌ {description} failed: {str(e)}")
            raise
    
    def show_menu(self):
        """Show interactive menu"""
        print("\n📋 Demo Options:")
        print("1. Run complete setup and demo")
        print("2. Run setup only")
        print("3. Run demo queries only")
        print("4. Reset database (clean start)")
        print("5. Exit")
        print()
        
        while True:
            try:
                choice = input("Select an option (1-5): ").strip()
                if choice in ['1', '2', '3', '4', '5']:
                    return choice
                else:
                    print("Please enter a number between 1 and 5.")
            except KeyboardInterrupt:
                print("\nExiting...")
                sys.exit(0)
    
    def wait_for_user(self, message="Press Enter to continue..."):
        """Wait for user input"""
        try:
            input(f"\n{message}")
        except KeyboardInterrupt:
            print("\nExiting...")
            sys.exit(0)
    
    def run_setup(self, interactive=False):
        """Run database setup"""
        print("\n🚀 SETUP PHASE: Creating temporal database and sample data")
        print("=========================================================")
        
        for script in self.setup_scripts:
            script_path = self.setup_path / script
            
            description = {
                "sqlite-setup.sql": "Creating SQLite temporal tables with history tracking",
                "sqlite-sample-data.sql": "Inserting sample data",
                "sqlite-simulate-changes.sql": "Simulating data changes over time"
            }.get(script, f"Executing {script}")
            
            self.execute_sql_file(script_path, description)
            
            if interactive and script == "sqlite-sample-data.sql":
                self.wait_for_user("Setup phase 1 complete. Ready to simulate changes?")
        
        print("\n✅ Setup phase completed successfully!")
        print("   • SQLite database created with temporal-like functionality")
        print("   • 4 main tables with automatic history tracking via triggers")
        print("   • Sample data inserted and changes simulated")
        
        if interactive:
            self.wait_for_user("Setup complete! Ready to run the demo?")
    
    def run_demo(self, interactive=False):
        """Run demo queries"""
        print("\n🎭 DEMO PHASE: Exploring temporal database capabilities")
        print("=====================================================")
        
        for script in self.demo_scripts:
            script_path = self.queries_path / script
            
            description = {
                "sqlite-temporal-queries.sql": "Running temporal query demonstrations"
            }.get(script, f"Executing {script}")
            
            self.execute_sql_file(script_path, description)
            
            if interactive:
                self.wait_for_user("Demo section complete. Continue?")
        
        print("\n✅ Demo phase completed successfully!")
    
    def reset_database(self):
        """Reset/delete the database"""
        if os.path.exists(self.db_path):
            os.remove(self.db_path)
            print(f"✅ Database {self.db_path} has been reset")
        else:
            print(f"ℹ️  Database {self.db_path} doesn't exist")
    
    def run(self, interactive=True):
        """Main execution method"""
        self.print_header()
        
        try:
            if interactive:
                choice = self.show_menu()
                
                if choice == "1":
                    # Complete setup and demo
                    self.run_setup(interactive=True)
                    self.run_demo(interactive=True)
                elif choice == "2":
                    # Setup only
                    self.run_setup(interactive=True)
                elif choice == "3":
                    # Demo only
                    if not os.path.exists(self.db_path):
                        print("⚠️  Database doesn't exist. Running setup first...")
                        self.run_setup(interactive=False)
                    self.run_demo(interactive=True)
                elif choice == "4":
                    # Reset database
                    self.reset_database()
                    return
                elif choice == "5":
                    # Exit
                    print("Goodbye! 👋")
                    return
            else:
                # Non-interactive mode - run everything
                self.run_setup(interactive=False)
                self.run_demo(interactive=False)
            
            # Final summary
            print("\n🎉 TEMPORAL DATABASE DEMO COMPLETED!")
            print("====================================")
            print("\n📊 What was demonstrated:")
            print("   • Temporal-like functionality using SQLite triggers")
            print("   • Complete audit trails and change tracking")
            print("   • Historical data analysis and reporting")
            print("   • Business intelligence with temporal context")
            print("   • Compliance and security auditing capabilities")
            print("\n🔗 Next steps:")
            print("   • Explore the SQLite database file: temporal_demo.db")
            print("   • Open with any SQLite browser/tool")
            print("   • Review the SQL scripts for learning")
            print("   • Try your own temporal queries")
            print(f"\n📁 Database file: {os.path.abspath(self.db_path)}")
            print(f"📁 Scripts location: {self.demo_root}")
            
        except Exception as e:
            print(f"\n❌ Demo execution failed: {str(e)}")
            print("\n🔧 Troubleshooting tips:")
            print("   • Ensure Python 3.6+ is installed")
            print("   • Check file permissions in the demo directory")
            print("   • Verify SQL script files exist")
            
            return 1
        
        print("\nThank you for exploring temporal databases! 🚀")
        return 0

def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Temporal Database Demo - SQLite Version")
    parser.add_argument("--db", default="temporal_demo.db", help="SQLite database file path")
    parser.add_argument("--non-interactive", action="store_true", help="Run in non-interactive mode")
    parser.add_argument("--reset", action="store_true", help="Reset database and exit")
    
    args = parser.parse_args()
    
    demo = TemporalDemoRunner(args.db)
    
    if args.reset:
        demo.reset_database()
        return 0
    
    return demo.run(interactive=not args.non_interactive)

if __name__ == "__main__":
    sys.exit(main())