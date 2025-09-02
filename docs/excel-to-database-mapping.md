# Excel to Database Mapping Guide

## Executive Summary

This document provides a comprehensive mapping between the data contained in `Drum Analysis 2025.xlsx` and the PostgreSQL database schema defined in the GAAStat application. It serves as the definitive reference for implementing the ETL (Extract, Transform, Load) job that will migrate Excel data into the normalized database structure.

The Excel file contains **32 sheets** with statistical data from 8 GAA football matches, organized into team statistics, player statistics, specialized analytics, and aggregated summaries. The database schema is designed to store this data in a normalized, queryable format with appropriate relationships and data integrity constraints.

## Sheet Categories and Database Mapping Overview

### 1. Match Team Statistics (8 sheets)
**Source**: `0X. [Competition] vs [Opposition] [Date]` sheets  
**Target**: `matches`, `match_team_statistics`, and related lookup tables  
**Contains**: ~235 statistical metrics per match with first half, second half, and full game breakdowns

### 2. Match Player Statistics (8 sheets)  
**Source**: `0X. Player Stats vs [Opposition] [Date]` sheets  
**Target**: `match_player_statistics` table  
**Contains**: ~80+ individual performance fields per player per match

### 3. Specialized Analytics (6 sheets)
**Source**: `Kickout Analysis Data`, `Shots from play Data`, `Scoreable Frees Data`  
**Target**: `kickout_analysis`, `shot_analysis`, `scoreable_free_analysis` tables  
**Contains**: Event-level tracking with timestamps and outcomes

### 4. Position-Based Analysis (4 sheets)
**Source**: `Goalkeepers`, `Defenders`, `Midfielders`, `Forwards`  
**Target**: `positional_analysis`, `position_averages` tables  
**Contains**: Aggregated statistics grouped by playing position

### 5. Summary and Reference (5 sheets)
**Source**: `Cumulative Stats 2025`, `Player Matrix`, `KPI Definitions`  
**Target**: `season_player_totals`, `players`, `metric_definitions` tables  
**Contains**: Season totals, player master data, and metric definitions

---

## Detailed Sheet-by-Sheet Mappings

## 1. Match Team Statistics Sheets

### Source Data Pattern
**Excel Sheets**: `08. Championship vs Magilligan `, `07. Drum vs Lissan 03.08.25`, etc.  
**Structure**: 236 rows × 18 columns with multi-level data organization

### Target Database Tables

#### 1.1 matches Table
```sql
-- Match header information extraction
INSERT INTO matches (
    match_number,           -- From sheet name (e.g., "08")
    date,                   -- From sheet name (e.g., "17.08.25")
    competition_id,         -- Lookup: "Championship" → competitions table
    opposition_id,          -- Lookup: "Magilligan" → teams table
    venue_id,              -- Derive from context (H/A)
    drum_score,            -- Row 3, Column 1: "2-06"
    opposition_score,      -- Row 3, Column 2: "0-01"
    drum_goals,            -- Parse from drum_score: "2-06" → 2
    drum_points,           -- Parse from drum_score: "2-06" → 6
    opposition_goals,      -- Parse from opposition_score: "0-01" → 0
    opposition_points,     -- Parse from opposition_score: "0-01" → 1
    point_difference,      -- Calculate: (2*3+6) - (0*3+1) = 11
    match_result_id,       -- Derive: W/L/D based on point_difference
    season_id             -- Lookup: "2025" → seasons table
)
```

#### 1.2 match_team_statistics Table
```sql
-- Statistical metrics mapping (235 rows per match)
INSERT INTO match_team_statistics (
    match_id,                    -- Foreign key to matches table
    metric_definition_id,        -- Lookup metric name in metric_definitions
    drum_first_half,            -- Column 1 value
    drum_second_half,           -- Column 2 value  
    drum_full_game,             -- Column 3 value
    opposition_first_half,      -- Column 4 value
    opposition_second_half,     -- Column 5 value
    opposition_full_game        -- Column 6 value
)
```

### Key Data Transformations

#### Score Format Parsing
```
Excel: "2-06" → Database: goals=2, points=6
Excel: "1-08" → Database: goals=1, points=8
```

#### Percentage Value Conversion
```
Excel: 0.5754189944134078 → Database: 0.575419 (DECIMAL(8,6))
Excel: NaN → Database: NULL
```

