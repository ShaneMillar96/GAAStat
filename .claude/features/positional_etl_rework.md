# Feature: Positional ETL Processing Rework

## Executive Summary

Rework the GAAStat ETL process to replace numbered player statistics sheet processing with positional sheet processing. This change will enable more structured player position tracking and support seasonal analysis by correlating match data with player positions across four positional sheets: Goalkeepers, Defenders, Midfielders, and Forwards.

The new system will process match data from positional sheets where each column represents a chronological match (with Home/Away designation), enabling correlation with numbered match sheets (01-99) for comprehensive statistical tracking.

## Problem Statement

The current ETL process relies on numbered player statistics sheets (e.g., "07. Player Stats vs Team") which are processed independently. The new requirement is to:

1. **Replace numbered player stats processing** with positional sheet processing
2. **Correlate matches chronologically** using sequential column positions in positional sheets
3. **Support Home/Away designation** with `(H)` and `(A)` patterns in column headers
4. **Link player positions** to match statistics for enhanced analytics
5. **Maintain data integrity** during the transition from the old to new processing approach

## Objectives & Success Metrics

### Primary Objectives
- Successfully process positional sheets (Goalkeepers, Defenders, Midfielders, Forwards)
- Correlate match columns with numbered sheets using chronological order
- Identify active players from red cells with text content
- Extend `match_player_statistics` to include position information
- Maintain existing ETL performance and error handling standards

### Success Metrics
- 100% successful processing of positional sheets with active player identification
- Accurate match correlation between numbered sheets and positional sheet columns
- Successful linking of player positions to match statistics
- No data loss during transition from numbered to positional sheet processing
- Maintained ETL processing performance (≤30 minutes for typical files)

## Functional Requirements

### Core Functionality

#### 1. Positional Sheet Processing
- **Sheet Recognition**: Identify sheets named "Goalkeepers", "Defenders", "Midfielders", "Forwards"
- **Player Block Detection**: Process 11-row player blocks starting with red cells containing player names
- **Match Row Processing**: Process rows 2-9 within each player block representing chronological matches
- **Statistics Extraction**: Extract statistics from columns C-CH (82 statistical columns)
- **Home/Away Pattern Recognition**: Parse `TeamName (H)` and `TeamName (A)` patterns from column B

#### 2. Match Correlation Logic
- **Player Block Structure**: Each player occupies 11 rows (name + 8 matches + avg + totals)
- **Sequential Row Mapping**: Map player block rows to numbered sheets chronologically
  - Row 2 (first match) ↔ Sheet "01"
  - Row 3 (second match) ↔ Sheet "02"
  - Rows 4-9 ↔ Sheets "03"-"08"
- **Statistical Column Range**: Process columns C through CH for comprehensive match statistics
- **Cross-Reference Validation**: Ensure match data consistency between positional blocks and numbered sheets

#### 3. Position Integration
- **Position Linking**: Associate player statistics with position categories
- **Position Tracking**: Enable position-based analytics and reporting
- **Historical Position Data**: Maintain position associations across multiple seasons

### User Stories

#### As a Data Analyst
- **Story**: I want to analyze player performance by position across matches
- **Acceptance Criteria**: 
  - Positional sheets are successfully processed
  - Player statistics include position information
  - Position-based queries return accurate data

#### As a System Administrator
- **Story**: I want to ensure the ETL process handles both old and new sheet formats
- **Acceptance Criteria**:
  - System gracefully handles files with mixed sheet formats
  - Clear error messaging for invalid sheet structures
  - Comprehensive logging for troubleshooting

#### As a Coach
- **Story**: I want to track player position consistency across matches
- **Acceptance Criteria**:
  - Players can be tracked across multiple positions
  - Position changes are accurately recorded
  - Historical position data is preserved

## Technical Architecture

### Components

#### Modified Components
1. **ExcelProcessingService**: Enhanced to handle positional sheet correlation
2. **ExcelParsingService**: Extended with positional sheet parsing capabilities
3. **PlayerStatisticsMapper**: Updated to include position context
4. **EtlConstants**: New constants for positional sheet patterns
5. **GAAStatDbContext**: Enhanced with position relationship queries

