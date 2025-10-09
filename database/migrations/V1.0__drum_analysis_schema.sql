-- =====================================================================
-- Flyway Migration: V2.0 - Drum Analysis 2025 Schema
-- =====================================================================
-- Purpose: Create comprehensive database schema for GAA statistics
--          tracking from Drum Analysis 2025 Excel data structure
--
-- Tables Created: 9 tables
--   1. seasons - Season management
--   2. positions - Player positions (GK, DEF, MID, FWD)
--   3. teams - Drum and opponent teams
--   4. competitions - Competition types within seasons
--   5. players - Player roster
--   6. matches - Match information and scores
--   7. match_team_statistics - Team stats by period
--   8. player_match_statistics - Player performance metrics (86+ fields)
--   9. kpi_definitions - Metric definitions
--
-- Related JIRA: GAAS-4
-- Created: 2025-10-09
-- =====================================================================

-- =====================================================================
-- TABLE 1: seasons
-- Purpose: Track different seasons of competition
-- =====================================================================
CREATE TABLE seasons (
    season_id SERIAL PRIMARY KEY,
    year INTEGER NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    start_date DATE,
    end_date DATE,
    is_current BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_seasons_year_range CHECK (year BETWEEN 2000 AND 2100),
    CONSTRAINT chk_seasons_date_order CHECK (start_date IS NULL OR end_date IS NULL OR start_date < end_date)
);

COMMENT ON TABLE seasons IS 'Tracks GAA seasons';
COMMENT ON COLUMN seasons.year IS 'Season year (e.g., 2025)';
COMMENT ON COLUMN seasons.is_current IS 'TRUE for currently active season';

CREATE INDEX idx_seasons_year ON seasons(year);
CREATE INDEX idx_seasons_current ON seasons(is_current) WHERE is_current = TRUE;