#### Metric Name Standardization
```
Excel Row Name              → Database metric_definition.metric_name
"Total Possession"          → "total_possession"
"Kickout Long"              → "kickout_long"
"Score source"              → "score_source"
```

### Sample ETL Logic
```sql
-- Example metric extraction for "Total Possession"
INSERT INTO match_team_statistics (
    match_id, 
    metric_definition_id, 
    drum_first_half, 
    drum_second_half, 
    drum_full_game,
    opposition_first_half,
    opposition_second_half, 
    opposition_full_game
) VALUES (
    @match_id,
    (SELECT metric_id FROM metric_definitions WHERE metric_name = 'total_possession'),
    0.575419,  -- Column 1 from Excel row 4
    0.398058,  -- Column 2 from Excel row 4
    0.480519,  -- Column 3 from Excel row 4
    0.424581,  -- Column 4 from Excel row 4
    0.601942,  -- Column 5 from Excel row 4
    NULL       -- Column 6 from Excel row 4 (if empty)
);
```

---

## 2. Match Player Statistics Sheets

### Source Data Pattern
**Excel Sheets**: `08. Player stats vs Magilligan `, `07. Player Stats vs Lissan 03.0`, etc.  
**Structure**: 21 rows (players) × 85 columns (statistics)

### Target Database Table: match_player_statistics

#### Column Mapping Reference
| Excel Column | Database Field | Data Type | Transform |
|--------------|----------------|-----------|-----------|
| `#` | - | - | Player lookup reference |
| `Player Name` | player_id | INTEGER | Lookup in players table |
| `Min` | minutes_played | INTEGER | Direct mapping |
| `TE` | total_engagements | INTEGER | Direct mapping |
| `TE/PSR` | engagement_efficiency | DECIMAL(5,4) | Direct mapping |
| `Scores` | scores | VARCHAR(20) | Direct mapping (e.g., "1-03(2f)") |
| `PSR` | possession_success_rate | DECIMAL(5,4) | Direct mapping |
| `PSR/TP` | possessions_per_te | DECIMAL(5,2) | Direct mapping |
| `TP` | total_possessions | INTEGER | Direct mapping |
| `ToW` | turnovers_won | INTEGER | Direct mapping |
| `Int` | interceptions | INTEGER | Direct mapping |
| `TPL` | - | - | Map to appropriate field |
| `KP` | - | - | Kick passes |
| `HP` | - | - | Hand passes |
| `Ta` | total_attacks | INTEGER | Direct mapping |
| `KR` | kick_retained | INTEGER | Direct mapping |
| `KL` | kick_lost | INTEGER | Direct mapping |
| `CR` | carry_retained | INTEGER | Direct mapping |
| `CL` | carry_lost | INTEGER | Direct mapping |
| `Tot` | shots_total | INTEGER | Direct mapping |
| `Pts` | points | INTEGER | Direct mapping |
| `Gls` | goals | INTEGER | Direct mapping |
| `Wid` | wides | INTEGER | Direct mapping |

### Advanced Player Statistics Mapping

#### Goalkeeping Statistics (Conditional)
```sql
-- Only populated for goalkeepers (jersey #1)
kickouts_total,         -- Excel column "KoR" + "KoL"
kickouts_retained,      -- Excel column "KoR"
kickouts_lost,          -- Excel column "KoL"  
kickout_percentage,     -- Excel column "%"
saves                   -- Excel column "Saves"
```

#### Disciplinary Records
```sql
-- Cards and fouls mapping
frees_conceded_total,      -- Sum of frees conceded columns
yellow_cards,              -- Excel "Y" column
black_cards,               -- Excel "B" column  
red_cards,                 -- Excel "R" column
```

### Data Validation Rules

#### Player Name Resolution
```sql
-- Player lookup strategy
SELECT player_id FROM players 
WHERE LOWER(TRIM(player_name)) = LOWER(TRIM(@excel_player_name))
   OR SOUNDEX(player_name) = SOUNDEX(@excel_player_name);

-- If not found, create new player record
INSERT INTO players (player_name, jersey_number, is_active)
VALUES (@excel_player_name, @jersey_number, true);
```

#### Score Parsing Logic
```sql
-- Parse complex score formats like "1-03(2f)"
-- Goals: 1, Points: 3, Free kicks: 2
CASE 
    WHEN scores LIKE '%(%' THEN
        -- Extract goals: SUBSTRING_INDEX(scores, '-', 1)
        -- Extract points: SUBSTRING_INDEX(SUBSTRING_INDEX(scores, '(', 1), '-', -1)
        -- Extract free kicks: SUBSTRING between '(' and 'f)'
    ELSE
        -- Simple format: "0-02" → goals=0, points=2
END
```

