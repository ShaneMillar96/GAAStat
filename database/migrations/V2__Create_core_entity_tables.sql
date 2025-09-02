-- GAA Statistics Database - Core Entity Tables Migration
-- Creates main business entities: matches and players

-- Players
CREATE TABLE players (
    player_id SERIAL PRIMARY KEY,
    player_name VARCHAR(100) NOT NULL,
    jersey_number INTEGER,
    is_active BOOLEAN DEFAULT TRUE,
    position_id INTEGER,
    FOREIGN KEY (position_id) REFERENCES positions(position_id),
    CONSTRAINT chk_jersey_number CHECK (jersey_number BETWEEN 1 AND 99)
);
COMMENT ON TABLE players IS 'Player master data and position information';
COMMENT ON COLUMN players.player_name IS 'Full name of the player';
COMMENT ON COLUMN players.jersey_number IS 'Jersey number (1-99)';
COMMENT ON COLUMN players.is_active IS 'Whether the player is currently active';

-- Matches
CREATE TABLE matches (
    match_id SERIAL PRIMARY KEY,
    match_number INTEGER,
    date DATE NOT NULL,
    drum_score VARCHAR(20),
    opposition_score VARCHAR(20),
    drum_goals INTEGER DEFAULT 0,
    drum_points INTEGER DEFAULT 0,
    opposition_goals INTEGER DEFAULT 0,
    opposition_points INTEGER DEFAULT 0,
    point_difference INTEGER,
    competition_id INTEGER,
    season_id INTEGER,
    venue_id INTEGER,
    match_result_id INTEGER,
    opposition_id INTEGER,
    FOREIGN KEY (competition_id) REFERENCES competitions(competition_id),
    FOREIGN KEY (season_id) REFERENCES seasons(season_id),
    FOREIGN KEY (venue_id) REFERENCES venues(venue_id),
    FOREIGN KEY (match_result_id) REFERENCES match_results(match_result_id),
    FOREIGN KEY (opposition_id) REFERENCES teams(team_id),
    CONSTRAINT chk_scores_non_negative CHECK (
        drum_goals >= 0 AND drum_points >= 0 AND 
        opposition_goals >= 0 AND opposition_points >= 0
    ),
    CONSTRAINT chk_point_difference CHECK (
        point_difference = (drum_goals * 3 + drum_points) - (opposition_goals * 3 + opposition_points)
    )
);
COMMENT ON TABLE matches IS 'Match information, results, and basic statistics';
COMMENT ON COLUMN matches.match_number IS 'Sequential match number for the season';
COMMENT ON COLUMN matches.drum_score IS 'Formatted score string for home team (e.g., "2-12")';
COMMENT ON COLUMN matches.opposition_score IS 'Formatted score string for opposition (e.g., "1-08")';
COMMENT ON COLUMN matches.drum_goals IS 'Number of goals scored by home team';
COMMENT ON COLUMN matches.drum_points IS 'Number of points scored by home team';
COMMENT ON COLUMN matches.opposition_goals IS 'Number of goals scored by opposition';
COMMENT ON COLUMN matches.opposition_points IS 'Number of points scored by opposition';
COMMENT ON COLUMN matches.point_difference IS 'Total point difference (positive = home win)';