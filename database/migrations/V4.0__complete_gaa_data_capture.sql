-- V4.0__complete_gaa_data_capture.sql
-- Purpose: Add missing tables and columns to capture all GAA statistics from Excel
-- Author: Claude Database Engineer
-- Date: 2025-09-01
-- Fixes: Missing 6 matches, incomplete player stats (85 columns), team stats, event data

-- =============================================
-- TEAM-LEVEL MATCH STATISTICS
-- =============================================

-- Team performance statistics per match
CREATE TABLE match_team_stats (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    team_id INTEGER NOT NULL REFERENCES teams(id),
    
    -- Possession and Control Statistics
    possession_percentage DECIMAL(5,2),
    total_possessions INTEGER DEFAULT 0,
    possessions_won INTEGER DEFAULT 0,
    possessions_lost INTEGER DEFAULT 0,
    
    -- Attack Statistics  
    attacks_total INTEGER DEFAULT 0,
    attacks_from_kickout_long INTEGER DEFAULT 0,
    attacks_from_kickout_short INTEGER DEFAULT 0,
    attacks_from_opp_kickout_long INTEGER DEFAULT 0,
    attacks_from_opp_kickout_short INTEGER DEFAULT 0,
    attacks_from_turnover INTEGER DEFAULT 0,
    attacks_from_possession_lost INTEGER DEFAULT 0,
    attacks_from_shot_short INTEGER DEFAULT 0,
    attack_efficiency DECIMAL(5,2), -- attacks to scores ratio
    
    -- Scoring Statistics
    scores_from_play INTEGER DEFAULT 0,
    scores_from_frees INTEGER DEFAULT 0,
    total_shots INTEGER DEFAULT 0,
    shots_on_target INTEGER DEFAULT 0,
    shots_wide INTEGER DEFAULT 0,
    shots_saved INTEGER DEFAULT 0,
    shots_short INTEGER DEFAULT 0,
    shots_blocked INTEGER DEFAULT 0,
    shot_conversion_rate DECIMAL(5,2),
    
    -- Kickout Statistics
    own_kickouts_total INTEGER DEFAULT 0,
    own_kickouts_won INTEGER DEFAULT 0,
    own_kickouts_won_percentage DECIMAL(5,2),
    opp_kickouts_contested INTEGER DEFAULT 0,
    opp_kickouts_won INTEGER DEFAULT 0,
    opp_kickouts_won_percentage DECIMAL(5,2),
    kickout_retention_rate DECIMAL(5,2),
    
    -- Free Statistics
    frees_conceded_total INTEGER DEFAULT 0,
    frees_conceded_attacking INTEGER DEFAULT 0,
    frees_conceded_midfield INTEGER DEFAULT 0,
    frees_conceded_defensive INTEGER DEFAULT 0,
    frees_50m_conceded INTEGER DEFAULT 0,
    
    -- Advanced Analytics
    avg_attacks_per_score DECIMAL(5,2),
    retained_attacks_to_shots DECIMAL(5,2),
    
    -- Metadata
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_match_team_stats UNIQUE(match_id, team_id)
);

-- =============================================
-- DETAILED MATCH EVENTS
-- =============================================

-- Capture event-by-event data from Excel analysis sheets
CREATE TABLE match_events (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL, -- 'kickout', 'shot_from_play', 'scoreable_free', 'turnover'
    event_time TIME, -- Time within match when event occurred
    period INTEGER, -- 1st half = 1, 2nd half = 2
    team_id INTEGER NOT NULL REFERENCES teams(id),
    player_name VARCHAR(100),
    jersey_number INTEGER,
    
    -- Event Outcome Details
    outcome VARCHAR(100), -- 'Won Clean', 'Lost Clean', 'Point', 'Goal', 'Wide', etc.
    outcome_location VARCHAR(50), -- 'Left', 'Right', 'Center', 'Short', 'Long'
    outcome_value INTEGER, -- Points scored from this event (0, 1, 3)
    
    -- Contextual Information
    field_position VARCHAR(50), -- 'Attacking Third', 'Midfield', 'Defensive Third'
    event_sequence INTEGER, -- Sequential number within match
    created_by VARCHAR(100), -- Player who created the opportunity
    assisted_by VARCHAR(100), -- Player who provided assist
    
    -- Additional Event Data (JSON for flexibility)
    event_details JSONB, -- Store any additional metrics specific to event type
    
    -- Performance Impact
    psr_impact INTEGER, -- PSR value assigned to this event (-3 to +3)
    team_possession_impact BOOLEAN, -- Did this event retain/lose possession
    
    -- Import Metadata
    source_sheet VARCHAR(100), -- Which Excel sheet this came from
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Constraints
    CONSTRAINT check_psr_impact_range CHECK (psr_impact >= -3 AND psr_impact <= 3),
    CONSTRAINT check_period_valid CHECK (period IN (1, 2))
);