---

## 3. Specialized Analytics Sheets

### 3.1 Kickout Analysis Data

**Source Sheet**: `Kickout Analysis Data`  
**Target Table**: `kickout_analysis`

#### Column Mapping
| Excel Column | Database Field | Transform |
|--------------|----------------|-----------|
| `Event` | - | Sequence identifier |
| `Time` | - | Used for time_period_id lookup |
| `Period` | time_period_id | 1→"First Half", 2→"Second Half" |
| `Team Name` | team_type_id | "Drum"→1, Opposition→2 |
| `Name` | kickout_type_id | Long/Short classification |
| `Outcome` | outcome_breakdown | JSON mapping |
| `Player` | - | Player context (optional) |
| `Location` | - | Field position reference |
| `Competition` | - | Match context |
| `Teams` | match_id | Lookup match by team names + date |

#### Sample ETL Implementation
```sql
INSERT INTO kickout_analysis (
    match_id,
    time_period_id,
    kickout_type_id,
    team_type_id,
    total_attempts,
    successful,
    success_rate,
    outcome_breakdown
)
SELECT 
    m.match_id,
    CASE ka.Period 
        WHEN 1 THEN (SELECT time_period_id FROM time_periods WHERE period_name = 'First Half')
        WHEN 2 THEN (SELECT time_period_id FROM time_periods WHERE period_name = 'Second Half')
    END,
    kt.kickout_type_id,
    tt.team_type_id,
    COUNT(*) as total_attempts,
    SUM(CASE WHEN ka.Outcome IN ('Won Clean', 'Break Won', 'Short Won') THEN 1 ELSE 0 END) as successful,
    ROUND(SUM(CASE WHEN ka.Outcome IN ('Won Clean', 'Break Won', 'Short Won') THEN 1 ELSE 0 END) * 1.0 / COUNT(*), 4),
    JSON_OBJECT(
        'won_clean', SUM(CASE WHEN ka.Outcome = 'Won Clean' THEN 1 ELSE 0 END),
        'break_won', SUM(CASE WHEN ka.Outcome = 'Break Won' THEN 1 ELSE 0 END),
        'short_won', SUM(CASE WHEN ka.Outcome = 'Short Won' THEN 1 ELSE 0 END),
        'sideline_ball', SUM(CASE WHEN ka.Outcome = 'Sideline Ball' THEN 1 ELSE 0 END)
    ) as outcome_breakdown
FROM excel_kickout_analysis ka
JOIN matches m ON m.opposition_id = (SELECT team_id FROM teams WHERE team_name LIKE '%' + SUBSTRING_INDEX(ka.Teams, ' vs ', -1) + '%')
JOIN kickout_types kt ON kt.type_name = ka.Name  
JOIN team_types tt ON tt.type_name = CASE WHEN ka.`Team Name` = 'Drum' THEN 'Drum' ELSE 'Opposition' END
GROUP BY m.match_id, ka.Period, kt.kickout_type_id, tt.team_type_id;
```

### 3.2 Shots from Play Data

**Source Sheet**: `Shots from play Data`  
**Target Table**: `shot_analysis`

#### Key Mapping Logic
```sql
INSERT INTO shot_analysis (
    match_id,
    player_id,
    shot_number,
    time_period,
    shot_type_id,
    shot_outcome_id,
    position_area_id
)
SELECT 
    m.match_id,
    p.player_id,
    ROW_NUMBER() OVER (PARTITION BY m.match_id ORDER BY spd.Time),
    CONCAT(
        CASE spd.Period WHEN 1 THEN '1H:' ELSE '2H:' END,
        spd.Time
    ),
    st.shot_type_id,  -- Always "From Play" for this sheet
    so.shot_outcome_id,
    pa.position_area_id
FROM excel_shots_play_data spd
JOIN matches m ON derive_match_from_teams_column(spd.Teams, spd.Competition)
JOIN players p ON p.player_name = spd.Player  
JOIN shot_types st ON st.type_name = 'From Play'
JOIN shot_outcomes so ON so.outcome_name = spd.Outcome
JOIN position_areas pa ON derive_area_from_location(spd.Location);
```

