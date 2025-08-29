# Feature: GAA Statistics Database Rebuild and Schema Design

## Executive Summary
Complete rebuild of the GAAStat database schema to support comprehensive GAA football match statistics tracking, analysis, and reporting. The new schema will handle detailed player performance metrics, match events, specialized statistics (kickouts, shots, frees), and support multi-season data management for the Drum team and their opponents using a clear-and-reload import strategy.

## Problem Statement
The current database schema is too simplistic to capture the rich statistical data available in GAA match analysis Excel files. With 85+ player metrics, 16 event types, and complex performance calculations (PSR values), we need a normalized, scalable database architecture that can:
- Store detailed match-by-match player statistics (85+ columns per player)
- Track individual match events with timestamps and outcomes
- Calculate and store derived metrics (performance success rates, efficiency percentages)
- Support season-long cumulative statistics and positional analysis
- Enable complex queries for player comparisons and team analytics
- Handle complete data refresh on each Excel import (clear-and-reload strategy)

## Objectives & Success Metrics
- Successfully import all 8 existing matches from the 2025 season Excel file
- Store all 85 player statistics columns without data loss
- Support PSR (Performance Success Rate) calculations with correct values
- Enable queries for cumulative season statistics
- Maintain data integrity across all relationships
- Process Excel imports with clear-and-reload in under 30 seconds
- Support concurrent read operations for analytics dashboards
- Provide rollback capability from snapshot for failed imports

## Functional Requirements

### Core Functionality
1. **Clear-and-Reload Import Strategy**: Each Excel import completely clears existing match data and reloads everything
2. **Multi-Season Support**: Track statistics across multiple seasons and competitions
3. **Comprehensive Player Stats**: Store 85+ individual metrics per player per match
4. **Event Tracking**: Record match events with minute timestamps and outcomes
5. **PSR Calculation**: Support Performance Success Rate value assignments (-3 to +3 range)
6. **Positional Analysis**: Group players by position (Goalkeepers, Defenders, Midfielders, Forwards)
7. **Match Timeline**: Store detailed match event sequences
8. **Kickout Analysis**: Dedicated tracking for kickout statistics and retention rates
9. **Shot Analysis**: Detailed shot outcomes, sources, and efficiency metrics
10. **Free Kick Tracking**: Scoreable free statistics and conversion rates
11. **Rollback Capability**: Restore from last successful import if current import fails

### User Stories
1. **As a coach**, I want to upload match Excel files so that all statistics are completely refreshed with latest data
2. **As an analyst**, I want to query cumulative season statistics so that I can identify performance trends
3. **As a coach**, I want to compare player performances across matches so that I can make informed selection decisions
4. **As a team manager**, I want to track team performance metrics against different opponents
5. **As a system admin**, I want import failures to automatically rollback so that we never lose working data

## Technical Architecture

### Components
**Database Layer:**
- PostgreSQL with Flyway migrations
- Normalized schema with 20+ tables
- Optimized indexes for analytics queries
- Import snapshot mechanism for rollback

**DAL Models (Entity Framework Core):**
- Database-first approach with scaffolded models
- Partial classes for business logic extensions
- Repository pattern for data access

**Services:**
- ExcelImportService for processing match files with clear-and-reload
- StatisticsCalculationService for derived metrics
- ImportSnapshotService for rollback capability
- MatchAnalyticsService for reporting queries

### Data Model

