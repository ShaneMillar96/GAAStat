# Drum Analysis 2025 Excel File - Data Structure Analysis

## Document Purpose

This document provides a comprehensive technical analysis of the data structure contained in `Drum Analysis 2025.xlsx` to inform database schema design for the GAAStat application. It identifies data entities, relationships, field types, and normalization requirements for storing GAA football statistics.

## Excel File Structure Overview

The Excel file contains **32 sheets** organized into the following categories:

### Sheet Categories

1. **Match Team Statistics** (8 sheets)
   - Format: `0X. [Competition] vs [Opposition] [Date]`
   - Contains team-level performance metrics for each match

2. **Match Player Statistics** (8 sheets)  
   - Format: `0X. Player Stats vs [Opposition] [Date]`
   - Contains individual player performance data for each match

3. **Position-Based Analysis** (4 sheets)
   - `Goalkeepers`, `Defenders`, `Midfielders`, `Forwards`
   - Aggregated statistics by playing position

4. **Specialized Analytics** (7 sheets)
   - `Kickout Analysis Data`, `Kickout Stats`
   - `Shots from play Data`, `Shots from Play Stats`
   - `Scoreable Frees Data`, `Scoreable Free Stats`
   - Contains focused analysis on specific aspects of play

5. **Summary and Reference** (5 sheets)
   - `Cumulative Stats 2025` - Season totals and averages
   - `Player Matrix` - Squad overview
   - `KPI Definitions` - Metric explanations
   - `Blank Team Stats`, `Blank Player Stats` - Templates
   - `CSV File` - Export format

## Core Data Entities Identified

### 1. Match Entity

**Primary Data Source**: Match team statistics sheets

```sql
-- Core match information
Match {
  match_id: INTEGER PRIMARY KEY
  match_number: INTEGER (1-8)
  date: DATE
  opposition_name: VARCHAR(100)
  venue: ENUM('H', 'A') -- Home/Away
  competition: VARCHAR(50) -- "League", "Championship", "Neal Carlin Cup"
  drum_score: VARCHAR(10) -- "X-XX" format
  opposition_score: VARCHAR(10) -- "X-XX" format
  drum_goals: INTEGER
  drum_points: INTEGER
  opposition_goals: INTEGER
  opposition_points: INTEGER
  result: ENUM('W', 'L', 'D')
  point_difference: INTEGER
}
```

### 2. Player Entity

**Primary Data Source**: Player statistics sheets, Player Matrix

```sql
Player {
  player_id: INTEGER PRIMARY KEY
  player_name: VARCHAR(100)
  jersey_number: INTEGER
  position: ENUM('Goalkeeper', 'Defender', 'Midfielder', 'Forward')
  is_active: BOOLEAN
}
```

### 3. Match Team Statistics Entity

**Primary Data Source**: Team match sheets (235 rows of data per match)

```sql
MatchTeamStats {
  match_team_stat_id: INTEGER PRIMARY KEY
  match_id: INTEGER FOREIGN KEY
  metric_category: VARCHAR(50) -- e.g., "Score source", "Kickout Long"
  metric_name: VARCHAR(100)
  drum_first_half: DECIMAL(10,6)
  drum_second_half: DECIMAL(10,6)
  drum_full_game: DECIMAL(10,6)
  opposition_first_half: DECIMAL(10,6)
  opposition_second_half: DECIMAL(10,6)
  opposition_full_game: DECIMAL(10,6)
}
```

### 4. Match Player Statistics Entity

**Primary Data Source**: Player match sheets

```sql
MatchPlayerStats {
  match_player_stat_id: INTEGER PRIMARY KEY
  match_id: INTEGER FOREIGN KEY
  player_id: INTEGER FOREIGN KEY
  minutes_played: INTEGER
  total_engagements: INTEGER
  engagement_efficiency: DECIMAL(5,4)
  scores: VARCHAR(20) -- Format like "1-03(2f)"
  possession_success_rate: DECIMAL(5,4)
  possessions_per_te: DECIMAL(5,2)
  total_possessions: INTEGER
  turnovers_won: INTEGER
  interceptions: INTEGER
  -- ... (80+ additional statistical fields)
}
```

## Detailed Data Structure Analysis

### Team Match Statistics Categories

Each match contains approximately **235 statistical data points** organized into categories:

#### 1. Possession Play Metrics
- Total Possession (percentage)
- Possession lost
- Turnovers
- Possessions in Open Play
- Kick Pass, Hand Pass counts

#### 2. Kickout Analysis  
```sql
KickoutStats {
  total_kickouts: INTEGER
  kickouts_won: INTEGER
  kickouts_won_percentage: DECIMAL(5,4)
  won_clean: INTEGER
  break_won: INTEGER
  short_won: INTEGER
  sideline_ball: INTEGER
}
```