### 3.3 Scoreable Frees Data

**Source Sheet**: `Scoreable Frees Data`  
**Target Table**: `scoreable_free_analysis`

#### Free Type Classification
```sql
-- Determine free type from context
CASE 
    WHEN sfd.Name LIKE '%Quick%' THEN 'Quick'
    ELSE 'Standard'
END as free_type,

-- Success determination
CASE 
    WHEN sfd.Outcome IN ('Goal', 'Point', '2 Pointer') THEN TRUE
    ELSE FALSE
END as success,

-- Distance calculation (derived from Location if available)
CASE 
    WHEN sfd.Location BETWEEN 13 AND 21 THEN '13-21m'
    WHEN sfd.Location BETWEEN 22 AND 30 THEN '22-30m'
    WHEN sfd.Location > 30 THEN '30m+'
    ELSE 'Close Range'
END as distance
```

---

## 4. Position-Based Analysis Sheets

### Source Sheets
- `Goalkeepers`
- `Defenders` 
- `Midfielders`
- `Forwards`

### Target Tables
- `positional_analysis` (match-specific position stats)
- `position_averages` (seasonal benchmarks)

### Mapping Strategy
```sql
-- Extract aggregated position statistics
INSERT INTO positional_analysis (
    match_id,
    position_id,
    avg_engagement_efficiency,
    avg_possession_success_rate,
    avg_conversion_rate,
    avg_tackle_success_rate,
    total_scores,
    total_possessions,
    total_tackles
)
SELECT 
    @match_id,
    pos.position_id,
    AVG(mps.engagement_efficiency),
    AVG(mps.possession_success_rate),
    AVG(mps.conversion_rate),
    AVG(mps.tackle_percentage),
    SUM(mps.goals * 3 + mps.points),
    SUM(mps.total_possessions),
    SUM(mps.tackles_total)
FROM match_player_statistics mps
JOIN players p ON p.player_id = mps.player_id
JOIN positions pos ON pos.position_name = p.position
WHERE mps.match_id = @match_id
GROUP BY pos.position_id;
```

---

## 5. Cumulative Stats and Reference Data

### 5.1 Cumulative Stats 2025 Sheet

**Source**: Season summary statistics  
**Target Table**: `season_player_totals`

#### Key Aggregations
```sql
INSERT INTO season_player_totals (
    player_id,
    season_id,
    games_played,
    total_minutes,
    avg_engagement_efficiency,
    avg_possession_success_rate,
    total_scores,
    total_goals,
    total_points,
    avg_conversion_rate,
    total_tackles,
    avg_tackle_success_rate,
    total_turnovers_won,
    total_interceptions
)
SELECT 
    p.player_id,
    s.season_id,
    COUNT(DISTINCT mps.match_id),
    SUM(mps.minutes_played),
    AVG(mps.engagement_efficiency),
    AVG(mps.possession_success_rate),
    SUM(mps.goals * 3 + mps.points),
    SUM(mps.goals),
    SUM(mps.points),
    AVG(mps.conversion_rate),
    SUM(mps.tackles_total),
    AVG(mps.tackle_percentage),
    SUM(mps.turnovers_won),
    SUM(mps.interceptions)
FROM match_player_statistics mps
JOIN players p ON p.player_id = mps.player_id
JOIN matches m ON m.match_id = mps.match_id
JOIN seasons s ON s.season_id = m.season_id
WHERE s.season_name = '2025'
GROUP BY p.player_id, s.season_id;
```

### 5.2 Player Matrix Sheet

**Source**: Squad information and player details  
**Target Table**: `players`

#### Player Master Data Creation
```sql
INSERT INTO players (
    player_name,
    jersey_number,
    position_id,
    is_active
)
SELECT DISTINCT
    TRIM(pm.PlayerName),
    pm.JerseyNumber,
    pos.position_id,
    CASE WHEN pm.Status = 'Active' THEN TRUE ELSE FALSE END
FROM excel_player_matrix pm
JOIN positions pos ON pos.position_name = pm.Position
ON CONFLICT (player_name) DO UPDATE SET
    jersey_number = EXCLUDED.jersey_number,
    position_id = EXCLUDED.position_id,
    is_active = EXCLUDED.is_active;
```

### 5.3 KPI Definitions Sheet

**Source**: Metric definitions and calculations  
**Target Tables**: `kpi_definitions`, `metric_definitions`

