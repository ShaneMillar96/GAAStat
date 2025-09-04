-- Clean invalid player data that was incorrectly ingested from Excel templates
-- This migration removes "Team Average", "Player 18", "Player 19" and similar invalid entries

-- First, let's see what we have (for logging purposes)
-- The actual deletion will be done below

-- Delete player statistics for invalid players first (to maintain referential integrity)
DELETE FROM match_player_statistics 
WHERE player_id IN (
    SELECT player_id 
    FROM players 
    WHERE player_name IN ('Team Average', 'Player 18', 'Player 19')
       OR player_name LIKE 'Player %'
       OR player_name ILIKE '%average%'
);

-- Delete the invalid player records
DELETE FROM players 
WHERE player_name IN ('Team Average', 'Player 18', 'Player 19')
   OR player_name LIKE 'Player %'
   OR player_name ILIKE '%average%';

-- Add a comment for logging
COMMENT ON TABLE players IS 'Player records - invalid template entries removed in V7';