#### 3. Attacking Play Metrics
```sql
AttackingStats {
  total_attacks: INTEGER
  attack_source_breakdown: JSON -- KoL, KoS, OKoL, OKoS, ToW, PL, SS, Th-Up
  kick_retained: INTEGER
  kick_lost: INTEGER
  carry_retained: INTEGER
  carry_lost: INTEGER
  attack_efficiency_percentage: DECIMAL(5,4)
}
```

#### 4. Shot Analysis
```sql
ShotStats {
  shots_from_play_total: INTEGER
  goals: INTEGER
  points: INTEGER
  two_pointers: INTEGER
  wides: INTEGER
  shorts: INTEGER
  saves: INTEGER
  blocked: INTEGER
  forty_fives: INTEGER
  woodwork: INTEGER
  conversion_rate_percentage: DECIMAL(5,4)
}
```

#### 5. Scoreable Free Statistics
```sql
ScoreableFreeStats {
  total_scoreable_frees: INTEGER
  goals_from_frees: INTEGER
  points_from_frees: INTEGER
  two_pointers_from_frees: INTEGER
  wides_from_frees: INTEGER
  shorts_from_frees: INTEGER
  quick_free: INTEGER
  conversion_rate_percentage: DECIMAL(5,4)
}
```

#### 6. Defensive Play Metrics
```sql
DefensiveStats {
  total_tackles: INTEGER
  contact_tackles: INTEGER
  missed_tackles: INTEGER
  tackle_success_percentage: DECIMAL(5,4)
  tackle_efficiency: DECIMAL(5,2)
  opponent_efficiency: DECIMAL(5,2)
}
```

### Player Match Statistics Structure

Each player's match performance contains **80+ data fields**:

#### Core Performance
- Minutes played
- Total engagements
- Engagement efficiency
- Scores (goals/points with free kick notation)
- PSR (Possession Success Rate)
- Total possessions

#### Detailed Breakdowns
```sql
PlayerMatchDetails {
  -- Possession metrics
  turnovers_won: INTEGER
  interceptions: INTEGER
  total_possession_lost: INTEGER
  kick_pass: INTEGER
  hand_pass: INTEGER
  handling_errors: INTEGER
  
  -- Attacking metrics
  total_attacks: INTEGER
  kick_retained: INTEGER
  kick_lost: INTEGER
  carry_retained: INTEGER
  carry_lost: INTEGER
  
  -- Shooting metrics
  shots_total: INTEGER
  goals: INTEGER
  points: INTEGER
  wides: INTEGER
  conversion_rate: DECIMAL(5,4)
  
  -- Defensive metrics
  tackles_total: INTEGER
  tackles_contact: INTEGER
  tackles_missed: INTEGER
  tackle_percentage: DECIMAL(5,4)
  
  -- Disciplinary
  frees_conceded_total: INTEGER
  frees_conceded_attacking: INTEGER
  frees_conceded_middle: INTEGER
  frees_conceded_defensive: INTEGER
  yellow_cards: INTEGER
  black_cards: INTEGER
  red_cards: INTEGER
  
  -- Goalkeeping (when applicable)
  kickouts_total: INTEGER
  kickouts_retained: INTEGER
  kickouts_lost: INTEGER
  kickout_percentage: DECIMAL(5,4)
  saves: INTEGER
}
```

## Specialized Analytics Data Structures

### 1. Kickout Analysis
Separate detailed tracking of kickout performance:
```sql
KickoutAnalysis {
  match_id: INTEGER FOREIGN KEY
  period: ENUM('First Half', 'Second Half', 'Full Game')
  kickout_type: ENUM('Long', 'Short')
  team: ENUM('Drum', 'Opposition')
  total_attempts: INTEGER
  successful: INTEGER
  success_rate: DECIMAL(5,4)
  outcome_breakdown: JSON -- Won clean, break won, etc.
}
```

### 2. Shot Analysis
Detailed shot tracking beyond basic statistics:
```sql
ShotAnalysis {
  match_id: INTEGER FOREIGN KEY
  player_id: INTEGER FOREIGN KEY
  shot_number: INTEGER
  shot_type: ENUM('From Play', 'Free Kick', 'Penalty')
  outcome: ENUM('Goal', 'Point', '2 Pointer', 'Wide', 'Short', 'Save', 'Block', '45', 'Woodwork')
  time_period: VARCHAR(10) -- e.g., "11-20"
  position_area: ENUM('Attacking Third', 'Middle Third', 'Defensive Third')
}
```

### 3. Scoreable Free Analysis
Specific tracking of free kick opportunities:
```sql
ScoreableFreeAnalysis {
  match_id: INTEGER FOREIGN KEY
  player_id: INTEGER FOREIGN KEY
  free_number: INTEGER
  free_type: ENUM('Standard', 'Quick')
  distance: VARCHAR(20)
  outcome: ENUM('Goal', 'Point', '2 Pointer', 'Wide', 'Short', 'Save', '45', 'Woodwork')
  success: BOOLEAN
}
```