```sql
-- =============================================
-- REFERENCE DATA (Never cleared during import)
-- =============================================

teams (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    abbreviation VARCHAR(10),
    county VARCHAR(50),
    division VARCHAR(50),
    color_primary VARCHAR(7),
    color_secondary VARCHAR(7),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

seasons (
    id SERIAL PRIMARY KEY,
    year INTEGER NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    start_date DATE,
    end_date DATE,
    is_current BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW()
);

competitions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'League', 'Championship', 'Cup'
    season_id INTEGER NOT NULL REFERENCES seasons(id),
    created_at TIMESTAMP DEFAULT NOW()
);

-- Event types from KPI definitions (16 types)
event_types (
    id SERIAL PRIMARY KEY,
    code VARCHAR(10) NOT NULL UNIQUE, -- '1.0', '2.0', etc.
    name VARCHAR(100) NOT NULL, -- 'Kickout', 'Attacks', etc.
    category VARCHAR(50),
    description TEXT,
    default_psr_value INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Possible outcomes for each event type with PSR values
event_outcomes (
    id SERIAL PRIMARY KEY,
    event_type_id INTEGER NOT NULL REFERENCES event_types(id),
    outcome VARCHAR(100) NOT NULL, -- 'Won clean', 'Lost clean', etc.
    psr_value INTEGER NOT NULL, -- -3 to +3 range
    assign_to VARCHAR(20) NOT NULL, -- 'Home', 'Opposition'
    description TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(event_type_id, outcome)
);

-- =============================================
-- MATCH DATA (Cleared and reloaded on import)
-- =============================================

matches (
    id SERIAL PRIMARY KEY,
    competition_id INTEGER NOT NULL REFERENCES competitions(id),
    match_number INTEGER, -- Sequential match number from Excel
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
    excel_sheet_name VARCHAR(255), -- Track source sheet
    imported_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT unique_match_teams_date UNIQUE(match_date, home_team_id, away_team_id)
);

-- Main player statistics table (85+ columns)
match_player_stats (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    player_name VARCHAR(100) NOT NULL, -- Store name as-is from Excel
    jersey_number INTEGER,
    team_id INTEGER NOT NULL REFERENCES teams(id),
    minutes_played INTEGER DEFAULT 0,
    
    -- Core Performance Metrics
    total_events INTEGER DEFAULT 0,
    performance_success_rate DECIMAL(5,4), -- PSR calculation
    total_possessions INTEGER DEFAULT 0,
    turnovers_won INTEGER DEFAULT 0,
    interceptions INTEGER DEFAULT 0,
    possessions_lost INTEGER DEFAULT 0,
    
    -- Passing
    kick_passes INTEGER DEFAULT 0,
    hand_passes INTEGER DEFAULT 0,
    kick_pass_success_rate DECIMAL(5,4),
    hand_pass_success_rate DECIMAL(5,4),
    
    -- Defensive Actions  
    tackles_made INTEGER DEFAULT 0,
    tackles_missed INTEGER DEFAULT 0,
    tackle_success_rate DECIMAL(5,4),
    
    -- Disciplinary
    frees_won INTEGER DEFAULT 0,
    frees_conceded INTEGER DEFAULT 0,
    cards_yellow INTEGER DEFAULT 0,
    cards_black INTEGER DEFAULT 0,
    cards_red INTEGER DEFAULT 0,
    
    -- Scoring from Play
    points_from_play INTEGER DEFAULT 0,
    goals_from_play INTEGER DEFAULT 0,
    two_pointers_from_play INTEGER DEFAULT 0,
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
    
    -- Advanced Metrics
    shot_efficiency DECIMAL(5,4),
    score_conversion_rate DECIMAL(5,4),
    
    imported_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT unique_match_player UNIQUE(match_id, player_name, jersey_number)
);

-- Specialized Kickout Statistics
match_kickout_stats (
    id SERIAL PRIMARY KEY,
    match_player_stat_id INTEGER NOT NULL REFERENCES match_player_stats(id) ON DELETE CASCADE,
    kickouts_taken INTEGER DEFAULT 0,
    kickouts_won_clean INTEGER DEFAULT 0,
    kickouts_lost_clean INTEGER DEFAULT 0,
    kickouts_won_break INTEGER DEFAULT 0,
    kickouts_lost_break INTEGER DEFAULT 0,
    kickouts_won_short INTEGER DEFAULT 0,
    kickouts_lost_short INTEGER DEFAULT 0,
    kickouts_to_right INTEGER DEFAULT 0,
    kickouts_to_left INTEGER DEFAULT 0,
    kickout_retention_rate DECIMAL(5,4),
    saves INTEGER DEFAULT 0, -- Goalkeeper saves
    imported_at TIMESTAMP DEFAULT NOW()
);

-- Attack and Shot Source Analysis
match_source_analysis (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    team_id INTEGER NOT NULL REFERENCES teams(id),
    analysis_type VARCHAR(50) NOT NULL, -- 'attack_source', 'shot_source', 'score_source'
    source_category VARCHAR(100) NOT NULL, -- 'Kickout Long', 'Turnover', etc.
    total_count INTEGER DEFAULT 0,
    successful_count INTEGER DEFAULT 0,
    success_rate DECIMAL(5,4),
    imported_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT unique_match_team_analysis UNIQUE(match_id, team_id, analysis_type, source_category)
);

-- =============================================
-- IMPORT MANAGEMENT (For rollback capability)
-- =============================================

import_history (
    id SERIAL PRIMARY KEY,
    import_type VARCHAR(50) NOT NULL DEFAULT 'excel_full_reload',
    file_name VARCHAR(500),
    file_size BIGINT,
    matches_imported INTEGER DEFAULT 0,
    players_processed INTEGER DEFAULT 0,
    events_created INTEGER DEFAULT 0,
    import_started_at TIMESTAMP DEFAULT NOW(),
    import_completed_at TIMESTAMP,
    import_status VARCHAR(20) DEFAULT 'in_progress', -- 'completed', 'failed', 'rolled_back'
    error_message TEXT,
    snapshot_id INTEGER -- Reference to snapshot before import
);

-- Store snapshot data for rollback capability
import_snapshots (
    id SERIAL PRIMARY KEY,
    created_at TIMESTAMP DEFAULT NOW(),
    matches_data JSONB, -- Compressed snapshot of matches table
    player_stats_data JSONB, -- Compressed snapshot of critical data
    total_matches INTEGER,
    total_player_records INTEGER,
    snapshot_size_mb DECIMAL(8,2)
);
```

