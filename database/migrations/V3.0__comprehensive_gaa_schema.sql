-- V3.0__comprehensive_gaa_schema.sql
-- Purpose: Create comprehensive GAA statistics database schema supporting 85+ player metrics,
--          clear-and-reload import strategy, and advanced analytics capabilities
-- Author: Database Engineer  
-- Date: 2025-08-29
-- Features: Multi-season support, PSR calculations, kickout analysis, rollback capability

-- =============================================
-- REFERENCE DATA (Never cleared during import)
-- =============================================

-- Teams table with enhanced metadata
CREATE TABLE teams (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    abbreviation VARCHAR(10),
    county VARCHAR(50),
    division VARCHAR(50),
    color_primary VARCHAR(7), -- Hex color codes
    color_secondary VARCHAR(7),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Seasons for multi-year data management
CREATE TABLE seasons (
    id SERIAL PRIMARY KEY,
    year INTEGER NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    start_date DATE,
    end_date DATE,
    is_current BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Competitions within seasons
CREATE TABLE competitions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'League', 'Championship', 'Cup'
    season_id INTEGER NOT NULL REFERENCES seasons(id),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Event types from KPI definitions (16 types total)
CREATE TABLE event_types (
    id SERIAL PRIMARY KEY,
    code VARCHAR(10) NOT NULL UNIQUE, -- '1.0', '2.0', '3.1', '3.2', etc.
    name VARCHAR(100) NOT NULL, -- 'Kickout', 'Attacks', 'Short Kickout', etc.
    category VARCHAR(50), -- 'Possession', 'Defensive', 'Attacking'
    description TEXT,
    default_psr_value INTEGER DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Possible outcomes for each event type with PSR values (-3 to +3 range)
CREATE TABLE event_outcomes (
    id SERIAL PRIMARY KEY,
    event_type_id INTEGER NOT NULL REFERENCES event_types(id),
    outcome VARCHAR(100) NOT NULL, -- 'Won clean', 'Lost clean', 'Won on break', etc.
    psr_value INTEGER NOT NULL CHECK (psr_value >= -3 AND psr_value <= 3),
    assign_to VARCHAR(20) NOT NULL CHECK (assign_to IN ('Home', 'Opposition')),
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(event_type_id, outcome)
);

-- =============================================
-- MATCH DATA (Cleared and reloaded on import)
-- =============================================

-- Core matches table with comprehensive match details
CREATE TABLE matches (
    id SERIAL PRIMARY KEY,
    competition_id INTEGER NOT NULL REFERENCES competitions(id),
    match_number INTEGER, -- Sequential match number from Excel source
    match_date DATE NOT NULL,
    home_team_id INTEGER NOT NULL REFERENCES teams(id),
    away_team_id INTEGER NOT NULL REFERENCES teams(id),
    venue VARCHAR(200),
    home_score_goals INTEGER DEFAULT 0,
    home_score_points INTEGER DEFAULT 0,
    away_score_goals INTEGER DEFAULT 0,
    away_score_points INTEGER DEFAULT 0,
    weather_conditions VARCHAR(100),
    attendance INTEGER,
    notes TEXT,
    excel_sheet_name VARCHAR(255), -- Track which Excel sheet this came from
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_match_teams_date UNIQUE(match_date, home_team_id, away_team_id)
);

-- Comprehensive player statistics table (85+ columns as specified)
CREATE TABLE match_player_stats (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    player_name VARCHAR(100) NOT NULL, -- Store name exactly as-is from Excel
    jersey_number INTEGER,
    team_id INTEGER NOT NULL REFERENCES teams(id),
    minutes_played INTEGER DEFAULT 0,
    
    -- Core Performance Metrics
    total_events INTEGER DEFAULT 0,
    performance_success_rate DECIMAL(8,4), -- PSR calculation result
    total_possessions INTEGER DEFAULT 0,
    turnovers_won INTEGER DEFAULT 0,
    interceptions INTEGER DEFAULT 0,
    possessions_lost INTEGER DEFAULT 0,
    
    -- Passing Statistics
    kick_passes INTEGER DEFAULT 0,
    hand_passes INTEGER DEFAULT 0,
    kick_pass_success_rate DECIMAL(8,4),
    hand_pass_success_rate DECIMAL(8,4),
    
    -- Defensive Actions
    tackles_made INTEGER DEFAULT 0,
    tackles_missed INTEGER DEFAULT 0,
    tackle_success_rate DECIMAL(8,4),
    
    -- Disciplinary Records
    frees_won INTEGER DEFAULT 0,
    frees_conceded INTEGER DEFAULT 0,
    cards_yellow INTEGER DEFAULT 0,
    cards_black INTEGER DEFAULT 0,
    cards_red INTEGER DEFAULT 0,
    
    -- Scoring from Play
    points_from_play INTEGER DEFAULT 0,
    goals_from_play INTEGER DEFAULT 0,
    two_pointers_from_play INTEGER DEFAULT 0, -- Special GAA scoring type
    shots_wide INTEGER DEFAULT 0,
    shots_saved INTEGER DEFAULT 0,
    shots_short INTEGER DEFAULT 0,
    shots_blocked INTEGER DEFAULT 0,
    shots_woodwork INTEGER DEFAULT 0,
    
    -- Scoring from Frees
    points_from_frees INTEGER DEFAULT 0,
    goals_from_frees INTEGER DEFAULT 0,
    frees_wide INTEGER DEFAULT 0,
    frees_saved INTEGER DEFAULT 0,
    frees_short INTEGER DEFAULT 0,
    
    -- Score Assists
    score_assists_points INTEGER DEFAULT 0,
    score_assists_goals INTEGER DEFAULT 0,
    
    -- Advanced Performance Metrics
    shot_efficiency DECIMAL(8,4), -- Percentage of shots resulting in scores
    score_conversion_rate DECIMAL(8,4), -- Success rate for scoring opportunities
    
    -- Additional Statistical Fields (extending to 85+ columns)
    defensive_actions INTEGER DEFAULT 0,
    attacking_plays INTEGER DEFAULT 0,
    possession_won_percentage DECIMAL(8,4),
    distribution_accuracy DECIMAL(8,4),
    ground_ball_wins INTEGER DEFAULT 0,
    aerial_contests_won INTEGER DEFAULT 0,
    clean_catches INTEGER DEFAULT 0,
    fumbles INTEGER DEFAULT 0,
    
    -- Performance Ratings
    overall_performance_rating DECIMAL(8,4),
    attacking_rating DECIMAL(8,4),
    defensive_rating DECIMAL(8,4),
    passing_rating DECIMAL(8,4),
    
    -- Match Context
    starting_position VARCHAR(50),
    substituted_on_minute INTEGER,
    substituted_off_minute INTEGER,
    captain BOOLEAN DEFAULT FALSE,
    
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_match_player UNIQUE(match_id, player_name, jersey_number)
);

-- Specialized Kickout Statistics (critical for GAA analysis)
CREATE TABLE match_kickout_stats (
    id SERIAL PRIMARY KEY,
    match_player_stat_id INTEGER NOT NULL REFERENCES match_player_stats(id) ON DELETE CASCADE,
    kickouts_taken INTEGER DEFAULT 0,
    kickouts_won_clean INTEGER DEFAULT 0,
    kickouts_lost_clean INTEGER DEFAULT 0,
    kickouts_won_break INTEGER DEFAULT 0, -- Won on the break/contest
    kickouts_lost_break INTEGER DEFAULT 0,
    kickouts_won_short INTEGER DEFAULT 0, -- Short kickout success
    kickouts_lost_short INTEGER DEFAULT 0,
    kickouts_to_right INTEGER DEFAULT 0, -- Direction analysis
    kickouts_to_left INTEGER DEFAULT 0,
    kickouts_down_middle INTEGER DEFAULT 0,
    kickout_retention_rate DECIMAL(8,4), -- Overall retention percentage
    saves INTEGER DEFAULT 0, -- Goalkeeper saves (if applicable)
    save_percentage DECIMAL(8,4),
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Attack and Shot Source Analysis for tactical insights
CREATE TABLE match_source_analysis (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    team_id INTEGER NOT NULL REFERENCES teams(id),
    analysis_type VARCHAR(50) NOT NULL, -- 'attack_source', 'shot_source', 'score_source'
    source_category VARCHAR(100) NOT NULL, -- 'Kickout Long', 'Turnover', 'Free Kick', etc.
    total_count INTEGER DEFAULT 0,
    successful_count INTEGER DEFAULT 0,
    success_rate DECIMAL(8,4),
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_match_team_analysis UNIQUE(match_id, team_id, analysis_type, source_category)
);

-- =============================================
-- IMPORT MANAGEMENT (For rollback capability)
-- =============================================

-- Track all import operations for audit and rollback
CREATE TABLE import_history (
    id SERIAL PRIMARY KEY,
    import_type VARCHAR(50) NOT NULL DEFAULT 'excel_full_reload',
    file_name VARCHAR(500),
    file_size BIGINT,
    matches_imported INTEGER DEFAULT 0,
    players_processed INTEGER DEFAULT 0,
    events_created INTEGER DEFAULT 0,
    import_started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    import_completed_at TIMESTAMP,
    import_status VARCHAR(20) NOT NULL DEFAULT 'in_progress', 
        CHECK (import_status IN ('in_progress', 'completed', 'failed', 'rolled_back')),
    error_message TEXT,
    snapshot_id INTEGER, -- Reference to pre-import snapshot
    processing_duration_seconds INTEGER
);

-- Store compressed snapshots for rollback capability
CREATE TABLE import_snapshots (
    id SERIAL PRIMARY KEY,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    matches_data JSONB, -- Compressed snapshot of matches table data
    player_stats_data JSONB, -- Compressed snapshot of critical statistical data
    kickout_stats_data JSONB, -- Compressed kickout statistics
    source_analysis_data JSONB, -- Compressed source analysis data
    total_matches INTEGER DEFAULT 0,
    total_player_records INTEGER DEFAULT 0,
    snapshot_size_mb DECIMAL(10,2),
    compression_ratio DECIMAL(8,4)
);

-- =============================================
-- PERFORMANCE INDEXES FOR ANALYTICS
-- =============================================

-- Primary performance indexes for frequent queries
CREATE INDEX idx_matches_date ON matches(match_date);
CREATE INDEX idx_matches_competition ON matches(competition_id);
CREATE INDEX idx_matches_teams ON matches(home_team_id, away_team_id);
CREATE INDEX idx_matches_season ON matches(competition_id, match_date);

-- Player statistics indexes for analytics
CREATE INDEX idx_player_stats_match ON match_player_stats(match_id);
CREATE INDEX idx_player_stats_team ON match_player_stats(team_id);
CREATE INDEX idx_player_stats_player_name ON match_player_stats(player_name);
CREATE INDEX idx_player_stats_performance ON match_player_stats(performance_success_rate DESC);
CREATE INDEX idx_player_stats_scoring ON match_player_stats(points_from_play DESC, goals_from_play DESC);

-- Composite indexes for complex analytics queries
CREATE INDEX idx_player_stats_match_team ON match_player_stats(match_id, team_id);
CREATE INDEX idx_player_stats_season_analysis ON match_player_stats(team_id, match_id, performance_success_rate);

-- Kickout analysis indexes
CREATE INDEX idx_kickout_stats_player ON match_kickout_stats(match_player_stat_id);
CREATE INDEX idx_kickout_retention ON match_kickout_stats(kickout_retention_rate DESC);

-- Source analysis indexes for tactical queries
CREATE INDEX idx_source_analysis_match_team ON match_source_analysis(match_id, team_id);
CREATE INDEX idx_source_analysis_type ON match_source_analysis(analysis_type, source_category);

-- Import management indexes
CREATE INDEX idx_import_history_status ON import_history(import_status, import_started_at);
CREATE INDEX idx_import_snapshots_created ON import_snapshots(created_at DESC);

-- Reference data indexes
CREATE INDEX idx_teams_name ON teams(name);
CREATE INDEX idx_seasons_current ON seasons(is_current, year DESC);
CREATE INDEX idx_competitions_season ON competitions(season_id, type);
CREATE INDEX idx_event_types_code ON event_types(code);
CREATE INDEX idx_event_outcomes_psr ON event_outcomes(event_type_id, psr_value);

-- =============================================
-- DATABASE CONSTRAINTS AND BUSINESS RULES
-- =============================================

-- Ensure valid PSR ranges
ALTER TABLE match_player_stats 
ADD CONSTRAINT check_psr_range 
CHECK (performance_success_rate IS NULL OR (performance_success_rate >= -3.0 AND performance_success_rate <= 3.0));

-- Ensure valid percentage ranges
ALTER TABLE match_player_stats
ADD CONSTRAINT check_percentage_ranges
CHECK (
    (shot_efficiency IS NULL OR (shot_efficiency >= 0 AND shot_efficiency <= 1)) AND
    (score_conversion_rate IS NULL OR (score_conversion_rate >= 0 AND score_conversion_rate <= 1)) AND
    (kick_pass_success_rate IS NULL OR (kick_pass_success_rate >= 0 AND kick_pass_success_rate <= 1)) AND
    (hand_pass_success_rate IS NULL OR (hand_pass_success_rate >= 0 AND hand_pass_success_rate <= 1)) AND
    (tackle_success_rate IS NULL OR (tackle_success_rate >= 0 AND tackle_success_rate <= 1))
);

-- Ensure valid kickout percentages
ALTER TABLE match_kickout_stats
ADD CONSTRAINT check_kickout_percentages
CHECK (
    (kickout_retention_rate IS NULL OR (kickout_retention_rate >= 0 AND kickout_retention_rate <= 1)) AND
    (save_percentage IS NULL OR (save_percentage >= 0 AND save_percentage <= 1))
);

-- Ensure teams cannot play themselves
ALTER TABLE matches
ADD CONSTRAINT check_different_teams
CHECK (home_team_id != away_team_id);

-- Ensure only one current season
CREATE UNIQUE INDEX unique_current_season ON seasons(is_current) WHERE is_current = TRUE;

-- =============================================
-- HELPER FUNCTIONS FOR DATA INTEGRITY
-- =============================================

-- Function to update timestamps automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply timestamp triggers to reference tables
CREATE TRIGGER update_teams_updated_at 
    BEFORE UPDATE ON teams 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- TABLE COMMENTS FOR DOCUMENTATION
-- =============================================

-- Reference data tables
COMMENT ON TABLE teams IS 'GAA teams participating in matches - reference data not cleared on import';
COMMENT ON TABLE seasons IS 'Season definitions for multi-year data management';
COMMENT ON TABLE competitions IS 'Competitions within seasons (League, Championship, Cup formats)';
COMMENT ON TABLE event_types IS 'KPI event type definitions with default PSR values';
COMMENT ON TABLE event_outcomes IS 'Possible outcomes for events with assigned PSR values (-3 to +3)';

-- Match data tables
COMMENT ON TABLE matches IS 'Match records cleared and reloaded on each Excel import';
COMMENT ON TABLE match_player_stats IS 'Comprehensive player statistics (85+ columns) cleared on import';
COMMENT ON TABLE match_kickout_stats IS 'Specialized kickout analysis data for goalkeepers and field players';
COMMENT ON TABLE match_source_analysis IS 'Attack/shot source analysis for tactical insights';

-- Import management tables
COMMENT ON TABLE import_history IS 'Audit trail of all Excel import operations';
COMMENT ON TABLE import_snapshots IS 'Compressed data snapshots for rollback capability';

-- Key column comments
COMMENT ON COLUMN match_player_stats.performance_success_rate IS 'PSR value calculation result (-3.0 to +3.0 range)';
COMMENT ON COLUMN match_player_stats.player_name IS 'Player name stored exactly as appears in Excel import';
COMMENT ON COLUMN matches.excel_sheet_name IS 'Source Excel sheet name for import traceability';
COMMENT ON COLUMN event_outcomes.psr_value IS 'Performance Success Rate value assigned to this outcome';

-- Schema completion marker
COMMENT ON SCHEMA public IS 'GAA Statistics comprehensive schema - V3.0 implementation completed';