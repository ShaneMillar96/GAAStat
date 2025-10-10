-- Migration: V1.1 - Add unique constraint to kpi_definitions table
-- Purpose: Enable idempotent upsert operations for KPI ETL (GAAS-7)
-- Created: 2025-10-10
-- JIRA: GAAS-7

-- Add unique constraint on natural key
-- Natural key: (event_number, event_name, outcome, team_assignment)
CREATE UNIQUE INDEX idx_kpi_definitions_unique_key
    ON kpi_definitions(event_number, event_name, outcome, team_assignment);

-- Add helpful comment for future reference
COMMENT ON INDEX idx_kpi_definitions_unique_key IS
    'Unique constraint for KPI definitions natural key, enables ON CONFLICT upsert in ETL pipeline';

-- Verification: Ensure constraint was created successfully
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename = 'kpi_definitions'
          AND indexname = 'idx_kpi_definitions_unique_key'
    ) THEN
        RAISE EXCEPTION 'Failed to create unique index idx_kpi_definitions_unique_key';
    END IF;

    RAISE NOTICE 'Migration V1.1 completed successfully';
END $$;