### API Design
```csharp
// Excel Import Endpoints
POST /api/import/excel
  Request: MultipartFormData with Excel file
  Response: { importId, matchesImported, playersProcessed, duration }

POST /api/import/rollback/{importId}
  Response: { success, restoredFromSnapshot, matchesRestored }

// Statistics Query Endpoints  
GET /api/statistics/season/{seasonId}/cumulative
GET /api/statistics/match/{matchId}/summary
GET /api/statistics/player/{playerName}/season/{seasonId}

// Analytics Endpoints
GET /api/analytics/top-scorers?season={seasonId}&position={position}&limit={limit}
GET /api/analytics/kickout-analysis/{matchId}
GET /api/analytics/team-comparison/{teamId}?opponent={opponentId}
```

### Integration Points
- Excel file parser using EPPlus/NPOI library
- PSR calculation engine based on KPI definitions
- Bulk insert optimization for performance
- Transaction management for rollback safety
- Background job processing for large imports

## Implementation Phases

### Phase 1: MVP (Database Foundation)
**Timeline: 2-3 days**

1. **Schema Creation**
   - Create migration V2.0 to drop existing tables
   - Create migration V3.0 with comprehensive new schema
   - Add proper indexes and constraints
   
2. **DAL Setup**
   - Remove existing DAL models
   - Scaffold new Entity Framework models
   - Create repository interfaces and implementations
   
3. **Basic Import Service**
   - Excel parsing with column mapping validation
   - Clear-and-reload transaction logic
   - Basic error handling and logging

**Acceptance Criteria:**
- All 8 existing matches import successfully
- No data loss during clear-and-reload process
- Database constraints prevent invalid data

### Phase 2: Enhancement (Import Robustness)
**Timeline: 2-3 days**

1. **Snapshot and Rollback System**
   - Implement pre-import snapshot creation
   - Build rollback mechanism for failed imports
   - Add import history tracking
   
2. **Advanced Excel Processing**
   - Handle all 31 sheet types correctly
   - Process cumulative statistics sheets
   - Validate data consistency across sheets
   
3. **Performance Optimization**
   - Bulk insert operations
   - Minimal logging during import
   - Connection pooling and transaction optimization

**Acceptance Criteria:**
- Import completes in under 30 seconds
- Failed imports automatically rollback
- All Excel sheet types processed correctly

### Phase 3: Analytics and Reporting
**Timeline: 2 days**

1. **Statistics Calculation Services**
   - Cumulative season statistics
   - Positional comparisons
   - Team performance metrics
   