## Data Relationships and Normalization

### Primary Relationships
1. **Match → MatchTeamStats** (1:Many)
2. **Match → MatchPlayerStats** (1:Many)  
3. **Player → MatchPlayerStats** (1:Many)
4. **Match → KickoutAnalysis** (1:Many)
5. **Match → ShotAnalysis** (1:Many)
6. **Player → ShotAnalysis** (1:Many)

### Lookup Tables Required
```sql
MetricDefinitions {
  metric_id: INTEGER PRIMARY KEY
  metric_name: VARCHAR(100)
  metric_description: TEXT
  metric_category: VARCHAR(50)
  data_type: VARCHAR(20)
  calculation_method: TEXT
}

Competitions {
  competition_id: INTEGER PRIMARY KEY
  competition_name: VARCHAR(100)
  season: VARCHAR(10)
  competition_type: ENUM('League', 'Championship', 'Cup')
}

Teams {
  team_id: INTEGER PRIMARY KEY
  team_name: VARCHAR(100)
  home_venue: VARCHAR(100)
  county: VARCHAR(50)
}
```

## Data Quality and Validation Requirements

### Data Integrity Rules
1. **Score Validation**: Goals and points must match formatted scores
2. **Percentage Constraints**: All percentage fields between 0 and 1
3. **Time Validation**: Minutes played ≤ match duration
4. **Statistical Consistency**: Team totals must equal sum of player stats
5. **Period Validation**: First half + Second half = Full game totals

### Missing Data Patterns
- NULL values represented as empty cells or "NaN"
- Some optional metrics not tracked for all players
- Goalkeeper-specific stats only for goalkeeping players
- Substitute players have reduced statistical coverage

## Key Performance Indicators (KPIs)

The file contains definitions for core GAA analytics metrics:

```sql
KPIDefinitions {
  kpi_code: VARCHAR(10) -- e.g., "1.0", "2.0"
  kpi_name: VARCHAR(50) -- e.g., "Kickout", "Attacks"
  description: TEXT
  calculation_formula: TEXT
  benchmark_values: JSON
  position_relevance: VARCHAR(100)
}
```

## Database Schema Recommendations

### Core Tables
1. **matches** - Match information and results
2. **players** - Player master data
3. **match_team_statistics** - Team-level match stats
4. **match_player_statistics** - Individual player match stats
5. **metric_definitions** - Statistical metric explanations

### Analytics Tables
6. **kickout_analysis** - Detailed kickout tracking
7. **shot_analysis** - Individual shot tracking
8. **scoreable_free_analysis** - Free kick performance
9. **positional_analysis** - Position-based aggregations

### Reference Tables
10. **competitions** - Competition master data
11. **teams** - Opposition team data
12. **seasons** - Season management
13. **positions** - Playing positions

### Aggregation Tables (Optional)
14. **season_player_totals** - Pre-calculated season stats
15. **position_averages** - Benchmark comparisons
16. **team_form_analysis** - Rolling performance metrics

## Technical Implementation Notes

### Data Import Considerations
1. **Excel Sheet Parsing**: 32 sheets require structured import process
2. **Data Type Conversion**: String percentages to decimal conversion
3. **Score Parsing**: "X-XX" format requires regex parsing
4. **NULL Handling**: Multiple NULL representation formats
5. **Validation**: Cross-sheet data consistency checks

### Index Strategy
```sql
-- Primary indexes
CREATE INDEX idx_match_date ON matches(date);
CREATE INDEX idx_match_opposition ON matches(opposition_name);
CREATE INDEX idx_player_name ON players(player_name);
CREATE INDEX idx_match_player_stats ON match_player_statistics(match_id, player_id);

-- Analytics indexes
CREATE INDEX idx_shot_analysis_match ON shot_analysis(match_id);
CREATE INDEX idx_kickout_analysis_match ON kickout_analysis(match_id);
```

### Storage Requirements
- **Estimated Storage per Match**: ~50KB team stats + ~200KB player stats
- **Season Storage**: ~2MB for 8 matches
- **Annual Storage**: ~8MB for full season (32 matches)

## Future Schema Evolution

### Extensibility Considerations
1. **Additional Metrics**: Schema should accommodate new KPI additions
2. **Video Integration**: Potential for linking to video timestamps
3. **GPS/Fitness Data**: Integration with player tracking systems
4. **Weather/Pitch Conditions**: Environmental factors
5. **Opposition Scouting**: Enhanced opponent analysis

This data structure analysis provides the foundation for designing a normalized, scalable database schema that can effectively store and analyze GAA football statistics as demonstrated in the Drum Analysis 2025 Excel file.