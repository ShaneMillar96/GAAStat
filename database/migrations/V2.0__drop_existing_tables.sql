-- V2.0__drop_existing_tables.sql
-- Purpose: Safely drop all existing tables to prepare for comprehensive GAA schema rebuild
-- Author: Database Engineer
-- Date: 2025-08-29
-- Risk: HIGH - This will drop all existing data. Ensure backups are taken before migration.

-- Drop triggers first to avoid dependency issues
DROP TRIGGER IF EXISTS update_player_stats_updated_at ON player_stats;
DROP TRIGGER IF EXISTS update_matches_updated_at ON matches;
DROP TRIGGER IF EXISTS update_players_updated_at ON players;
DROP TRIGGER IF EXISTS update_teams_updated_at ON teams;
DROP TRIGGER IF EXISTS update_users_updated_at ON users;

-- Drop the trigger function
DROP FUNCTION IF EXISTS update_updated_at_column();

-- Drop indexes explicitly (they will be dropped with tables but for clarity)
DROP INDEX IF EXISTS idx_teams_sport;
DROP INDEX IF EXISTS idx_player_stats_match_player;
DROP INDEX IF EXISTS idx_matches_teams;
DROP INDEX IF EXISTS idx_matches_date;
DROP INDEX IF EXISTS idx_players_team_id;

-- Drop tables in reverse dependency order
-- Start with tables that have foreign key references
DROP TABLE IF EXISTS player_stats CASCADE;
DROP TABLE IF EXISTS matches CASCADE;
DROP TABLE IF EXISTS players CASCADE;
DROP TABLE IF EXISTS teams CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Comment for clarity on what was accomplished
COMMENT ON SCHEMA public IS 'Schema cleared for GAA Statistics comprehensive rebuild - V2.0 completed';