2. **API Endpoints**
   - Query endpoints for all major statistics
   - Performance-optimized database queries
   - Caching for frequently accessed data

**Acceptance Criteria:**
- All major statistical queries respond under 2 seconds
- Cumulative calculations match Excel source
- API supports all planned analytics use cases

## Risk Assessment

### Technical Risks
1. **Data Loss During Clear-and-Reload**
   - *Risk Level: HIGH*
   - *Mitigation:* Mandatory snapshot before each import, tested rollback procedures
   
2. **Excel Format Changes Breaking Import**
   - *Risk Level: MEDIUM*
   - *Mitigation:* Robust column mapping validation, detailed format documentation
   
3. **Performance Issues with Large Datasets**
   - *Risk Level: MEDIUM*
   - *Mitigation:* Bulk operations, proper indexing, performance testing

4. **Import Failure Leaving Database in Inconsistent State**
   - *Risk Level: HIGH*
   - *Mitigation:* Transaction-wrapped imports, automatic rollback on failure

### Business Risks
1. **Extended Downtime During Schema Migration**
   - *Risk Level: LOW*
   - *Mitigation:* Quick migration scripts, staging environment testing
   
2. **Historical Data Loss**
   - *Risk Level: MEDIUM*
   - *Mitigation:* Complete backup before migration, validation scripts

## Testing Strategy

### Unit Testing
- Excel parser validation for all column types
- PSR calculation accuracy against known values
- Clear-and-reload transaction behavior
- Rollback mechanism functionality

### Integration Testing
- End-to-end Excel import with real data files
- Database constraint validation under load
- Performance benchmarks for 30-second import target
- Concurrent read access during import operations

### User Acceptance Testing
- Import all 8 existing matches successfully
- Verify statistics exactly match Excel calculations
- Validate cumulative season totals
- Test rollback recovery from simulated failures

## Recommended Agent Assignments

Based on the requirements, these specialized agents should handle implementation:

- **database-engineer**: Design and create Flyway migrations, optimize schema structure, implement indexing strategies for analytics performance
- **dotnet-developer**: Build comprehensive DAL models, create Excel import service with clear-and-reload logic, implement rollback mechanisms
- **dotnet-developer**: Develop statistics calculation services, create API endpoints for analytics queries

## Timeline Estimate

**Phase 1 (Database Foundation):** 2-3 days
- Day 1: Schema design, migrations creation
- Day 2: DAL model generation, basic repository setup  
- Day 3: Core Excel import service implementation

**Phase 2 (Import Robustness):** 2-3 days
- Day 1: Snapshot and rollback system
- Day 2: Advanced Excel processing for all sheet types
- Day 3: Performance optimization and bulk operations

**Phase 3 (Analytics):** 2 days  
- Day 1: Statistics calculation services
- Day 2: API endpoints and query optimization

**Total Estimate:** 6-8 days for complete implementation

## Open Questions

1. ✅ **Player Team Changes:** Confirmed - players never change teams, no historical tracking needed
2. ✅ **Import Strategy:** Confirmed - clear-and-reload approach for complete data refresh  
3. ✅ **Performance Requirements:** Target under 30 seconds for Excel import
4. ✅ **Real-time Data Entry:** Not required - Excel import only
5. ✅ **Rollback Strategy:** Snapshot-based rollback to last successful import
6. ✅ **Match Deletion:** Not applicable - only completed matches in Excel

## Success Criteria

The database rebuild will be considered successful when:

1. **Functional Requirements Met:**
   - All 85+ player statistics columns stored correctly
   - Complete clear-and-reload import process works reliably
   - Rollback mechanism tested and functional
   - All 16 event types with PSR calculations implemented

2. **Performance Requirements Met:**
   - Excel import completes in under 30 seconds
   - Analytics queries respond in under 2 seconds
   - Concurrent read access maintained during imports

3. **Data Integrity Maintained:**
   - Zero data loss during import process
   - All statistical calculations match Excel source
   - Database constraints prevent invalid data entry

4. **Operational Requirements Met:**
   - Import failures automatically trigger rollback
   - Import history tracked for audit purposes
   - System can handle multiple seasons of data growth