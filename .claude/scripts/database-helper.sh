#!/bin/bash

# Database Helper Script for GAAStat Project
# Provides database operations for JIRA workflow automation

set -euo pipefail

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-gaastat}"
DB_USER="${DB_USER:-postgres}"
BACKUP_DIR=".work/database-backups"
MIGRATION_DIR="database/migrations"
SCRIPTS_DIR=".claude/scripts"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    local level=$1
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')

    case $level in
        "INFO")
            echo -e "${BLUE}[INFO]${NC} ${timestamp} - $message"
            ;;
        "WARN")
            echo -e "${YELLOW}[WARN]${NC} ${timestamp} - $message"
            ;;
        "ERROR")
            echo -e "${RED}[ERROR]${NC} ${timestamp} - $message"
            ;;
        "SUCCESS")
            echo -e "${GREEN}[SUCCESS]${NC} ${timestamp} - $message"
            ;;
    esac
}

# Create backup directory if it doesn't exist
ensure_backup_dir() {
    if [ ! -d "$BACKUP_DIR" ]; then
        mkdir -p "$BACKUP_DIR"
        log "INFO" "Created backup directory: $BACKUP_DIR"
    fi
}

# Database connection test
test_connection() {
    log "INFO" "Testing database connection to $DB_HOST:$DB_PORT/$DB_NAME"

    if PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" > /dev/null 2>&1; then
        log "SUCCESS" "Database connection successful"
        return 0
    else
        log "ERROR" "Database connection failed"
        return 1
    fi
}

# Create full database backup
backup_database() {
    local backup_name="${1:-$(date '+%Y%m%d_%H%M%S')_full_backup}"
    local backup_file="$BACKUP_DIR/${backup_name}.sql"

    ensure_backup_dir

    log "INFO" "Creating database backup: $backup_file"

    if PGPASSWORD="$PGPASSWORD" pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --clean --if-exists --create --verbose > "$backup_file" 2>/dev/null; then

        # Compress the backup
        gzip "$backup_file"
        local compressed_file="${backup_file}.gz"
        local file_size=$(du -h "$compressed_file" | cut -f1)

        log "SUCCESS" "Database backup created: $compressed_file ($file_size)"
        echo "$compressed_file"
    else
        log "ERROR" "Database backup failed"
        return 1
    fi
}

# Create schema-only backup
backup_schema() {
    local backup_name="${1:-$(date '+%Y%m%d_%H%M%S')_schema_backup}"
    local backup_file="$BACKUP_DIR/${backup_name}.sql"

    ensure_backup_dir

    log "INFO" "Creating schema backup: $backup_file"

    if PGPASSWORD="$PGPASSWORD" pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --schema-only --clean --if-exists --create --verbose > "$backup_file" 2>/dev/null; then

        log "SUCCESS" "Schema backup created: $backup_file"
        echo "$backup_file"
    else
        log "ERROR" "Schema backup failed"
        return 1
    fi
}

# Restore database from backup
restore_database() {
    local backup_file="$1"

    if [ ! -f "$backup_file" ]; then
        log "ERROR" "Backup file not found: $backup_file"
        return 1
    fi

    log "INFO" "Restoring database from: $backup_file"
    log "WARN" "This will overwrite the current database. Continue? (y/N)"
    read -r confirmation

    if [[ ! "$confirmation" =~ ^[Yy]$ ]]; then
        log "INFO" "Restore cancelled"
        return 0
    fi

    # Check if file is compressed
    if [[ "$backup_file" == *.gz ]]; then
        if gunzip -c "$backup_file" | PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres 2>/dev/null; then
            log "SUCCESS" "Database restored from compressed backup"
        else
            log "ERROR" "Database restore failed"
            return 1
        fi
    else
        if PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -f "$backup_file" 2>/dev/null; then
            log "SUCCESS" "Database restored from backup"
        else
            log "ERROR" "Database restore failed"
            return 1
        fi
    fi
}

