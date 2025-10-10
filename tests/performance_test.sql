-- =====================================================================
-- Performance Test Suite for Dashboard Queries
-- =====================================================================
-- Purpose: Validate query performance after index optimization
-- Usage: Run with \timing on
-- Success Criteria:
--   1. All queries use index scans (not sequential scans)
--   2. Execution time < 10ms for Top 3 queries
--   3. Execution time < 5ms for Team Possession
--   4. No temporary file creation (temp_blks_written = 0)
-- =====================================================================

\timing on

\echo '======================================================================'
\echo 'GAAStat Dashboard Query Performance Test Suite'
\echo 'Version: 1.0'
\echo 'Date: 2025-10-10'
\echo '======================================================================'
\echo ''

-- =====================================================================
-- Test 1: Top 3 PSR Leaders (League)
-- =====================================================================

\echo '=== Test 1: Top 3 PSR Leaders (League) ==='
\echo 'Target: < 10ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    p.player_id,
    p.full_name,
    p.jersey_number,
    pos.code AS position_code,
    SUM(pms.psr) AS total_psr,
    COUNT(DISTINCT pms.match_id) AS matches_played,
    ROUND(AVG(pms.psr), 2) AS avg_psr_per_match
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN positions pos ON p.position_id = pos.position_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    p.is_active = TRUE
    AND s.year = 2025
    AND c.type = 'League'
    AND pms.minutes_played > 0
GROUP BY p.player_id, p.full_name, p.jersey_number, pos.code
HAVING SUM(pms.minutes_played) >= 30
ORDER BY total_psr DESC
LIMIT 3;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 2: Top 3 Composite Scores (Championship)
-- =====================================================================

\echo '=== Test 2: Top 3 Composite Scores (Championship) ==='
\echo 'Target: < 15ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    p.player_id,
    p.full_name,
    p.jersey_number,
    pos.code AS position_code,
    SUM(pms.psr) AS total_psr,
    SUM(pms.total_engagements) AS total_engagements,
    SUM(pms.total_shots) AS total_shots,
    ROUND(
        SUM(pms.psr) * 0.4 +
        SUM(pms.total_engagements) * 0.3 +
        SUM(pms.total_shots) * 0.3,
        2
    ) AS composite_score,
    COUNT(DISTINCT pms.match_id) AS matches_played
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN positions pos ON p.position_id = pos.position_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    p.is_active = TRUE
    AND s.year = 2025
    AND c.type = 'Championship'
    AND pms.minutes_played > 0
GROUP BY p.player_id, p.full_name, p.jersey_number, pos.code
HAVING SUM(pms.minutes_played) >= 30
ORDER BY composite_score DESC
LIMIT 3;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 3: Top Scorer (League)
-- =====================================================================

\echo '=== Test 3: Top Scorer (League) ==='
\echo 'Target: < 20ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    p.player_id,
    p.full_name,
    p.jersey_number,
    pos.code AS position_code,
    SUM(pms.shots_play_goals + pms.frees_goals) AS total_goals,
    SUM(
        pms.shots_play_points +
        pms.frees_points +
        pms.shots_play_2points * 2 +
        pms.frees_2points * 2
    ) AS total_points_raw,
    SUM(
        (pms.shots_play_goals + pms.frees_goals) * 3 +
        pms.shots_play_points +
        pms.frees_points +
        pms.shots_play_2points * 2 +
        pms.frees_2points * 2
    ) AS total_score_points,
    CONCAT(
        SUM(pms.shots_play_goals + pms.frees_goals),
        '-',
        LPAD(
            SUM(
                pms.shots_play_points +
                pms.frees_points +
                pms.shots_play_2points * 2 +
                pms.frees_2points * 2
            )::TEXT,
            2,
            '0'
        )
    ) AS score_notation,
    COUNT(DISTINCT pms.match_id) AS matches_played
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN positions pos ON p.position_id = pos.position_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    p.is_active = TRUE
    AND s.year = 2025
    AND c.type = 'League'
    AND pms.minutes_played > 0
GROUP BY p.player_id, p.full_name, p.jersey_number, pos.code
HAVING SUM(
    (pms.shots_play_goals + pms.frees_goals) * 3 +
    pms.shots_play_points +
    pms.frees_points +
    pms.shots_play_2points * 2 +
    pms.frees_2points * 2
) > 0
ORDER BY total_score_points DESC
LIMIT 1;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 4: Best Free Taker (All Competitions)
-- =====================================================================

