-- Rollback Migration: V1.1
-- Purpose: Remove unique constraint from kpi_definitions table
-- Created: 2025-10-10
-- JIRA: GAAS-7

-- Drop the unique index
DROP INDEX IF EXISTS idx_kpi_definitions_unique_key;

-- Verification: Ensure constraint was removed
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename = 'kpi_definitions'
          AND indexname = 'idx_kpi_definitions_unique_key'
    ) THEN
        RAISE EXCEPTION 'Failed to drop unique index idx_kpi_definitions_unique_key';
    END IF;

    RAISE NOTICE 'Rollback V1.1 completed successfully';
END $$;