# Run database migrations
run_migrations() {
    local migration_dir="${1:-$MIGRATION_DIR}"

    if [ ! -d "$migration_dir" ]; then
        log "ERROR" "Migration directory not found: $migration_dir"
        return 1
    fi

    log "INFO" "Running database migrations from: $migration_dir"

    # Check if we're using Flyway or custom migrations
    if command -v flyway &> /dev/null; then
        log "INFO" "Using Flyway for migrations"
        flyway -url="jdbc:postgresql://$DB_HOST:$DB_PORT/$DB_NAME" \
               -user="$DB_USER" \
               -password="$PGPASSWORD" \
               -locations="filesystem:$migration_dir" \
               migrate
    else
        log "INFO" "Running custom migration scripts"

        # Run SQL files in alphabetical order
        for migration_file in "$migration_dir"/*.sql; do
            if [ -f "$migration_file" ]; then
                log "INFO" "Applying migration: $(basename "$migration_file")"

                if PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$migration_file"; then
                    log "SUCCESS" "Migration applied: $(basename "$migration_file")"
                else
                    log "ERROR" "Migration failed: $(basename "$migration_file")"
                    return 1
                fi
            fi
        done
    fi

    log "SUCCESS" "All migrations completed successfully"
}

# Generate Entity Framework models
scaffold_models() {
    local output_dir="${1:-backend/src/GAAStat.Dal/Models}"
    local namespace="${2:-GAAStat.Dal.Models}"

    log "INFO" "Scaffolding EF Core models to: $output_dir"

    # Check if we're in the correct directory structure
    if [ ! -f "backend/GAAStat.sln" ]; then
        log "ERROR" "Not in the correct project directory. Please run from project root."
        return 1
    fi

    cd backend

    # Scaffold using dotnet ef
    if dotnet ef dbcontext scaffold \
        "Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$PGPASSWORD" \
        Npgsql.EntityFrameworkCore.PostgreSQL \
        --output-dir "../$output_dir" \
        --namespace "$namespace" \
        --context "GAAStatDbContext" \
        --context-dir "../$output_dir" \
        --force \
        --verbose; then

        log "SUCCESS" "EF Core models scaffolded successfully"
    else
        log "ERROR" "EF Core scaffolding failed"
        return 1
    fi

    cd ..
}

# Database health check
health_check() {
    log "INFO" "Performing database health check"

    # Test connection
    if ! test_connection; then
        return 1
    fi

    # Check database size
    local db_size=$(PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c "SELECT pg_size_pretty(pg_database_size('$DB_NAME'));")
    log "INFO" "Database size: $db_size"

    # Check connection count
    local connections=$(PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c "SELECT count(*) FROM pg_stat_activity WHERE datname = '$DB_NAME';")
    log "INFO" "Active connections: $connections"

    # Check for long-running queries
    local long_queries=$(PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c "SELECT count(*) FROM pg_stat_activity WHERE state = 'active' AND query_start < NOW() - INTERVAL '1 minute';")

    if [ "$long_queries" -gt 0 ]; then
        log "WARN" "Found $long_queries long-running queries (>1 minute)"
    else
        log "INFO" "No long-running queries detected"
    fi

    # Check table sizes
    log "INFO" "Top 5 largest tables:"
    PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -c "SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
            FROM pg_tables
            WHERE schemaname NOT IN ('information_schema', 'pg_catalog')
            ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
            LIMIT 5;"

    log "SUCCESS" "Database health check completed"
}

# Reset database to clean state
reset_database() {
    log "WARN" "This will completely reset the database. All data will be lost!"
    log "WARN" "Continue? (y/N)"
    read -r confirmation

    if [[ ! "$confirmation" =~ ^[Yy]$ ]]; then
        log "INFO" "Database reset cancelled"
        return 0
    fi

    log "INFO" "Creating backup before reset"
    local backup_file=$(backup_database "pre_reset_$(date '+%Y%m%d_%H%M%S')")

    if [ $? -eq 0 ]; then
        log "SUCCESS" "Backup created: $backup_file"
    else
        log "ERROR" "Backup failed. Aborting reset."
        return 1
    fi

    log "INFO" "Dropping and recreating database"

    # Drop database
    if PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
        -c "DROP DATABASE IF EXISTS $DB_NAME;"; then
        log "INFO" "Database dropped"
    else
        log "ERROR" "Failed to drop database"
        return 1
    fi

    # Create database
    if PGPASSWORD="$PGPASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
        -c "CREATE DATABASE $DB_NAME;"; then
        log "INFO" "Database created"
    else
        log "ERROR" "Failed to create database"
        return 1
    fi

    # Run migrations if they exist
    if [ -d "$MIGRATION_DIR" ] && [ "$(ls -A $MIGRATION_DIR)" ]; then
        log "INFO" "Running migrations on clean database"
        run_migrations
    fi

    log "SUCCESS" "Database reset completed"
}

# Show usage information
show_help() {
    cat << EOF
Database Helper Script for GAAStat Project

Usage: $0 <command> [arguments]

Commands:
    test-connection         Test database connection
    backup [name]           Create full database backup
    backup-schema [name]    Create schema-only backup
    restore <backup_file>   Restore database from backup
    migrate [dir]           Run database migrations
    scaffold [output_dir]   Generate EF Core models
    health-check           Perform database health check
    reset                  Reset database to clean state
    help                   Show this help message

Environment Variables:
    DB_HOST                Database host (default: localhost)
    DB_PORT                Database port (default: 5432)
    DB_NAME                Database name (default: gaastat)
    DB_USER                Database user (default: postgres)
    PGPASSWORD             Database password (required)

Examples:
    $0 test-connection
    $0 backup migration_backup_v1
    $0 migrate database/migrations
    $0 scaffold backend/src/GAAStat.Dal/Models
    $0 health-check

EOF
}

# Main command dispatcher
main() {
    if [ $# -eq 0 ]; then
        show_help
        return 1
    fi

    local command=$1
    shift

    case $command in
        "test-connection")
            test_connection
            ;;
        "backup")
            backup_database "$@"
            ;;
        "backup-schema")
            backup_schema "$@"
            ;;
        "restore")
            if [ $# -eq 0 ]; then
                log "ERROR" "Backup file path required"
                return 1
            fi
            restore_database "$1"
            ;;
        "migrate")
            run_migrations "$@"
            ;;
        "scaffold")
            scaffold_models "$@"
            ;;
        "health-check")
            health_check
            ;;
        "reset")
            reset_database
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            log "ERROR" "Unknown command: $command"
            show_help
            return 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"