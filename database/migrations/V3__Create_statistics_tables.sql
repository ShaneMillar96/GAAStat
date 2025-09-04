-- GAA Statistics Database - Statistics Tables Migration
-- Creates detailed analytics and performance tracking tables

-- Match Team Statistics
CREATE TABLE match_team_statistics (
    match_team_stat_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    drum_first_half DECIMAL(10,4),
    drum_second_half DECIMAL(10,4),
    drum_full_game DECIMAL(10,4),
    opposition_first_half DECIMAL(10,4),
    opposition_second_half DECIMAL(10,4),
    opposition_full_game DECIMAL(10,4),
    metric_definition_id INTEGER NOT NULL,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (metric_definition_id) REFERENCES metric_definitions(metric_id),
    CONSTRAINT chk_period_totals CHECK (
        ABS((COALESCE(drum_first_half, 0) + COALESCE(drum_second_half, 0)) - COALESCE(drum_full_game, 0)) < 0.01
    )
);
COMMENT ON TABLE match_team_statistics IS 'Detailed team-level performance metrics (235+ data points per match)';
COMMENT ON COLUMN match_team_statistics.drum_first_half IS 'Home team metric value for first half';
COMMENT ON COLUMN match_team_statistics.drum_second_half IS 'Home team metric value for second half';
COMMENT ON COLUMN match_team_statistics.drum_full_game IS 'Home team metric value for full game';

-- Match Player Statistics
CREATE TABLE match_player_statistics (
    match_player_stat_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    player_id INTEGER NOT NULL,
    minutes_played INTEGER DEFAULT 0,
    total_engagements INTEGER DEFAULT 0,
    engagement_efficiency DECIMAL(5,4),
    scores VARCHAR(20),
    possession_success_rate DECIMAL(5,4),
    possessions_per_te DECIMAL(10,4),
    total_possessions INTEGER DEFAULT 0,
    turnovers_won INTEGER DEFAULT 0,
    interceptions INTEGER DEFAULT 0,
    total_attacks INTEGER DEFAULT 0,
    kick_retained INTEGER DEFAULT 0,
    kick_lost INTEGER DEFAULT 0,
    carry_retained INTEGER DEFAULT 0,
    carry_lost INTEGER DEFAULT 0,
    shots_total INTEGER DEFAULT 0,
    goals INTEGER DEFAULT 0,
    points INTEGER DEFAULT 0,
    wides INTEGER DEFAULT 0,
    conversion_rate DECIMAL(5,4),
    tackles_total INTEGER DEFAULT 0,
    tackles_contact INTEGER DEFAULT 0,
    tackles_missed INTEGER DEFAULT 0,
    tackle_percentage DECIMAL(5,4),
    frees_conceded_total INTEGER DEFAULT 0,
    yellow_cards INTEGER DEFAULT 0,
    black_cards INTEGER DEFAULT 0,
    red_cards INTEGER DEFAULT 0,
    kickouts_total INTEGER DEFAULT 0,
    kickouts_retained INTEGER DEFAULT 0,
    kickouts_lost INTEGER DEFAULT 0,
    kickout_percentage DECIMAL(5,4),
    saves INTEGER DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id),
    CONSTRAINT chk_minutes_played CHECK (minutes_played >= 0 AND minutes_played <= 120),
    CONSTRAINT chk_percentage_rates CHECK (
        (engagement_efficiency IS NULL OR (engagement_efficiency >= 0 AND engagement_efficiency <= 2.5)) AND
        (possession_success_rate IS NULL OR (possession_success_rate >= 0 AND possession_success_rate <= 1)) AND
        (conversion_rate IS NULL OR (conversion_rate >= 0 AND conversion_rate <= 1)) AND
        (tackle_percentage IS NULL OR (tackle_percentage >= 0 AND tackle_percentage <= 1)) AND
        (kickout_percentage IS NULL OR (kickout_percentage >= 0 AND kickout_percentage <= 1))
    ),
    CONSTRAINT chk_kick_totals CHECK (kick_retained + kick_lost >= 0),
    CONSTRAINT chk_carry_totals CHECK (carry_retained + carry_lost >= 0),
    CONSTRAINT chk_tackle_totals CHECK (tackles_contact + tackles_missed <= tackles_total),
    CONSTRAINT chk_kickout_totals CHECK (kickouts_retained + kickouts_lost <= kickouts_total),
    CONSTRAINT chk_cards_non_negative CHECK (
        yellow_cards >= 0 AND black_cards >= 0 AND red_cards >= 0
    ),
    CONSTRAINT chk_scores_non_negative CHECK (
        goals >= 0 AND points >= 0 AND wides >= 0 AND shots_total >= 0
    )
);
COMMENT ON TABLE match_player_statistics IS 'Individual player performance data (80+ fields per player per match)';
COMMENT ON COLUMN match_player_statistics.engagement_efficiency IS 'Player engagement efficiency rate (0-2.5)';
COMMENT ON COLUMN match_player_statistics.possession_success_rate IS 'Success rate for possessions (0-1)';
COMMENT ON COLUMN match_player_statistics.possessions_per_te IS 'Possessions per total engagement';