#### New Components
1. **PositionalSheetProcessor**: Dedicated service for positional sheet handling
2. **MatchCorrelationService**: Handles chronological match mapping
3. **PositionPlayerMappingService**: Manages player-position associations

### Data Model

#### Database Schema Changes

##### New Tables
```sql
-- Enhanced player-position tracking for match-specific positions
CREATE TABLE match_player_positions (
    match_player_position_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL,
    player_id INTEGER NOT NULL,
    position_id INTEGER NOT NULL,
    is_starting_position BOOLEAN DEFAULT TRUE,
    minutes_in_position INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id),
    FOREIGN KEY (position_id) REFERENCES positions(position_id),
    UNIQUE(match_id, player_id, position_id)
);

-- Positional sheet metadata tracking
CREATE TABLE positional_sheet_metadata (
    positional_sheet_id SERIAL PRIMARY KEY,
    etl_job_id INTEGER NOT NULL,
    position_id INTEGER NOT NULL,
    sheet_name VARCHAR(100) NOT NULL,
    total_matches_processed INTEGER DEFAULT 0,
    total_players_processed INTEGER DEFAULT 0,
    processing_status VARCHAR(20) DEFAULT 'pending',
    error_message TEXT,
    processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (etl_job_id) REFERENCES etl_jobs(etl_job_id) ON DELETE CASCADE,
    FOREIGN KEY (position_id) REFERENCES positions(position_id)
);

-- Match column correlation tracking
CREATE TABLE match_column_correlations (
    correlation_id SERIAL PRIMARY KEY,
    etl_job_id INTEGER NOT NULL,
    numbered_sheet_name VARCHAR(100) NOT NULL,
    match_id INTEGER NOT NULL,
    positional_column_index INTEGER NOT NULL,
    team_name VARCHAR(100),
    is_home_match BOOLEAN,
    match_date DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (etl_job_id) REFERENCES etl_jobs(etl_job_id) ON DELETE CASCADE,
    FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
);
```

##### Modified Tables
```sql
-- Add position context to match_player_statistics
ALTER TABLE match_player_statistics 
ADD COLUMN recorded_position_id INTEGER,
ADD CONSTRAINT fk_recorded_position 
    FOREIGN KEY (recorded_position_id) REFERENCES positions(position_id);

COMMENT ON COLUMN match_player_statistics.recorded_position_id 
IS 'Position the player was recorded in for this specific match';
```

#### Position Reference Data
```sql
-- Ensure standard GAA positions exist
INSERT INTO positions (position_name, position_category, description) VALUES 
('Goalkeeper', 'Goalkeepers', 'Primary goalkeeper position'),
('Full Back', 'Defenders', 'Defensive back line position'),
('Half Back', 'Defenders', 'Defensive half line position'),
('Midfielder', 'Midfielders', 'Central midfield position'),
('Half Forward', 'Forwards', 'Attacking half line position'),
('Full Forward', 'Forwards', 'Attacking front line position')
ON CONFLICT (position_name) DO NOTHING;
```

### API Design

#### New Service Interfaces

```csharp
public interface IPositionalSheetProcessingService
{
    Task<ServiceResult<PositionalProcessingResult>> ProcessPositionalSheetsAsync(
        int jobId, 
        ExcelPackage package, 
        Dictionary<string, int> matchIdMap,
        CancellationToken cancellationToken = default);
        
    Task<ServiceResult<MatchColumnCorrelation[]>> CorrelateMatchColumnsAsync(
        ExcelWorksheet positionalSheet, 
        Dictionary<string, int> matchIdMap);
        
    Task<ServiceResult<PlayerPositionData[]>> ExtractPlayerPositionDataAsync(
        ExcelWorksheet worksheet, 
        int positionId, 
        MatchColumnCorrelation[] correlations);
}

public interface IMatchCorrelationService
{
    Task<ServiceResult<MatchCorrelationResult>> CorrelatePositionalToNumberedSheetsAsync(
        Dictionary<string, ExcelWorksheet> positionalSheets,
        Dictionary<string, int> matchIdMap);
        
    MatchColumnCorrelation CreateCorrelation(
        string numberedSheetName, 
        int matchId, 
        int columnIndex, 
        string teamPattern,
        bool isHomeMatch);
}
```

### Integration Points

