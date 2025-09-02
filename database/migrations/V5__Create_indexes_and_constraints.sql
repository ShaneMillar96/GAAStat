-- GAA Statistics Database - Indexes and Performance Optimization Migration
-- Creates strategic indexes for common query patterns and additional constraints

-- Indexes for Core Tables
-- Matches table indexes
CREATE INDEX idx_matches_date ON matches(date);
CREATE INDEX idx_matches_season_id ON matches(season_id);
CREATE INDEX idx_matches_competition_id ON matches(competition_id);
CREATE INDEX idx_matches_opposition_id ON matches(opposition_id);
CREATE INDEX idx_matches_season_date ON matches(season_id, date);

-- Players table indexes
CREATE INDEX idx_players_name ON players(player_name);
CREATE INDEX idx_players_jersey_number ON players(jersey_number);
CREATE INDEX idx_players_position_id ON players(position_id);
CREATE INDEX idx_players_active ON players(is_active) WHERE is_active = TRUE;

-- Statistics Tables Indexes
-- Match Player Statistics - most frequently queried table
CREATE INDEX idx_match_player_stats_match_id ON match_player_statistics(match_id);
CREATE INDEX idx_match_player_stats_player_id ON match_player_statistics(player_id);
CREATE INDEX idx_match_player_stats_match_player ON match_player_statistics(match_id, player_id);
CREATE INDEX idx_match_player_stats_goals ON match_player_statistics(goals) WHERE goals > 0;
CREATE INDEX idx_match_player_stats_points ON match_player_statistics(points) WHERE points > 0;
CREATE INDEX idx_match_player_stats_minutes ON match_player_statistics(minutes_played);

-- Match Team Statistics indexes
CREATE INDEX idx_match_team_stats_match_id ON match_team_statistics(match_id);
CREATE INDEX idx_match_team_stats_metric_id ON match_team_statistics(metric_definition_id);
CREATE INDEX idx_match_team_stats_match_metric ON match_team_statistics(match_id, metric_definition_id);

-- Analytics Tables Indexes
-- Shot Analysis indexes
CREATE INDEX idx_shot_analysis_match_id ON shot_analysis(match_id);
CREATE INDEX idx_shot_analysis_player_id ON shot_analysis(player_id);
CREATE INDEX idx_shot_analysis_shot_type ON shot_analysis(shot_type_id);
CREATE INDEX idx_shot_analysis_outcome ON shot_analysis(shot_outcome_id);
CREATE INDEX idx_shot_analysis_match_player ON shot_analysis(match_id, player_id);

-- Kickout Analysis indexes
CREATE INDEX idx_kickout_analysis_match_id ON kickout_analysis(match_id);
CREATE INDEX idx_kickout_analysis_team_type ON kickout_analysis(team_type_id);
CREATE INDEX idx_kickout_analysis_kickout_type ON kickout_analysis(kickout_type_id);
CREATE INDEX idx_kickout_analysis_time_period ON kickout_analysis(time_period_id);

-- Scoreable Free Analysis indexes
CREATE INDEX idx_scoreable_free_match_id ON scoreable_free_analysis(match_id);
CREATE INDEX idx_scoreable_free_player_id ON scoreable_free_analysis(player_id);
CREATE INDEX idx_scoreable_free_success ON scoreable_free_analysis(success);
CREATE INDEX idx_scoreable_free_type ON scoreable_free_analysis(free_type_id);

-- Positional Analysis indexes
CREATE INDEX idx_positional_analysis_match_id ON positional_analysis(match_id);
CREATE INDEX idx_positional_analysis_position_id ON positional_analysis(position_id);
CREATE INDEX idx_positional_analysis_match_position ON positional_analysis(match_id, position_id);

-- Aggregation Tables Indexes
-- Season Player Totals indexes
CREATE INDEX idx_season_player_totals_player_id ON season_player_totals(player_id);
CREATE INDEX idx_season_player_totals_season_id ON season_player_totals(season_id);
CREATE INDEX idx_season_player_totals_games_played ON season_player_totals(games_played);
CREATE INDEX idx_season_player_totals_total_scores ON season_player_totals(total_scores);

-- Position Averages indexes
CREATE INDEX idx_position_averages_position_id ON position_averages(position_id);
CREATE INDEX idx_position_averages_season_id ON position_averages(season_id);

-- Reference Tables Indexes (for lookups)
CREATE INDEX idx_competitions_name ON competitions(competition_name);
CREATE INDEX idx_competitions_type ON competitions(competition_type_id);
CREATE INDEX idx_teams_name ON teams(team_name);
CREATE INDEX idx_seasons_current ON seasons(is_current) WHERE is_current = TRUE;
CREATE INDEX idx_seasons_dates ON seasons(start_date, end_date);

-- Composite Indexes for Common Query Patterns
-- Player performance over time
CREATE INDEX idx_player_season_performance ON match_player_statistics(player_id, match_id);

-- Team statistics by competition and season
CREATE INDEX idx_team_stats_comp_season ON matches(competition_id, season_id, date);

-- Match results analysis
CREATE INDEX idx_match_results_venue_season ON matches(venue_id, season_id, match_result_id);

-- Top scorers queries
CREATE INDEX idx_top_scorers ON match_player_statistics(goals DESC, points DESC) 
WHERE goals > 0 OR points > 0;

-- Player efficiency analysis
CREATE INDEX idx_player_efficiency ON match_player_statistics(engagement_efficiency DESC) 
WHERE engagement_efficiency IS NOT NULL;

-- Seasonal analysis composite index
CREATE INDEX idx_seasonal_analysis ON match_player_statistics(player_id, match_id)
INCLUDE (goals, points, minutes_played, engagement_efficiency);

-- JSON Indexes for JSON columns
CREATE INDEX idx_kpi_definitions_benchmarks ON kpi_definitions USING GIN(benchmark_values);
CREATE INDEX idx_kickout_outcome_breakdown ON kickout_analysis USING GIN(outcome_breakdown);

-- Additional Constraints and Validation
-- Ensure unique player jersey numbers per season (if needed)
-- This would require additional logic to track jersey numbers by season
-- For now, we'll allow jersey number changes across seasons

-- Ensure match date is within season boundaries
-- This constraint would be complex to implement at the database level
-- Better handled in application logic during data insertion

-- Performance hint: Partial indexes for common filtering patterns
CREATE INDEX idx_active_players_by_position ON players(position_id) 
WHERE is_active = TRUE;

-- Note: CURRENT_DATE is not immutable, so we cannot use it in a partial index predicate
-- This index would need to be recreated periodically or handled differently
-- CREATE INDEX idx_recent_matches ON matches(date) 
-- WHERE date >= CURRENT_DATE - INTERVAL '1 year';

CREATE INDEX idx_successful_kickouts ON kickout_analysis(match_id, success_rate) 
WHERE success_rate > 0.5;

-- Statistics for query optimization
-- These will be automatically updated but we can ensure they exist
ANALYZE matches;
ANALYZE players;
ANALYZE match_player_statistics;
ANALYZE match_team_statistics;