-- =============================================
-- SEASON CUMULATIVE STATISTICS
-- =============================================

-- Season-long aggregated statistics from "Cumulative Stats 2025" sheet
CREATE TABLE cumulative_season_stats (
    id SERIAL PRIMARY KEY,
    season_id INTEGER NOT NULL REFERENCES seasons(id),
    player_name VARCHAR(100) NOT NULL,
    jersey_number INTEGER,
    position VARCHAR(50),
    team_id INTEGER NOT NULL REFERENCES teams(id),
    
    -- Match Participation
    matches_played INTEGER DEFAULT 0,
    minutes_played_total INTEGER DEFAULT 0,
    matches_started INTEGER DEFAULT 0,
    
    -- Core Performance (Season Totals)
    total_events_season INTEGER DEFAULT 0,
    average_psr DECIMAL(8,4),
    total_possessions_season INTEGER DEFAULT 0,
    possession_success_rate DECIMAL(8,4),
    
    -- Scoring Summary (Season)
    scores_string VARCHAR(50), -- e.g., "2-15" format
    total_points_season INTEGER DEFAULT 0,
    total_goals_season INTEGER DEFAULT 0,
    total_two_pointers_season INTEGER DEFAULT 0,
    points_per_game DECIMAL(5,2),
    goals_per_game DECIMAL(5,2),
    
    -- Shooting Summary (Season)
    shots_total_season INTEGER DEFAULT 0,
    shots_on_target_season INTEGER DEFAULT 0,
    shot_accuracy_percentage DECIMAL(5,2),
    shots_from_play_season INTEGER DEFAULT 0,
    shots_from_frees_season INTEGER DEFAULT 0,
    
    -- Passing Summary (Season) 
    kick_passes_season INTEGER DEFAULT 0,
    hand_passes_season INTEGER DEFAULT 0,
    pass_completion_rate DECIMAL(5,2),
    
    -- Defensive Summary (Season)
    tackles_made_season INTEGER DEFAULT 0,
    tackles_missed_season INTEGER DEFAULT 0,
    tackle_success_rate_season DECIMAL(5,2),
    interceptions_season INTEGER DEFAULT 0,
    turnovers_won_season INTEGER DEFAULT 0,
    
    -- Discipline Summary (Season)
    frees_won_season INTEGER DEFAULT 0,
    frees_conceded_season INTEGER DEFAULT 0,
    yellow_cards_season INTEGER DEFAULT 0,
    black_cards_season INTEGER DEFAULT 0,
    red_cards_season INTEGER DEFAULT 0,
    
    -- Kickout Performance (Season) - For Goalkeepers
    kickouts_taken_season INTEGER DEFAULT 0,
    kickout_success_rate_season DECIMAL(5,2),
    saves_made_season INTEGER DEFAULT 0,
    save_percentage_season DECIMAL(5,2),
    
    -- Advanced Season Metrics
    player_efficiency_rating DECIMAL(8,4), -- Overall performance index
    consistency_rating DECIMAL(8,4), -- Performance consistency across matches
    impact_rating DECIMAL(8,4), -- Match-winning contribution index
    
    -- Update Tracking
    last_match_included INTEGER REFERENCES matches(id),
    stats_updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT unique_player_season UNIQUE(season_id, player_name, team_id)
);