-- Kickout Analysis
CREATE TABLE kickout_analysis (
    kickout_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    total_attempts INTEGER DEFAULT 0,
    successful INTEGER DEFAULT 0,
    success_rate DECIMAL(5,4),
    outcome_breakdown JSONB,
    time_period_id INTEGER,
    kickout_type_id INTEGER,
    team_type_id INTEGER,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (time_period_id) REFERENCES time_periods(time_period_id),
    FOREIGN KEY (kickout_type_id) REFERENCES kickout_types(kickout_type_id),
    FOREIGN KEY (team_type_id) REFERENCES team_types(team_type_id),
    CONSTRAINT chk_kickout_success_rate CHECK (success_rate IS NULL OR (success_rate >= 0 AND success_rate <= 1)),
    CONSTRAINT chk_kickout_attempts CHECK (successful <= total_attempts)
);
COMMENT ON TABLE kickout_analysis IS 'Detailed kickout performance tracking by type and period';
COMMENT ON COLUMN kickout_analysis.outcome_breakdown IS 'JSONB object containing detailed outcome statistics';

-- Shot Analysis
CREATE TABLE shot_analysis (
    shot_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    player_id INTEGER,
    shot_number INTEGER,
    time_period VARCHAR(20),
    shot_type_id INTEGER,
    shot_outcome_id INTEGER,
    position_area_id INTEGER,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id),
    FOREIGN KEY (shot_type_id) REFERENCES shot_types(shot_type_id),
    FOREIGN KEY (shot_outcome_id) REFERENCES shot_outcomes(shot_outcome_id),
    FOREIGN KEY (position_area_id) REFERENCES position_areas(position_area_id),
    CONSTRAINT chk_shot_number CHECK (shot_number > 0)
);
COMMENT ON TABLE shot_analysis IS 'Individual shot tracking with outcome and location data';
COMMENT ON COLUMN shot_analysis.shot_number IS 'Sequential shot number within the match';
COMMENT ON COLUMN shot_analysis.time_period IS 'Time period when shot was taken';

-- Scoreable Free Analysis
CREATE TABLE scoreable_free_analysis (
    scoreable_free_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    player_id INTEGER,
    free_number INTEGER,
    distance VARCHAR(20),
    success BOOLEAN,
    free_type_id INTEGER,
    shot_outcome_id INTEGER,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id),
    FOREIGN KEY (free_type_id) REFERENCES free_types(free_type_id),
    FOREIGN KEY (shot_outcome_id) REFERENCES shot_outcomes(shot_outcome_id),
    CONSTRAINT chk_free_number CHECK (free_number > 0)
);
COMMENT ON TABLE scoreable_free_analysis IS 'Free kick performance with distance and success tracking';
COMMENT ON COLUMN scoreable_free_analysis.distance IS 'Distance description of the free kick';
COMMENT ON COLUMN scoreable_free_analysis.success IS 'Whether the free kick was successful';

-- Positional Analysis
CREATE TABLE positional_analysis (
    positional_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    position_id INTEGER NOT NULL,
    avg_engagement_efficiency DECIMAL(5,4),
    avg_possession_success_rate DECIMAL(5,4),
    avg_conversion_rate DECIMAL(5,4),
    avg_tackle_success_rate DECIMAL(5,4),
    total_scores INTEGER DEFAULT 0,
    total_possessions INTEGER DEFAULT 0,
    total_tackles INTEGER DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (position_id) REFERENCES positions(position_id),
    CONSTRAINT chk_positional_percentages CHECK (
        (avg_engagement_efficiency IS NULL OR (avg_engagement_efficiency >= 0 AND avg_engagement_efficiency <= 2.5)) AND
        (avg_possession_success_rate IS NULL OR (avg_possession_success_rate >= 0 AND avg_possession_success_rate <= 1)) AND
        (avg_conversion_rate IS NULL OR (avg_conversion_rate >= 0 AND avg_conversion_rate <= 1)) AND
        (avg_tackle_success_rate IS NULL OR (avg_tackle_success_rate >= 0 AND avg_tackle_success_rate <= 1))
    ),
    CONSTRAINT chk_positional_totals CHECK (
        total_scores >= 0 AND total_possessions >= 0 AND total_tackles >= 0
    )
);
COMMENT ON TABLE positional_analysis IS 'Position-based aggregated statistics per match';