-- V7__Add_unique_constraints_for_etl.sql
-- Adding unique constraints to prevent duplicate data during ETL processing

-- Add unique constraint on match_player_statistics to prevent duplicate player stats for the same match
-- This ensures data integrity during Excel import processing
ALTER TABLE match_player_statistics 
ADD CONSTRAINT uk_match_player_statistics_match_player 
UNIQUE (match_id, player_id);

-- Add unique constraint on matches to prevent duplicate match records
-- This prevents importing the same match multiple times
ALTER TABLE matches 
ADD CONSTRAINT uk_matches_date_opposition 
UNIQUE (date, opposition_id);

-- Add index to improve performance for ETL lookups on match_player_statistics
CREATE INDEX IF NOT EXISTS idx_match_player_statistics_match_player 
ON match_player_statistics (match_id, player_id);

-- Add index to improve performance for ETL lookups on matches
CREATE INDEX IF NOT EXISTS idx_matches_date_opposition 
ON matches (date, opposition_id);

-- Add comments for documentation
COMMENT ON CONSTRAINT uk_match_player_statistics_match_player ON match_player_statistics IS 
'Ensures each player can only have one statistics record per match during ETL processing';

COMMENT ON CONSTRAINT uk_matches_date_opposition ON matches IS 
'Prevents duplicate matches for the same date and opposition during ETL processing';