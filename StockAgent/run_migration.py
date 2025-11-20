"""
Script to run SQL migration for AI trading tables.
"""

import pymysql
import util
from log.custom_logger import log

def run_migration():
    """Run SQL migration to create AI trading tables."""
    try:
        # Read SQL file
        sql_file = "database/migrations/002_CreateAITradingTables.sql"
        with open(sql_file, 'r', encoding='utf-8') as f:
            sql_script = f.read()
        
        # Connect to database
        ssl_config = {'ssl': {'ca': None}} if util.DB_SSL_MODE == "Required" else None
        connection = pymysql.connect(
            host=util.DB_HOST,
            port=util.DB_PORT,
            user=util.DB_USER,
            password=util.DB_PASSWORD,
            database=util.DB_NAME,
            ssl=ssl_config,
            charset='utf8mb4',
            cursorclass=pymysql.cursors.DictCursor
        )
        
        log.logger.info("Connected to database. Running migration...")
        
        # Execute each CREATE TABLE statement separately
        with connection.cursor() as cursor:
            # Extract CREATE TABLE statements
            statements = []
            current_statement = []
            in_create = False
            
            for line in sql_script.split('\n'):
                line = line.strip()
                if not line or line.startswith('--'):
                    continue
                
                if 'CREATE TABLE' in line.upper():
                    in_create = True
                    current_statement = [line]
                elif in_create:
                    current_statement.append(line)
                    if line.endswith(';'):
                        statements.append(' '.join(current_statement))
                        current_statement = []
                        in_create = False
            
            # Execute each statement
            for i, statement in enumerate(statements, 1):
                try:
                    log.logger.info(f"Executing statement {i}/{len(statements)}...")
                    cursor.execute(statement)
                    log.logger.info(f"Statement {i} executed successfully")
                except Exception as e:
                    error_msg = str(e)
                    if "already exists" in error_msg.lower() or "Duplicate" in error_msg:
                        log.logger.warning(f"Statement {i}: Table already exists, skipping")
                    else:
                        log.logger.error(f"Error in statement {i}: {error_msg}")
                        log.logger.error(f"Statement: {statement[:200]}...")
                        raise
        
        connection.commit()
        log.logger.info("Migration completed successfully!")
        
        # Verify tables were created
        with connection.cursor() as cursor:
            cursor.execute("SHOW TABLES LIKE 'ai_%'")
            tables = cursor.fetchall()
            log.logger.info(f"Created tables: {[list(t.values())[0] for t in tables]}")
        
        connection.close()
        return True
        
    except Exception as e:
        log.logger.error(f"Migration failed: {e}")
        return False

if __name__ == "__main__":
    run_migration()