-- =============================================
-- ENHANCED PLAYER STATISTICS COLUMNS
-- =============================================

-- Add missing columns to match_player_stats to capture all 85 Excel columns
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS scores_string VARCHAR(50); -- e.g., "0-03(2f)" format
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS psr_per_total_possessions DECIMAL(8,4);

-- Enhanced Passing Statistics
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS handling_errors INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS pass_turnovers INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS passes_intercepted INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS support_scores INTEGER DEFAULT 0;

-- Detailed Kickout Statistics (Own Team)
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS own_kickouts_contested INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS own_kickouts_won_clean INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS own_kickouts_won_break INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS own_kickouts_won_short INTEGER DEFAULT 0;

-- Opposition Kickout Statistics  
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS opp_kickouts_contested INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS opp_kickouts_won_clean INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS opp_kickouts_won_break INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS opp_kickouts_won_short INTEGER DEFAULT 0;

-- Attack Creation
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS attacks_created INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS attacks_from_kickout INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS attacks_lost INTEGER DEFAULT 0;

-- Detailed Shot Statistics
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS two_pointers_attempted INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS shots_blocked_against INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS shots_45s_earned INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS quick_shots INTEGER DEFAULT 0;

-- Free Kick Details
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_45s_earned INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_blocked INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS quick_frees INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS free_conversion_rate DECIMAL(8,4);

-- Assist Breakdowns
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS total_assists INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS point_assists INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS goal_assists INTEGER DEFAULT 0;

-- Tackle Details
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS total_tackle_attempts INTEGER DEFAULT 0;

-- Detailed Disciplinary
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_attacking_third INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_midfield INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_defensive_third INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_penalty_area INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS frees_50m_conceded INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS delays INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS dissent INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS three_vs_three_fouls INTEGER DEFAULT 0;

-- Throw-up/Ruck Statistics
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS throw_ups_won INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS throw_ups_lost INTEGER DEFAULT 0;

-- Enhanced Goalkeeper Statistics
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS total_kickouts_taken INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS kickouts_right INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS kickouts_left INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS kickouts_center INTEGER DEFAULT 0;
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS goalkeeper_saves_detail INTEGER DEFAULT 0;

-- Performance Percentages and Ratios
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS total_shot_percentage DECIMAL(8,4);
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS attacking_efficiency DECIMAL(8,4);
ALTER TABLE match_player_stats ADD COLUMN IF NOT EXISTS defensive_efficiency DECIMAL(8,4);

-- =============================================
-- POSITION-SPECIFIC ANALYSIS TABLES
-- =============================================

-- Position-specific performance metrics
CREATE TABLE position_performance_stats (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    position_group VARCHAR(20) NOT NULL, -- 'Goalkeeper', 'Defender', 'Midfielder', 'Forward'
    player_name VARCHAR(100) NOT NULL,
    team_id INTEGER NOT NULL REFERENCES teams(id),
    
    -- Position-Specific Metrics (varies by position)
    primary_responsibility VARCHAR(100), -- e.g., "Shot Stopping", "Build-up Play", "Score Creation"
    responsibility_success_rate DECIMAL(8,4),
    
    -- Key Performance Areas
    key_metric_1_name VARCHAR(50),
    key_metric_1_value DECIMAL(10,2),
    key_metric_2_name VARCHAR(50),
    key_metric_2_value DECIMAL(10,2),
    key_metric_3_name VARCHAR(50),
    key_metric_3_value DECIMAL(10,2),
    
    -- Position Ranking within Team
    position_rank INTEGER,
    position_rating DECIMAL(5,2), -- Out of 10
    
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_match_position_player UNIQUE(match_id, position_group, player_name)
);

-- =============================================
-- INDEXES FOR PERFORMANCE
-- =============================================

-- Team Statistics Indexes
CREATE INDEX idx_team_stats_match ON match_team_stats(match_id);
CREATE INDEX idx_team_stats_team ON match_team_stats(team_id);
CREATE INDEX idx_team_stats_possession ON match_team_stats(possession_percentage DESC);
CREATE INDEX idx_team_stats_efficiency ON match_team_stats(attack_efficiency DESC);

