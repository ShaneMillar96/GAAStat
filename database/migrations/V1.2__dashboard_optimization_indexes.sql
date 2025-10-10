-- =====================================================================
-- Flyway Migration: V1.2 - Dashboard Optimization Indexes
-- =====================================================================
-- Purpose: Create optimized indexes for dashboard queries
-- Expected Impact: 50-70% performance improvement for aggregations
-- Related JIRA: GAAS-10 (Dashboard Implementation)
-- Created: 2025-10-10
-- PostgreSQL Version: 11+ (requires INCLUDE clause support)
-- =====================================================================
-- PERFORMANCE TARGETS:
-- - All dashboard queries: < 50ms (cold cache)
-- - Top 3 PSR Leaders: < 5ms
-- - Top 3 Composite Scores: < 8ms
-- - Top Scorer: < 10ms
-- - Team Possession: < 3ms
-- =====================================================================

-- =====================================================================
-- INDEX 1: Player Match Statistics Aggregation Optimization
-- =====================================================================
-- Purpose: Optimize player-level aggregations with competition filtering
-- Supports: All dashboard widgets requiring player statistics
-- Expected Impact: 50-70% reduction in query time for filtered aggregations
--
-- This covering index enables index-only scans for the most common
-- aggregation patterns in dashboard queries. The INCLUDE clause stores
-- frequently accessed columns directly in the index, eliminating the
-- need for table lookups.
--
-- Rationale:
-- 1. player_id as leading column for GROUP BY player_id
-- 2. match_id for JOIN optimization with matches table
-- 3. INCLUDE clause stores frequently accessed aggregation columns
-- 4. PostgreSQL 11+ supports INCLUDE for index-only scans
-- =====================================================================

CREATE INDEX IF NOT EXISTS idx_pms_aggregation_optimized
ON player_match_statistics (
    player_id,
    match_id
) INCLUDE (
    psr,
    total_engagements,
    minutes_played,
    shots_play_goals,
    shots_play_points,
    shots_play_2points,
    frees_goals,
    frees_points,
    frees_2points,
    frees_total,
    ko_drum_kow,
    ko_opp_kow
);

COMMENT ON INDEX idx_pms_aggregation_optimized IS
'Covering index for dashboard aggregation queries. Optimizes GROUP BY player_id with frequently accessed statistics in INCLUDE clause for index-only scans. (GAAS-10)';

-- =====================================================================
-- INDEX 2: Competition Type Filter Optimization
-- =====================================================================
-- Purpose: Optimize competition type filtering in WHERE clauses
-- Supports: All filtered dashboard queries
-- Expected Impact: Instant competition type lookups
--
-- While competitions is a small table (3 rows), this composite index
-- ensures optimal performance for the critical competition type filter
-- used in every dashboard query. The (type, competition_id) ordering
-- enables efficient range scans and joins.
--
-- Rationale:
-- 1. type as leading column for WHERE type = 'League'
-- 2. competition_id for efficient JOINs to matches table
-- 3. Small table (3 rows) but critical for all filtered queries
-- =====================================================================

CREATE INDEX IF NOT EXISTS idx_competitions_type_id
ON competitions (
    type,
    competition_id
);

COMMENT ON INDEX idx_competitions_type_id IS
'Optimizes competition type filtering for dashboard queries. Supports efficient WHERE type = ''League'' clauses and subsequent JOINs to matches table. (GAAS-10)';

-- =====================================================================
-- INDEX 3: Match-Competition Join Optimization
-- =====================================================================
-- Purpose: Optimize matches → competitions JOIN with covering index
-- Supports: All dashboard queries requiring competition filtering
-- Expected Impact: Sub-millisecond JOIN performance
--
-- This covering index enhances the existing idx_matches_competition
-- by including commonly accessed match fields (match_date, home_team_id,
-- away_team_id) directly in the index. This eliminates table lookups
-- for these frequently queried columns.
--
-- Rationale:
-- 1. competition_id for JOIN optimization
-- 2. match_id for subsequent JOINs to statistics tables
-- 3. INCLUDE clause avoids table lookups for common fields
--
-- Note: We create a new index rather than modifying the existing one
-- (idx_matches_competition) to maintain backward compatibility with any
-- queries relying on the original index structure.
-- =====================================================================

CREATE INDEX IF NOT EXISTS idx_matches_competition_optimized
ON matches (
    competition_id,
    match_id
) INCLUDE (
    match_date,
    home_team_id,
    away_team_id
);

COMMENT ON INDEX idx_matches_competition_optimized IS
'Covering index for matches-competitions JOINs. INCLUDE clause stores match_date and team IDs for index-only scans in dashboard queries. (GAAS-10)';

-- =====================================================================
-- ANALYZE: Update Query Planner Statistics
-- =====================================================================
-- Analyze all affected tables to ensure the query planner has up-to-date
-- statistics and can make optimal decisions about index usage.
-- =====================================================================

ANALYZE player_match_statistics;
ANALYZE matches;
ANALYZE competitions;
ANALYZE players;

-- =====================================================================
-- VERIFICATION: Display Index Information
-- =====================================================================
-- Show the newly created indexes and their sizes for verification.
-- Expected index sizes with current data (152 player_match_statistics records):
-- - idx_pms_aggregation_optimized: ~50-80KB
-- - idx_competitions_type_id: ~8KB
-- - idx_matches_competition_optimized: ~8KB
-- =====================================================================

DO $$
BEGIN
    RAISE NOTICE '=======================================================';
    RAISE NOTICE 'Dashboard Optimization Indexes Created Successfully';
    RAISE NOTICE '=======================================================';
    RAISE NOTICE 'Next Steps:';
    RAISE NOTICE '1. Run verification queries to test index usage';
    RAISE NOTICE '2. Monitor query performance for dashboard endpoints';
    RAISE NOTICE '3. Check index usage statistics after 1 week';
    RAISE NOTICE '=======================================================';
END $$;

SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
    pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) AS table_size
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
    AND indexname IN (
        'idx_pms_aggregation_optimized',
        'idx_competitions_type_id',
        'idx_matches_competition_optimized'
    )
ORDER BY pg_relation_size(indexrelid) DESC;

-- =====================================================================
-- MIGRATION COMPLETE - V1.2
-- =====================================================================
-- IMPORTANT: After deployment, run EXPLAIN ANALYZE on dashboard queries
-- to verify the query planner is using the new indexes.
--
-- Example verification query:
-- EXPLAIN ANALYZE
-- SELECT p.player_id, p.full_name, SUM(pms.psr) AS total_psr
-- FROM player_match_statistics pms
-- JOIN players p ON pms.player_id = p.player_id
-- JOIN matches m ON pms.match_id = m.match_id
-- JOIN competitions c ON m.competition_id = c.competition_id
-- WHERE p.is_active = TRUE AND c.type = 'League'
-- GROUP BY p.player_id, p.full_name
-- ORDER BY total_psr DESC LIMIT 3;
--
-- Look for: "Index Only Scan using idx_pms_aggregation_optimized"
-- =====================================================================