#### KPI Definition Mapping
```sql
INSERT INTO kpi_definitions (
    kpi_code,
    kpi_name,
    description,
    calculation_formula,
    benchmark_values,
    position_relevance
)
SELECT 
    kd.Code,
    kd.KPIName,
    kd.Description,
    kd.CalculationMethod,
    JSON_OBJECT(
        'excellent', kd.ExcellentThreshold,
        'good', kd.GoodThreshold,
        'average', kd.AverageThreshold,
        'poor', kd.PoorThreshold
    ),
    kd.ApplicablePositions
FROM excel_kpi_definitions kd;
```

---

## ETL Implementation Guidelines

### 1. Processing Order
1. **Reference Data First**: Load competitions, teams, seasons, positions, etc.
2. **Match Headers**: Create match records from team stats sheet headers
3. **Team Statistics**: Process 235 metrics per match into match_team_statistics
4. **Player Statistics**: Load individual performance data
5. **Specialized Analytics**: Process event-level data (kickouts, shots, frees)
6. **Position Analysis**: Calculate aggregated position statistics
7. **Season Totals**: Generate cumulative statistics

### 2. Data Quality Checks
```sql
-- Validation queries to run after ETL
-- Ensure score consistency
SELECT match_id, 
       drum_goals, drum_points, 
       (drum_goals * 3 + drum_points) as calculated_total,
       (SELECT SUM(goals * 3 + points) FROM match_player_statistics WHERE match_id = m.match_id) as player_total
FROM matches m
WHERE calculated_total != player_total;

-- Check for missing players
SELECT DISTINCT mps.player_id 
FROM match_player_statistics mps
LEFT JOIN players p ON p.player_id = mps.player_id
WHERE p.player_id IS NULL;

-- Verify statistical totals
SELECT match_id,
       (SELECT drum_full_game FROM match_team_statistics 
        WHERE match_id = m.match_id AND metric_definition_id = 
        (SELECT metric_id FROM metric_definitions WHERE metric_name = 'total_possession')) as team_possession,
       AVG(mps.possession_success_rate) as avg_player_psr
FROM matches m
JOIN match_player_statistics mps ON mps.match_id = m.match_id
GROUP BY match_id;
```

### 3. Error Handling Strategies

#### Missing Data Patterns
- **Empty Cells**: Convert to NULL values
- **"NaN" Strings**: Convert to NULL values  
- **Zero vs NULL**: Distinguish between actual zero values and missing data
- **Player Name Variations**: Use fuzzy matching with SOUNDEX

#### Data Type Conversions
```sql
-- Safe decimal conversion
CASE 
    WHEN ISNUMERIC(excel_value) = 1 AND excel_value != 'NaN' 
    THEN CAST(excel_value AS DECIMAL(8,6))
    ELSE NULL 
END

-- Score parsing with error handling
CASE 
    WHEN score_string LIKE '_-__' 
    THEN {
        goals: CAST(SUBSTRING(score_string, 1, 1) AS INT),
        points: CAST(SUBSTRING(score_string, 3, 2) AS INT)
    }
    ELSE NULL
END
```

### 4. Performance Optimization

#### Batch Processing Recommendations
- Process matches in chronological order
- Use bulk inserts for player statistics (21 players × 8 matches = 168 records)
- Implement transaction boundaries per match
- Use prepared statements for repeated inserts

#### Indexing Strategy for ETL
```sql
-- Temporary indexes during ETL (drop after completion)
CREATE INDEX idx_temp_player_lookup ON players (LOWER(TRIM(player_name)));
CREATE INDEX idx_temp_match_lookup ON matches (date, opposition_id);
CREATE INDEX idx_temp_metric_lookup ON metric_definitions (metric_name);
```

### 5. Logging and Monitoring

#### ETL Process Tracking
```sql
-- ETL execution log
CREATE TABLE etl_execution_log (
    execution_id SERIAL PRIMARY KEY,
    sheet_name VARCHAR(100),
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    records_processed INTEGER,
    records_successful INTEGER,
    records_failed INTEGER,
    error_summary TEXT
);
```

#### Data Quality Metrics
- Record counts per table after ETL
- Percentage of NULL values by critical fields
- Statistical consistency checks (team vs player totals)
- Foreign key integrity verification

This comprehensive mapping document provides the foundation for implementing a robust ETL job that will accurately migrate all Excel data into the PostgreSQL database while maintaining data integrity and enabling powerful analytics capabilities.