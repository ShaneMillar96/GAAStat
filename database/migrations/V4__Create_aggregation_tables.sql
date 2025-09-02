-- GAA Statistics Database - Aggregation Tables Migration
-- Creates pre-calculated summary tables for performance optimization

-- Season Player Totals
CREATE TABLE season_player_totals (
    season_total_id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL,
    season_id INTEGER NOT NULL,
    games_played INTEGER DEFAULT 0,
    total_minutes INTEGER DEFAULT 0,
    avg_engagement_efficiency DECIMAL(5,4),
    avg_possession_success_rate DECIMAL(5,4),
    total_scores INTEGER DEFAULT 0,
    total_goals INTEGER DEFAULT 0,
    total_points INTEGER DEFAULT 0,
    avg_conversion_rate DECIMAL(5,4),
    total_tackles INTEGER DEFAULT 0,
    avg_tackle_success_rate DECIMAL(5,4),
    total_turnovers_won INTEGER DEFAULT 0,
    total_interceptions INTEGER DEFAULT 0,
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (season_id) REFERENCES seasons(season_id) ON DELETE CASCADE,
    CONSTRAINT chk_games_played CHECK (games_played >= 0),
    CONSTRAINT chk_total_minutes CHECK (total_minutes >= 0),
    CONSTRAINT chk_season_percentages CHECK (
        (avg_engagement_efficiency IS NULL OR (avg_engagement_efficiency >= 0 AND avg_engagement_efficiency <= 1)) AND
        (avg_possession_success_rate IS NULL OR (avg_possession_success_rate >= 0 AND avg_possession_success_rate <= 1)) AND
        (avg_conversion_rate IS NULL OR (avg_conversion_rate >= 0 AND avg_conversion_rate <= 1)) AND
        (avg_tackle_success_rate IS NULL OR (avg_tackle_success_rate >= 0 AND avg_tackle_success_rate <= 1))
    ),
    CONSTRAINT chk_season_totals CHECK (
        total_scores >= 0 AND total_goals >= 0 AND total_points >= 0 AND 
        total_tackles >= 0 AND total_turnovers_won >= 0 AND total_interceptions >= 0
    ),
    UNIQUE(player_id, season_id)
);
COMMENT ON TABLE season_player_totals IS 'Player season statistics and averages';
COMMENT ON COLUMN season_player_totals.games_played IS 'Number of games played in the season';
COMMENT ON COLUMN season_player_totals.total_minutes IS 'Total minutes played in the season';
COMMENT ON COLUMN season_player_totals.avg_engagement_efficiency IS 'Average engagement efficiency across all games';
COMMENT ON COLUMN season_player_totals.total_scores IS 'Total combined goals and points scored';

-- Position Averages
CREATE TABLE position_averages (
    position_avg_id SERIAL PRIMARY KEY,
    position_id INTEGER NOT NULL,
    season_id INTEGER NOT NULL,
    avg_engagement_efficiency DECIMAL(5,4),
    avg_possession_success_rate DECIMAL(5,4),
    avg_conversion_rate DECIMAL(5,4),
    avg_tackle_success_rate DECIMAL(5,4),
    avg_scores_per_game DECIMAL(6,2),
    avg_possessions_per_game DECIMAL(6,2),
    avg_tackles_per_game DECIMAL(6,2),
    FOREIGN KEY (position_id) REFERENCES positions(position_id) ON DELETE CASCADE,
    FOREIGN KEY (season_id) REFERENCES seasons(season_id) ON DELETE CASCADE,
    CONSTRAINT chk_position_percentages CHECK (
        (avg_engagement_efficiency IS NULL OR (avg_engagement_efficiency >= 0 AND avg_engagement_efficiency <= 1)) AND
        (avg_possession_success_rate IS NULL OR (avg_possession_success_rate >= 0 AND avg_possession_success_rate <= 1)) AND
        (avg_conversion_rate IS NULL OR (avg_conversion_rate >= 0 AND avg_conversion_rate <= 1)) AND
        (avg_tackle_success_rate IS NULL OR (avg_tackle_success_rate >= 0 AND avg_tackle_success_rate <= 1))
    ),
    CONSTRAINT chk_position_averages CHECK (
        avg_scores_per_game >= 0 AND avg_possessions_per_game >= 0 AND avg_tackles_per_game >= 0
    ),
    UNIQUE(position_id, season_id)
);
COMMENT ON TABLE position_averages IS 'Position-based benchmark comparisons';
COMMENT ON COLUMN position_averages.avg_scores_per_game IS 'Average scores per game for this position';
COMMENT ON COLUMN position_averages.avg_possessions_per_game IS 'Average possessions per game for this position';
COMMENT ON COLUMN position_averages.avg_tackles_per_game IS 'Average tackles per game for this position';