#### External Systems
- **Excel Processing**: EPPlus library for worksheet manipulation
- **Database**: PostgreSQL via Entity Framework Core
- **Logging**: Microsoft.Extensions.Logging for comprehensive tracking

#### Internal Dependencies
- **Existing ETL Pipeline**: Maintains compatibility with current job tracking
- **Reference Data Services**: Leverages existing position and team data
- **Validation Services**: Extends current validation patterns

### Technology Research

Based on the existing codebase analysis:

#### EPPlus Integration Patterns
- **Current Usage**: Successfully implemented for numbered sheet processing
- **Extension Required**: Add support for cell color detection and player block parsing
- **Performance Considerations**: Maintain single ExcelPackage instance to prevent stream exhaustion
- **Player Block Detection Pattern**: 
```csharp
// Detect red cells for player name identification
var fill = worksheet.Cells[row, col].Style.Fill;
bool isRedCell = fill.BackgroundColor.Rgb == "FFFF0000" || 
                 fill.BackgroundColor.Theme != null;

// Player block structure (11 rows per player)
// Row 1: Player name (red cell)
// Rows 2-9: Match data (8 matches)
// Row 10: "Avg. per game"
// Row 11: "Player Totals"
```

#### Statistical Column Processing
- **Column Range**: Process columns C through CH (approximately 82 statistical columns)
- **Column Categories**: Summary, Possession Play, Kickout Analysis, Opposition Kickout Analysis
- **Data Extraction**: Extract statistics for each match row within player blocks
- **Pattern**: 
```csharp
// Extract statistics for match row within player block
for (int col = 3; col <= lastColumn; col++) // C to CH
{
    var statValue = worksheet.Cells[matchRow, col].Value?.ToString();
    // Process individual statistic
}
```

#### Entity Framework Core Patterns
- **Context Scoping**: Use scoped contexts for each positional sheet to prevent state corruption
- **Bulk Operations**: Leverage existing batch processing patterns (100-record batches)
- **Foreign Key Relationships**: Extend existing player-match relationships with position context

#### Error Handling Integration
- **Existing Patterns**: Leverage established ETL error tracking via `etl_validation_errors`
- **Timeout Handling**: Implement per-sheet timeouts (2 minutes) with continuation logic
- **Validation Pipeline**: Extend existing validation with positional-specific checks

## Implementation Phases

### Phase 1: Database Schema and Infrastructure (Week 1)
- **Database Migration**: Create new tables for positional sheet tracking
- **Reference Data**: Seed position data with GAA-standard positions
- **Model Generation**: Scaffold new EF Core models
- **Service Interfaces**: Define new service contracts

**Deliverables:**
- Migration V8__Add_positional_sheet_support.sql
- Updated EF Core models
- New service interface definitions
- Unit test foundations

### Phase 2: Core Positional Processing (Week 2)
- **Sheet Recognition**: Implement positional sheet identification logic
- **Column Correlation**: Build match column to numbered sheet mapping
- **Red Cell Detection**: Implement active player identification from cell colors
- **Position Integration**: Link player statistics to position data

**Deliverables:**
- PositionalSheetProcessingService implementation
- MatchCorrelationService implementation
- Enhanced ExcelParsingService with positional support
- Integration tests for core functionality

### Phase 3: ETL Pipeline Integration (Week 3)
- **Workflow Integration**: Embed positional processing in main ETL pipeline
- **Error Handling**: Implement comprehensive error tracking and recovery
- **Performance Optimization**: Ensure processing times remain under 30 minutes
- **Backward Compatibility**: Support mixed sheet format files during transition

**Deliverables:**
- Updated ExcelProcessingService with positional workflow
- Enhanced error tracking and validation
- Performance benchmarks and optimizations
- Comprehensive integration tests

### Phase 4: Testing and Validation (Week 4)
- **End-to-End Testing**: Full ETL pipeline testing with positional sheets
- **Data Validation**: Verify accuracy of player-position correlations
- **Performance Testing**: Confirm processing time requirements
- **Documentation**: Update technical documentation and deployment guides

**Deliverables:**
- Complete test suite covering all scenarios
- Performance validation reports
- Updated documentation
- Deployment and rollback procedures

## Risk Assessment

### High-Risk Items