-- Match Events Indexes
CREATE INDEX idx_events_match ON match_events(match_id);
CREATE INDEX idx_events_type ON match_events(event_type);
CREATE INDEX idx_events_team ON match_events(team_id);
CREATE INDEX idx_events_player ON match_events(player_name);
CREATE INDEX idx_events_outcome ON match_events(outcome);
CREATE INDEX idx_events_time ON match_events(period, event_time);

-- Seasonal Statistics Indexes  
CREATE INDEX idx_seasonal_player ON cumulative_season_stats(player_name);
CREATE INDEX idx_seasonal_team ON cumulative_season_stats(team_id);
CREATE INDEX idx_seasonal_season ON cumulative_season_stats(season_id);
CREATE INDEX idx_seasonal_psr ON cumulative_season_stats(average_psr DESC);
CREATE INDEX idx_seasonal_efficiency ON cumulative_season_stats(player_efficiency_rating DESC);

-- Enhanced Player Statistics Indexes
CREATE INDEX idx_player_stats_scores ON match_player_stats(points_from_play DESC, goals_from_play DESC);
CREATE INDEX idx_player_stats_assists ON match_player_stats(total_assists DESC);
CREATE INDEX idx_player_stats_kickouts ON match_player_stats(total_kickouts_taken DESC);

-- Position Performance Indexes
CREATE INDEX idx_position_match ON position_performance_stats(match_id);
CREATE INDEX idx_position_group ON position_performance_stats(position_group);
CREATE INDEX idx_position_rating ON position_performance_stats(position_rating DESC);

-- =============================================
-- DATA VALIDATION CONSTRAINTS  
-- =============================================

-- Team Statistics Constraints
ALTER TABLE match_team_stats 
ADD CONSTRAINT check_team_percentages
CHECK (
    (possession_percentage IS NULL OR (possession_percentage >= 0 AND possession_percentage <= 100)) AND
    (attack_efficiency IS NULL OR attack_efficiency >= 0) AND
    (shot_conversion_rate IS NULL OR (shot_conversion_rate >= 0 AND shot_conversion_rate <= 100)) AND
    (kickout_retention_rate IS NULL OR (kickout_retention_rate >= 0 AND kickout_retention_rate <= 100))
);

-- Match Events Constraints
ALTER TABLE match_events
ADD CONSTRAINT check_event_types
CHECK (event_type IN ('kickout', 'shot_from_play', 'scoreable_free', 'turnover', 'possession', 'attack'));

ALTER TABLE match_events
ADD CONSTRAINT check_outcome_value
CHECK (outcome_value >= 0 AND outcome_value <= 3);

-- Enhanced Player Stats Constraints  
ALTER TABLE match_player_stats
ADD CONSTRAINT check_enhanced_percentages
CHECK (
    (total_shot_percentage IS NULL OR (total_shot_percentage >= 0 AND total_shot_percentage <= 1)) AND
    (free_conversion_rate IS NULL OR (free_conversion_rate >= 0 AND free_conversion_rate <= 1)) AND
    (attacking_efficiency IS NULL OR attacking_efficiency >= 0) AND
    (defensive_efficiency IS NULL OR defensive_efficiency >= 0)
);

-- Position Performance Constraints
ALTER TABLE position_performance_stats
ADD CONSTRAINT check_position_groups  
CHECK (position_group IN ('Goalkeeper', 'Defender', 'Midfielder', 'Forward'));

ALTER TABLE position_performance_stats
ADD CONSTRAINT check_position_rating
CHECK (position_rating >= 0 AND position_rating <= 10);

-- =============================================
-- HELPER FUNCTIONS FOR CALCULATIONS
-- =============================================

