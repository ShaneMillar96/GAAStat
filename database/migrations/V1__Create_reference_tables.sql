-- GAA Statistics Database - Reference Tables Migration
-- Creates all lookup and reference tables that other tables depend on

-- Competition Types
CREATE TABLE competition_types (
    competition_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT
);
COMMENT ON TABLE competition_types IS 'Classification of competition types (League, Championship, Cup)';

-- Venues
CREATE TABLE venues (
    venue_id SERIAL PRIMARY KEY,
    venue_code VARCHAR(20) NOT NULL UNIQUE,
    venue_description VARCHAR(100) NOT NULL
);
COMMENT ON TABLE venues IS 'Match venue designations (Home/Away)';

-- Match Results
CREATE TABLE match_results (
    match_result_id SERIAL PRIMARY KEY,
    result_code VARCHAR(10) NOT NULL UNIQUE,
    result_description VARCHAR(50) NOT NULL
);
COMMENT ON TABLE match_results IS 'Match outcome types (Win, Loss, Draw)';

-- Seasons
CREATE TABLE seasons (
    season_id SERIAL PRIMARY KEY,
    season_name VARCHAR(50) NOT NULL UNIQUE,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_current BOOLEAN DEFAULT FALSE,
    CONSTRAINT chk_season_dates CHECK (end_date > start_date)
);
COMMENT ON TABLE seasons IS 'Season definitions and date ranges';

-- Positions
CREATE TABLE positions (
    position_id SERIAL PRIMARY KEY,
    position_name VARCHAR(50) NOT NULL UNIQUE,
    position_category VARCHAR(20) NOT NULL,
    description TEXT
);
COMMENT ON TABLE positions IS 'Playing position definitions and categories';

-- Metric Categories
CREATE TABLE metric_categories (
    metric_category_id SERIAL PRIMARY KEY,
    category_name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT
);
COMMENT ON TABLE metric_categories IS 'Statistical metric category groupings';

-- Metric Definitions
CREATE TABLE metric_definitions (
    metric_id SERIAL PRIMARY KEY,
    metric_name VARCHAR(100) NOT NULL UNIQUE,
    metric_description TEXT,
    data_type VARCHAR(20) NOT NULL,
    calculation_method TEXT,
    metric_category_id INTEGER NOT NULL,
    FOREIGN KEY (metric_category_id) REFERENCES metric_categories(metric_category_id)
);
COMMENT ON TABLE metric_definitions IS 'Statistical metric explanations and calculation methods';

-- KPI Definitions
CREATE TABLE kpi_definitions (
    kpi_id SERIAL PRIMARY KEY,
    kpi_code VARCHAR(20) NOT NULL UNIQUE,
    kpi_name VARCHAR(100) NOT NULL,
    description TEXT,
    calculation_formula TEXT,
    benchmark_values JSONB,
    position_relevance VARCHAR(255)
);
COMMENT ON TABLE kpi_definitions IS 'KPI definitions with formulas and benchmark values';

-- Time Periods
CREATE TABLE time_periods (
    time_period_id SERIAL PRIMARY KEY,
    period_name VARCHAR(30) NOT NULL UNIQUE,
    description VARCHAR(100)
);
COMMENT ON TABLE time_periods IS 'Game period classifications (First Half, Second Half, Full Game)';

-- Kickout Types
CREATE TABLE kickout_types (
    kickout_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(30) NOT NULL UNIQUE,
    description VARCHAR(100)
);
COMMENT ON TABLE kickout_types IS 'Kickout classifications (Long, Short)';

-- Team Types
CREATE TABLE team_types (
    team_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(30) NOT NULL UNIQUE,
    description VARCHAR(100)
);
COMMENT ON TABLE team_types IS 'Team type designations (Drum, Opposition)';

-- Shot Types
CREATE TABLE shot_types (
    shot_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(100)
);
COMMENT ON TABLE shot_types IS 'Shot type classifications (From Play, Free Kick, Penalty)';

-- Shot Outcomes
CREATE TABLE shot_outcomes (
    shot_outcome_id SERIAL PRIMARY KEY,
    outcome_name VARCHAR(30) NOT NULL UNIQUE,
    description VARCHAR(100),
    is_score BOOLEAN DEFAULT FALSE
);
COMMENT ON TABLE shot_outcomes IS 'Shot outcome types (Goal, Point, Wide, Save, etc.)';

-- Position Areas
CREATE TABLE position_areas (
    position_area_id SERIAL PRIMARY KEY,
    area_name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(100)
);
COMMENT ON TABLE position_areas IS 'Field position areas (Attacking Third, Middle Third, Defensive Third)';

-- Free Types
CREATE TABLE free_types (
    free_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(30) NOT NULL UNIQUE,
    description VARCHAR(100)
);
COMMENT ON TABLE free_types IS 'Free kick types (Standard, Quick)';

-- Competitions (depends on competition_types)
CREATE TABLE competitions (
    competition_id SERIAL PRIMARY KEY,
    competition_name VARCHAR(100) NOT NULL,
    season VARCHAR(20) NOT NULL,
    competition_type_id INTEGER NOT NULL,
    FOREIGN KEY (competition_type_id) REFERENCES competition_types(competition_type_id)
);
COMMENT ON TABLE competitions IS 'Competition master data';

-- Teams
CREATE TABLE teams (
    team_id SERIAL PRIMARY KEY,
    team_name VARCHAR(100) NOT NULL UNIQUE,
    home_venue VARCHAR(100),
    county VARCHAR(50)
);
COMMENT ON TABLE teams IS 'Opposition team information';