#### 1. Data Correlation Accuracy
- **Risk**: Incorrect mapping between positional columns and numbered sheets
- **Impact**: High - Corrupted statistical data
- **Mitigation**: 
  - Implement comprehensive correlation validation
  - Create data integrity checks
  - Maintain audit trails for all correlations

#### 2. Performance Degradation
- **Risk**: New processing requirements slow down ETL pipeline
- **Impact**: Medium - Exceeds 30-minute processing limit
- **Mitigation**:
  - Optimize database queries with appropriate indexes
  - Implement parallel processing where safe
  - Monitor and profile critical path performance

#### 3. Backward Compatibility
- **Risk**: Breaking existing functionality for current sheet formats
- **Impact**: High - Disrupts existing workflows
- **Mitigation**:
  - Maintain parallel processing paths
  - Implement feature flags for gradual rollout
  - Comprehensive regression testing

### Medium-Risk Items

#### 1. Red Cell Detection Reliability
- **Risk**: Inconsistent identification of active players
- **Impact**: Medium - Incomplete player data processing
- **Mitigation**:
  - Implement multiple detection strategies
  - Add manual validation capabilities
  - Create clear error reporting for failed detections

#### 2. Memory Usage with Large Files
- **Risk**: Increased memory consumption with complex sheet processing
- **Impact**: Medium - Potential system instability
- **Mitigation**:
  - Implement streaming processing patterns
  - Monitor memory usage in production
  - Add memory usage alerts

## Testing Strategy

### Unit Testing
- **Coverage Target**: >90% for all new components
- **Key Areas**: 
  - Positional sheet recognition logic
  - Match correlation algorithms
  - Red cell detection mechanisms
  - Position-player mapping functions

### Integration Testing
- **Database Integration**: Test all new entity relationships and constraints
- **Service Integration**: Verify service layer interactions and data flow
- **ETL Pipeline**: End-to-end pipeline testing with sample files

### Performance Testing
- **Load Testing**: Process multiple large files simultaneously
- **Memory Testing**: Monitor memory usage patterns with various file sizes
- **Timeout Testing**: Verify timeout handling and recovery mechanisms

### User Acceptance Testing
- **Data Accuracy**: Verify statistical correlation accuracy
- **Error Handling**: Test error scenarios and recovery procedures
- **Usability**: Ensure clear error messages and processing feedback

## Recommended Agent Assignments

Based on the technical requirements and existing codebase patterns:

- **database-agent**: Handle all database schema changes, migrations, and model scaffolding
- **backend-agent**: Implement service layer modifications and core processing logic
- **etl-agent**: Focus on ETL pipeline integration and Excel processing enhancements
- **test-agent**: Develop comprehensive test suites and validation procedures
- **integration-agent**: Handle service integration and end-to-end workflow testing

## Timeline Estimate

### Development: 4 Weeks
- **Week 1**: Database foundation and infrastructure (32 hours)
- **Week 2**: Core positional processing implementation (40 hours)
- **Week 3**: ETL pipeline integration and optimization (40 hours)
- **Week 4**: Testing, validation, and documentation (32 hours)

### Testing and Deployment: 1 Week
- **Integration Testing**: 16 hours
- **Performance Validation**: 8 hours
- **Documentation and Deployment**: 8 hours

**Total Estimate**: 176 hours over 5 weeks

## Open Questions

### Technical Clarifications Needed
1. **Red Cell Standards**: Are there specific RGB values or Excel formatting standards for identifying "red cells"?
2. **Position Granularity**: Should we support sub-positions within the four main categories?
3. **Data Migration**: How should we handle existing player statistics during the transition?
4. **Match Ordering**: Should we validate chronological ordering of matches in positional sheets?

### Business Requirements
1. **Rollback Strategy**: What's the preferred approach if issues are discovered post-deployment?
2. **Training Requirements**: Do end users need training on the new positional sheet formats?
3. **Data Validation**: What level of manual validation is expected during the transition period?

### Performance Considerations
1. **File Size Limits**: Are there maximum file size constraints for positional sheet processing?
2. **Concurrent Processing**: Should we support processing multiple positional files simultaneously?
3. **Caching Strategy**: Would position-match correlations benefit from caching mechanisms?