-- Function to calculate player efficiency rating
CREATE OR REPLACE FUNCTION calculate_player_efficiency(
    p_player_name VARCHAR,
    p_match_id INTEGER
) RETURNS DECIMAL(8,4) AS $$
DECLARE
    efficiency_score DECIMAL(8,4) := 0.0;
    psr_weight DECIMAL(2,1) := 0.3;
    scoring_weight DECIMAL(2,1) := 0.25;
    possession_weight DECIMAL(2,1) := 0.25;
    discipline_weight DECIMAL(2,1) := 0.2;
BEGIN
    -- Calculate weighted efficiency based on multiple factors
    SELECT 
        (COALESCE(performance_success_rate, 0) * psr_weight) +
        (COALESCE(points_from_play + (goals_from_play * 3), 0) * scoring_weight / 10.0) +
        (COALESCE(total_possessions, 0) * possession_weight / 20.0) +
        (GREATEST(0, 5 - COALESCE(frees_conceded + cards_yellow + (cards_black * 2), 0)) * discipline_weight)
    INTO efficiency_score
    FROM match_player_stats
    WHERE player_name = p_player_name AND match_id = p_match_id;
    
    RETURN COALESCE(efficiency_score, 0.0);
END;
$$ LANGUAGE plpgsql;

-- Function to update cumulative season stats
CREATE OR REPLACE FUNCTION update_cumulative_stats(
    p_season_id INTEGER,
    p_player_name VARCHAR,
    p_team_id INTEGER
) RETURNS VOID AS $$
BEGIN
    -- Insert or update cumulative statistics for a player
    INSERT INTO cumulative_season_stats (
        season_id, player_name, team_id,
        matches_played, minutes_played_total,
        total_events_season, average_psr,
        total_points_season, total_goals_season
    )
    SELECT 
        p_season_id,
        p_player_name,
        p_team_id,
        COUNT(*) as matches_played,
        SUM(COALESCE(minutes_played, 0)) as total_minutes,
        SUM(COALESCE(total_events, 0)) as total_events,
        AVG(COALESCE(performance_success_rate, 0)) as avg_psr,
        SUM(COALESCE(points_from_play, 0) + COALESCE(points_from_frees, 0)) as total_points,
        SUM(COALESCE(goals_from_play, 0) + COALESCE(goals_from_frees, 0)) as total_goals
    FROM match_player_stats mps
    JOIN matches m ON mps.match_id = m.id
    JOIN competitions c ON m.competition_id = c.id
    WHERE c.season_id = p_season_id 
    AND mps.player_name = p_player_name
    AND mps.team_id = p_team_id
    ON CONFLICT (season_id, player_name, team_id)
    DO UPDATE SET
        matches_played = EXCLUDED.matches_played,
        minutes_played_total = EXCLUDED.minutes_played_total,
        total_events_season = EXCLUDED.total_events_season,
        average_psr = EXCLUDED.average_psr,
        total_points_season = EXCLUDED.total_points_season,
        total_goals_season = EXCLUDED.total_goals_season,
        stats_updated_at = CURRENT_TIMESTAMP;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- TABLE COMMENTS FOR DOCUMENTATION
-- =============================================

COMMENT ON TABLE match_team_stats IS 'Team-level performance statistics per match extracted from Excel team sheets';
COMMENT ON TABLE match_events IS 'Detailed event-by-event data from Kickout/Shot/Free analysis Excel sheets';
COMMENT ON TABLE cumulative_season_stats IS 'Season-long aggregated player statistics from Cumulative Stats 2025 sheet';
COMMENT ON TABLE position_performance_stats IS 'Position-specific performance metrics for tactical analysis';

COMMENT ON COLUMN match_team_stats.attack_efficiency IS 'Ratio of attacks that result in scores or quality shots';
COMMENT ON COLUMN match_events.psr_impact IS 'Performance Success Rate impact of this event (-3 to +3)';
COMMENT ON COLUMN cumulative_season_stats.player_efficiency_rating IS 'Composite efficiency score across all performance areas';
COMMENT ON COLUMN match_player_stats.scores_string IS 'Score format as appears in Excel (e.g., "0-03(2f)")';

-- Schema completion marker
COMMENT ON SCHEMA public IS 'GAA Statistics enhanced schema - V4.0 complete data capture implementation';