\echo '=== Test 4: Best Free Taker (All Competitions) ==='
\echo 'Target: < 15ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    p.player_id,
    p.full_name,
    p.jersey_number,
    pos.code AS position_code,
    SUM(pms.frees_total) AS total_frees_attempted,
    SUM(pms.frees_points + pms.frees_2points + pms.frees_goals) AS total_frees_scored,
    SUM(
        (pms.frees_goals * 3) +
        (pms.frees_2points * 2) +
        pms.frees_points
    ) AS total_points_from_frees,
    ROUND(
        SUM(pms.frees_points + pms.frees_2points + pms.frees_goals)::NUMERIC /
        NULLIF(SUM(pms.frees_total), 0) * 100,
        2
    ) AS free_accuracy_percentage,
    COUNT(DISTINCT pms.match_id) AS matches_played
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN positions pos ON p.position_id = pos.position_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    p.is_active = TRUE
    AND s.year = 2025
    AND pms.minutes_played > 0
GROUP BY p.player_id, p.full_name, p.jersey_number, pos.code
HAVING SUM(pms.frees_total) >= 5
ORDER BY free_accuracy_percentage DESC
LIMIT 1;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 5: Most Minutes Played (Cup)
-- =====================================================================

\echo '=== Test 5: Most Minutes Played (Cup) ==='
\echo 'Target: < 10ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    p.player_id,
    p.full_name,
    p.jersey_number,
    pos.code AS position_code,
    SUM(pms.minutes_played) AS total_minutes,
    COUNT(DISTINCT pms.match_id) AS matches_played,
    ROUND(AVG(pms.minutes_played), 2) AS avg_minutes_per_match
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN positions pos ON p.position_id = pos.position_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    p.is_active = TRUE
    AND s.year = 2025
    AND c.type = 'Cup'
    AND pms.minutes_played > 0
GROUP BY p.player_id, p.full_name, p.jersey_number, pos.code
ORDER BY total_minutes DESC
LIMIT 1;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 6: Most Kickouts Won (League)
-- =====================================================================

\echo '=== Test 6: Most Kickouts Won (League) ==='
\echo 'Target: < 15ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    p.player_id,
    p.full_name,
    p.jersey_number,
    pos.code AS position_code,
    SUM(pms.ko_drum_kow + pms.ko_opp_kow) AS total_kickouts_won,
    SUM(pms.ko_drum_kow) AS drum_kickouts_won,
    SUM(pms.ko_opp_kow) AS opposition_kickouts_won,
    COUNT(DISTINCT pms.match_id) AS matches_played
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN positions pos ON p.position_id = pos.position_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    p.is_active = TRUE
    AND s.year = 2025
    AND c.type = 'League'
    AND pms.minutes_played > 0
GROUP BY p.player_id, p.full_name, p.jersey_number, pos.code
HAVING SUM(pms.ko_drum_kow + pms.ko_opp_kow) > 0
ORDER BY total_kickouts_won DESC
LIMIT 1;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 7: Team Possession Statistics (All Competitions)
-- =====================================================================

\echo '=== Test 7: Team Possession Statistics (All) ==='
\echo 'Target: < 5ms execution time'
\echo ''

EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT
    t.team_id,
    t.name AS team_name,
    t.is_drum,
    COUNT(DISTINCT mts.match_id) AS matches_played,
    ROUND(AVG(mts.total_possession) * 100, 2) AS avg_possession_percentage,
    ROUND(MIN(mts.total_possession) * 100, 2) AS min_possession_percentage,
    ROUND(MAX(mts.total_possession) * 100, 2) AS max_possession_percentage
FROM match_team_statistics mts
JOIN matches m ON mts.match_id = m.match_id
JOIN teams t ON mts.team_id = t.team_id
JOIN competitions c ON m.competition_id = c.competition_id
JOIN seasons s ON c.season_id = s.season_id
WHERE
    s.year = 2025
    AND mts.period = 'Full'
GROUP BY t.team_id, t.name, t.is_drum
ORDER BY t.is_drum DESC, avg_possession_percentage DESC;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test 8: Index Usage Verification
-- =====================================================================

\echo '=== Test 8: Index Usage Verification ==='
\echo 'Checking if new indexes are being used...'
\echo ''

SELECT
    indexname,
    idx_scan AS times_used,
    idx_tup_read AS tuples_read,
    idx_tup_fetch AS tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
    AND indexname IN (
        'idx_pms_aggregation_optimized',
        'idx_competitions_type_id',
        'idx_matches_competition_optimized'
    )
ORDER BY indexname;

\echo ''
\echo '----------------------------------------------------------------------'
\echo ''

-- =====================================================================
-- Test Summary
-- =====================================================================

\echo '======================================================================'
\echo 'Performance Test Suite Complete'
\echo '======================================================================'
\echo ''
\echo 'Review Checklist:'
\echo '  1. All EXPLAIN plans show Index Scan (not Seq Scan)'
\echo '  2. Execution times meet targets (see above)'
\echo '  3. No temporary files created (temp_blks_written = 0)'
\echo '  4. New indexes show usage (idx_scan > 0)'
\echo ''
\echo 'Next Steps:'
\echo '  - If all tests pass: Deploy to production'
\echo '  - If any test fails: Review EXPLAIN output and adjust indexes'
\echo '  - Monitor production performance for 1 week'
\echo ''
\echo '======================================================================'