-- =====================================================================
-- TABLE 2: positions
-- Purpose: Define player positions (GK, DEF, MID, FWD)
-- =====================================================================
CREATE TABLE positions (
    position_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    code VARCHAR(10) NOT NULL UNIQUE,
    display_order INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE positions IS 'Player position definitions';
COMMENT ON COLUMN positions.code IS 'Short code: GK, DEF, MID, FWD';
COMMENT ON COLUMN positions.display_order IS 'Display order in UI';

CREATE INDEX idx_positions_code ON positions(code);

-- =====================================================================
-- TABLE 3: teams
-- Purpose: Store all teams (Drum and opponents)
-- =====================================================================
CREATE TABLE teams (
    team_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    abbreviation VARCHAR(10),
    is_drum BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE teams IS 'All teams: Drum and opponents';
COMMENT ON COLUMN teams.is_drum IS 'TRUE only for Drum team';
COMMENT ON COLUMN teams.is_active IS 'FALSE for inactive/historical teams';

CREATE INDEX idx_teams_name ON teams(name);
CREATE INDEX idx_teams_drum ON teams(is_drum) WHERE is_drum = TRUE;
CREATE INDEX idx_teams_active ON teams(is_active) WHERE is_active = TRUE;

-- =====================================================================
-- TABLE 4: competitions
-- Purpose: Track competition types within seasons
-- =====================================================================
CREATE TABLE competitions (
    competition_id SERIAL PRIMARY KEY,
    season_id INTEGER NOT NULL,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_competitions_season FOREIGN KEY (season_id)
        REFERENCES seasons(season_id) ON DELETE CASCADE,
    CONSTRAINT chk_competitions_type CHECK (type IN ('Championship', 'League', 'Cup', 'Friendly'))
);

COMMENT ON TABLE competitions IS 'Competition types within each season';
COMMENT ON COLUMN competitions.type IS 'Championship, League, Cup, or Friendly';

CREATE INDEX idx_competitions_season ON competitions(season_id);
CREATE INDEX idx_competitions_type ON competitions(type);
CREATE UNIQUE INDEX idx_competitions_season_name ON competitions(season_id, name);

-- =====================================================================
-- TABLE 5: players
-- Purpose: Store player roster information
-- =====================================================================
CREATE TABLE players (
    player_id SERIAL PRIMARY KEY,
    jersey_number INTEGER NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    full_name VARCHAR(200) NOT NULL,
    position_id INTEGER NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_players_position FOREIGN KEY (position_id)
        REFERENCES positions(position_id) ON DELETE RESTRICT,
    CONSTRAINT chk_players_jersey CHECK (jersey_number BETWEEN 1 AND 99)
);

COMMENT ON TABLE players IS 'Player roster';
COMMENT ON COLUMN players.jersey_number IS 'Jersey/squad number';
COMMENT ON COLUMN players.is_active IS 'FALSE for retired/inactive players';

CREATE INDEX idx_players_jersey ON players(jersey_number);
CREATE INDEX idx_players_position ON players(position_id);
CREATE INDEX idx_players_full_name ON players(full_name);
CREATE INDEX idx_players_active ON players(is_active) WHERE is_active = TRUE;

-- =====================================================================
-- TABLE 6: matches
-- Purpose: Store match information
-- =====================================================================
CREATE TABLE matches (
    match_id SERIAL PRIMARY KEY,
    competition_id INTEGER NOT NULL,
    match_number INTEGER NOT NULL,
    home_team_id INTEGER NOT NULL,
    away_team_id INTEGER NOT NULL,
    match_date DATE NOT NULL,
    venue VARCHAR(50) NOT NULL,
    home_score_first_half VARCHAR(10),
    home_score_second_half VARCHAR(10),
    home_score_full_time VARCHAR(10),
    away_score_first_half VARCHAR(10),
    away_score_second_half VARCHAR(10),
    away_score_full_time VARCHAR(10),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_matches_competition FOREIGN KEY (competition_id)
        REFERENCES competitions(competition_id) ON DELETE RESTRICT,
    CONSTRAINT fk_matches_home_team FOREIGN KEY (home_team_id)
        REFERENCES teams(team_id) ON DELETE RESTRICT,
    CONSTRAINT fk_matches_away_team FOREIGN KEY (away_team_id)
        REFERENCES teams(team_id) ON DELETE RESTRICT,
    CONSTRAINT chk_matches_venue CHECK (venue IN ('Home', 'Away', 'Neutral')),
    CONSTRAINT chk_matches_teams CHECK (home_team_id != away_team_id)
);

COMMENT ON TABLE matches IS 'Match information and scores';
COMMENT ON COLUMN matches.match_number IS 'Sequential match number in season';
COMMENT ON COLUMN matches.venue IS 'Home, Away, or Neutral';
COMMENT ON COLUMN matches.home_score_first_half IS 'GAA notation: G-PP (e.g., "0-04")';

CREATE INDEX idx_matches_competition ON matches(competition_id);
CREATE INDEX idx_matches_date ON matches(match_date);
CREATE INDEX idx_matches_home_team ON matches(home_team_id);
CREATE INDEX idx_matches_away_team ON matches(away_team_id);
CREATE UNIQUE INDEX idx_matches_competition_number ON matches(competition_id, match_number);

-- =====================================================================
-- TABLE 7: match_team_statistics
-- Purpose: Store team-level match statistics by period
-- =====================================================================
CREATE TABLE match_team_statistics (
    match_team_stat_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    period VARCHAR(10) NOT NULL,
    scoreline VARCHAR(10),
    total_possession NUMERIC(5, 4),
    score_source_kickout_long INTEGER DEFAULT 0,
    score_source_kickout_short INTEGER DEFAULT 0,
    score_source_opp_kickout_long INTEGER DEFAULT 0,
    score_source_opp_kickout_short INTEGER DEFAULT 0,
    score_source_turnover INTEGER DEFAULT 0,
    score_source_possession_lost INTEGER DEFAULT 0,
    score_source_shot_short INTEGER DEFAULT 0,
    score_source_throw_up_in INTEGER DEFAULT 0,
    shot_source_kickout_long INTEGER DEFAULT 0,
    shot_source_kickout_short INTEGER DEFAULT 0,
    shot_source_opp_kickout_long INTEGER DEFAULT 0,
    shot_source_opp_kickout_short INTEGER DEFAULT 0,
    shot_source_turnover INTEGER DEFAULT 0,
    shot_source_possession_lost INTEGER DEFAULT 0,
    shot_source_shot_short INTEGER DEFAULT 0,
    shot_source_throw_up_in INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_mts_match FOREIGN KEY (match_id)
        REFERENCES matches(match_id) ON DELETE CASCADE,
    CONSTRAINT fk_mts_team FOREIGN KEY (team_id)
        REFERENCES teams(team_id) ON DELETE RESTRICT,
    CONSTRAINT chk_mts_period CHECK (period IN ('1st', '2nd', 'Full')),
    CONSTRAINT chk_mts_possession CHECK (total_possession IS NULL OR (total_possession >= 0 AND total_possession <= 1))
);

COMMENT ON TABLE match_team_statistics IS 'Team statistics by period (6 records per match)';
COMMENT ON COLUMN match_team_statistics.period IS '1st, 2nd, or Full';
COMMENT ON COLUMN match_team_statistics.total_possession IS 'Possession percentage as decimal (0-1)';

CREATE INDEX idx_mts_match ON match_team_statistics(match_id);
CREATE INDEX idx_mts_team ON match_team_statistics(team_id);
CREATE INDEX idx_mts_period ON match_team_statistics(period);
CREATE UNIQUE INDEX idx_mts_match_team_period ON match_team_statistics(match_id, team_id, period);

-- =====================================================================
-- TABLE 8: player_match_statistics
-- Purpose: Store individual player performance metrics (86+ fields)
-- =====================================================================
CREATE TABLE player_match_statistics (
    player_match_stat_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    player_id INTEGER NOT NULL,

    -- Summary Statistics (8 fields)
    minutes_played INTEGER DEFAULT 0,
    total_engagements INTEGER DEFAULT 0,
    te_per_psr NUMERIC(5, 2),
    scores VARCHAR(20),
    psr INTEGER DEFAULT 0,
    psr_per_tp NUMERIC(5, 2),

    -- Possession Play (14 fields)
    tp INTEGER DEFAULT 0,
    tow INTEGER DEFAULT 0,
    interceptions INTEGER DEFAULT 0,
    tpl INTEGER DEFAULT 0,
    kp INTEGER DEFAULT 0,
    hp INTEGER DEFAULT 0,
    ha INTEGER DEFAULT 0,
    turnovers INTEGER DEFAULT 0,
    ineffective INTEGER DEFAULT 0,
    shot_short INTEGER DEFAULT 0,
    shot_save INTEGER DEFAULT 0,
    fouled INTEGER DEFAULT 0,
    woodwork INTEGER DEFAULT 0,

    -- Kickout Analysis - Drum (4 fields)
    ko_drum_kow INTEGER DEFAULT 0,
    ko_drum_wc INTEGER DEFAULT 0,
    ko_drum_bw INTEGER DEFAULT 0,
    ko_drum_sw INTEGER DEFAULT 0,

    -- Kickout Analysis - Opposition (4 fields)
    ko_opp_kow INTEGER DEFAULT 0,
    ko_opp_wc INTEGER DEFAULT 0,
    ko_opp_bw INTEGER DEFAULT 0,
    ko_opp_sw INTEGER DEFAULT 0,

    -- Attacking Play (5 fields)
    ta INTEGER DEFAULT 0,
    kr INTEGER DEFAULT 0,
    kl INTEGER DEFAULT 0,
    cr INTEGER DEFAULT 0,
    cl INTEGER DEFAULT 0,

    -- Shots from Play (11 fields)
    shots_play_total INTEGER DEFAULT 0,
    shots_play_points INTEGER DEFAULT 0,
    shots_play_2points INTEGER DEFAULT 0,
    shots_play_goals INTEGER DEFAULT 0,
    shots_play_wide INTEGER DEFAULT 0,
    shots_play_short INTEGER DEFAULT 0,
    shots_play_save INTEGER DEFAULT 0,
    shots_play_woodwork INTEGER DEFAULT 0,
    shots_play_blocked INTEGER DEFAULT 0,
    shots_play_45 INTEGER DEFAULT 0,
    shots_play_percentage NUMERIC(5, 4),

    -- Scoreable Frees (11 fields)
    frees_total INTEGER DEFAULT 0,
    frees_points INTEGER DEFAULT 0,
    frees_2points INTEGER DEFAULT 0,
    frees_goals INTEGER DEFAULT 0,
    frees_wide INTEGER DEFAULT 0,
    frees_short INTEGER DEFAULT 0,
    frees_save INTEGER DEFAULT 0,
    frees_woodwork INTEGER DEFAULT 0,
    frees_45 INTEGER DEFAULT 0,
    frees_qf INTEGER DEFAULT 0,
    frees_percentage NUMERIC(5, 4),

    -- Total Shots (2 fields)
    total_shots INTEGER DEFAULT 0,
    total_shots_percentage NUMERIC(5, 4),

    -- Assists (3 fields)
    assists_total INTEGER DEFAULT 0,
    assists_point INTEGER DEFAULT 0,
    assists_goal INTEGER DEFAULT 0,

    -- Tackles (4 fields)
    tackles_total INTEGER DEFAULT 0,
    tackles_contested INTEGER DEFAULT 0,
    tackles_missed INTEGER DEFAULT 0,
    tackles_percentage NUMERIC(5, 4),

    -- Frees Conceded (5 fields)
    frees_conceded_total INTEGER DEFAULT 0,
    frees_conceded_attack INTEGER DEFAULT 0,
    frees_conceded_midfield INTEGER DEFAULT 0,
    frees_conceded_defense INTEGER DEFAULT 0,
    frees_conceded_penalty INTEGER DEFAULT 0,

    -- 50m Free Conceded (4 fields)
    frees_50m_total INTEGER DEFAULT 0,
    frees_50m_delay INTEGER DEFAULT 0,
    frees_50m_dissent INTEGER DEFAULT 0,
    frees_50m_3v3 INTEGER DEFAULT 0,

    -- Bookings (3 fields)
    yellow_cards INTEGER DEFAULT 0,
    black_cards INTEGER DEFAULT 0,
    red_cards INTEGER DEFAULT 0,

    -- Throw Up (2 fields)
    throw_up_won INTEGER DEFAULT 0,
    throw_up_lost INTEGER DEFAULT 0,

    -- Goalkeeper Stats (5 fields)
    gk_total_kickouts INTEGER DEFAULT 0,
    gk_kickout_retained INTEGER DEFAULT 0,
    gk_kickout_lost INTEGER DEFAULT 0,
    gk_kickout_percentage NUMERIC(5, 4),
    gk_saves INTEGER DEFAULT 0,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_pms_match FOREIGN KEY (match_id)
        REFERENCES matches(match_id) ON DELETE CASCADE,
    CONSTRAINT fk_pms_player FOREIGN KEY (player_id)
        REFERENCES players(player_id) ON DELETE CASCADE,
    CONSTRAINT chk_pms_minutes CHECK (minutes_played >= 0 AND minutes_played <= 120)
);

COMMENT ON TABLE player_match_statistics IS 'Individual player performance metrics (86+ fields)';
COMMENT ON COLUMN player_match_statistics.scores IS 'GAA notation: G-PP(Ff) (e.g., "1-03(1f)")';
COMMENT ON COLUMN player_match_statistics.psr IS 'Possession Success Rate';

CREATE INDEX idx_pms_match ON player_match_statistics(match_id);
CREATE INDEX idx_pms_player ON player_match_statistics(player_id);
CREATE INDEX idx_pms_match_player ON player_match_statistics(match_id, player_id);
CREATE INDEX idx_pms_minutes ON player_match_statistics(minutes_played) WHERE minutes_played > 0;

-- =====================================================================
-- TABLE 9: kpi_definitions
-- Purpose: Store metric definitions and PSR values
-- =====================================================================
CREATE TABLE kpi_definitions (
    kpi_id SERIAL PRIMARY KEY,
    event_number INTEGER NOT NULL,
    event_name VARCHAR(100) NOT NULL,
    outcome VARCHAR(100) NOT NULL,
    team_assignment VARCHAR(50) NOT NULL,
    psr_value NUMERIC(5, 2) NOT NULL,
    definition TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_kpi_team CHECK (team_assignment IN ('Home', 'Opposition', 'Both'))
);

COMMENT ON TABLE kpi_definitions IS 'KPI and metric definitions';
COMMENT ON COLUMN kpi_definitions.psr_value IS 'Possession Success Rate value for this outcome';

CREATE INDEX idx_kpi_event ON kpi_definitions(event_number, event_name);
CREATE INDEX idx_kpi_outcome ON kpi_definitions(outcome);

-- =====================================================================
-- TRIGGERS: Auto-update timestamps
-- =====================================================================
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_seasons_updated
    BEFORE UPDATE ON seasons
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER trg_teams_updated
    BEFORE UPDATE ON teams
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER trg_competitions_updated
    BEFORE UPDATE ON competitions
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER trg_players_updated
    BEFORE UPDATE ON players
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER trg_matches_updated
    BEFORE UPDATE ON matches
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER trg_mts_updated
    BEFORE UPDATE ON match_team_statistics
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER trg_pms_updated
    BEFORE UPDATE ON player_match_statistics
    FOR EACH ROW EXECUTE FUNCTION update_timestamp();

-- =====================================================================
-- SEED DATA
-- =====================================================================

-- Seed positions
INSERT INTO positions (name, code, display_order) VALUES
    ('Goalkeeper', 'GK', 1),
    ('Defender', 'DEF', 2),
    ('Midfielder', 'MID', 3),
    ('Forward', 'FWD', 4);

-- Seed current season
INSERT INTO seasons (year, name, is_current) VALUES
    (2025, '2025 Season', TRUE);

-- Seed Drum team
INSERT INTO teams (name, abbreviation, is_drum, is_active) VALUES
    ('Drum', 'DRM', TRUE, TRUE);

-- =====================================================================
-- MIGRATION COMPLETE
-- =====================================================================
-- Tables: 9 created
-- Indexes: 30+ created
-- Constraints: All foreign keys, checks, and unique constraints applied
-- Triggers: 7 timestamp update triggers created
-- Seed Data: Positions, Season 2025, Drum team
-- =====================================================================
