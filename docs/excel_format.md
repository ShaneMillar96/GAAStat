# GAA Statistics Excel File Format Reference

## Overview
This document provides a comprehensive reference for the Excel file format used to store GAA football match statistics for the Drum team. The Excel file serves as the single source of truth for all match data and contains detailed statistics across multiple sheets.

## File Structure

### File Naming Convention
- **Pattern:** `{Team Name} Analysis {Season Year}.xlsx`
- **Example:** `Drum Analysis 2025.xlsx`

### Sheet Organization
The Excel file contains **31 sheets** organized into the following categories:

#### 1. Match-Specific Sheets (16 sheets - 8 matches × 2 sheets each)
Each match has exactly 2 associated sheets:

**Match Statistics Sheets:**
- Format: `{MatchNumber}. {Competition} vs {Opponent} {Date}`
- Examples:
  - `08. Championship vs Magilligan 17.08.25`
  - `07. League Drum vs Lissan 03.08.25`
  - `01. Cup Drum vs Magilligan 04.05.25`

**Player Statistics Sheets:**
- Format: `{MatchNumber}. Player stats vs {Opponent} {Date}`  
- Examples:
  - `08. Player stats vs Magilligan`
  - `07. Player Stats vs Lissan 03.0`
  - `01. Player Stats vs Magilligan`

#### 2. Template Sheets (2 sheets)
- `Blank Team Stats` - Template for new match statistics
- `Blank Player Stats` - Template for new player statistics

#### 3. Data Processing Sheets (1 sheet)
- `CSV File` - Intermediate processing sheet

#### 4. Aggregate Analysis Sheets (12 sheets)
- `Cumulative Stats 2025` - Season-long cumulative statistics
- `Goalkeepers` - Goalkeeper-specific analysis
- `Defenders` - Defender position analysis  
- `Midfielders` - Midfielder position analysis
- `Forwards` - Forward position analysis
- `Player Matrix` - Cross-player comparison matrix
- `Kickout Analysis Data` - Raw kickout event data
- `Kickout Stats` - Processed kickout statistics
- `Shots from play Data` - Raw shot attempt data
- `Shots from Play Stats` - Processed shooting statistics
- `Scoreable Frees Data` - Raw free kick data
- `Scoreable Free Stats` - Processed free kick statistics
- `KPI Definitions` - Event types and PSR value definitions

## Match Statistics Sheet Format

### Sheet Structure
- **Dimensions:** 235 rows × 18 columns
- **Header Row:** Row 1 contains match information
- **Data Rows:** Rows 2-235 contain match events and statistics

### Column Layout
| Column | Name | Content |
|--------|------|---------|
| A | Event Description | Event names, scoreline updates |
| B | Match Title | Match identifier and details |
| C-P | Event Data | Unnamed columns containing event statistics |
| Q | Drum | Home team (1.0/0.0) indicator |
| R | Opposition | Away team (1.0/0.0) indicator |

### Key Data Points
- **Scoreline:** Found in early rows (format: "2-06", "1-05", etc.)
- **Match Events:** Detailed timeline of match occurrences
- **Team Performance:** Binary indicators for home/away success

## Player Statistics Sheet Format

### Sheet Structure  
- **Dimensions:** 21-23 rows × 85 columns
- **Header Rows:** 
  - Row 1: Sheet identifier ("Drum", "Summary")
  - Row 2: Column headers with statistic abbreviations
- **Data Rows:** Rows 3+ contain individual player statistics

### Player Data Columns

#### Core Player Information
| Column | Header | Description | Data Type |
|--------|---------|-------------|-----------|
| A | # | Jersey number | Integer |
| B | Player Name | Full player name | String |
| C | Min | Minutes played | Integer |

#### Performance Metrics  
| Column | Header | Description | Range | Notes |
|--------|---------|-------------|-------|--------|
| D | TE | Total Events | 0-50+ | Count of all player actions |
| E | TE/PSR | Events per PSR | 0.0-1.0 | Efficiency ratio |
| F | Scores | Total scores | String | Format: "0-01", "1-02" |
| G | PSR | Performance Success Rate | 0-50+ | Sum of PSR values |
| H | PSR/TP | PSR per Total Possessions | 0.0-3.0 | Efficiency metric |
| I | TP | Total Possessions | 0-30+ | Ball possession count |

#### Ball Handling & Possession
| Column | Header | Description |
|--------|---------|-------------|
| J | ToW | Turnovers Won |
| K | Int | Interceptions |  
| L | TPL | Total Possessions Lost |
| M | KP | Kick Passes |
| N | HP | Hand Passes |
| O | Ha | Handling errors |

#### Defensive Actions
| Column | Header | Description |
|--------|---------|-------------|
| P | TO | Turnovers |
| Q | In | Interceptions |
| R | SS | Successful tackles |
| S | S Save | Saves (goalkeeper) |

#### Scoring Statistics - From Play
| Columns | Headers | Description |
|---------|---------|-------------|
| T-AB | Various | Shots from play outcomes |
| T | Fo | Fouls |
| U | Ww | Wides |
| V-W | KoW | Kickout wins |
| X-Z | WC, BW, SW | Win categories |
| AA-AB | TA | Total attempts |

#### Scoring Statistics - From Frees  
| Columns | Headers | Description |
|---------|---------|-------------|
| AC-AK | Various | Free kick outcomes |
| AC | KR | Kick retained |
| AD | KL | Kick lost |
| AE | CR | ? |
| AF | CL | ? |
| AG | Tot | Totals |

#### Specialized Statistics
| Columns | Headers | Description |
|---------|---------|-------------|
| AH-AM | Pts, 2 Pts, Gls | Points, 2-pointers, Goals |
| AN-AS | Shot outcomes | Wide, Save, Short, etc. |
| AT-AY | Free kick stats | Conversion rates |
| AZ-BC | Advanced metrics | Efficiency percentages |

#### Disciplinary & Cards
| Columns | Headers | Description |
|---------|---------|-------------|
| BD-BI | Card types | Yellow, Black, Red cards |
| BD | Yel | Yellow cards |
| BE | Bla | Black cards |  
| BF | Red | Red cards |

#### Kickout Analysis (Goalkeepers)
| Columns | Headers | Description |
|---------|---------|-------------|
| BJ-BM | Kickout stats | Won, Lost, Retention |
| BJ | Won | Kickouts won |
| BK | Los | Kickouts lost |
| BL | TKo | Total kickouts |
| BM | KoR | Kickouts right |
| BN | KoL | Kickouts left |
| BO | % | Retention percentage |
| BP | Saves | Goalkeeper saves |

## Event Types and PSR Values

### KPI Definitions Reference
Based on the `KPI Definitions` sheet, there are **16 main event types**:

| Code | Event Name | PSR Range | Description |
|------|------------|-----------|-------------|
| 1.0 | Kickout | -1 to +1 | Kickout outcomes (won/lost) |
| 2.0 | Attacks | 0 to +1 | Attack possession outcomes |
| 3.0 | Shot from play | -1 to +3 | Shooting outcomes and scoring |
| 4.0 | Scoreable free | -1 to +3 | Free kick scoring attempts |
| 5.0 | Score source | 0 | Origin tracking for scores |
| 6.0 | Tackle | -1 to +1 | Defensive tackle success |
| 7.0 | Free conceded | -2 to 0 | Foul locations and severity |
| 8.0 | Possession lost | -1 | Various turnover types |
| 9.0 | Bookings | -1 to -3 | Card penalties |
| 10.0 | Possessions | +1 | Ball possession events |
| 11.0 | Ball Won | +2 | Defensive ball recovery |
| 12.0 | Score assist | +1 to +2 | Assist for scores |
| 13.0 | Goalkeepers | -1 to +3 | GK-specific actions |
| 14.0 | Attack source | 0 | Origin tracking for attacks |
| 15.0 | Shot source | 0 | Origin tracking for shots |
| 16.0 | 50m Free Conceded | -2 to 0 | Serious foul penalties |

### PSR Value Assignments

#### Positive Values (+1 to +3)
- **+3:** Goals scored, goalkeeper saves
- **+2:** 2-point scores, ball won via tackle/interception, goal assists  
- **+1:** Points scored, successful tackles, possessions, score assists

#### Negative Values (-1 to -3)
- **-3:** Red cards
- **-2:** Black cards, penalties, serious fouls
- **-1:** Missed tackles, possession lost, yellow cards, short shots

#### Neutral Values (0)
- **0:** Tracking events (sources), wide shots, saves, quick frees

## Data Processing Guidelines

### Import Processing Order
1. **Reference Data:** Load teams, competitions, seasons first
2. **Match Data:** Process match statistics sheets for basic match info
3. **Player Stats:** Process detailed player statistics for each match
4. **Validation:** Cross-reference totals and consistency checks

### Data Type Mappings
- **Integer Fields:** Minutes, jersey numbers, counts, card totals
- **Decimal Fields:** Percentages, rates, efficiency metrics
- **String Fields:** Player names, score formats ("2-06"), positions
- **Date Fields:** Match dates (parse from sheet names)
- **Boolean Fields:** Binary success indicators (1.0/0.0)

### Special Handling Required

#### Score Format Parsing
- **Format:** "Goals-Points" (e.g., "2-06" = 2 goals, 6 points)
- **Handling:** Split on hyphen, validate numeric values

#### Percentage Values
- **Format:** Decimal (0.75) or percentage string ("75%")
- **Handling:** Normalize to decimal format for database storage

#### Missing Data
- **Empty Cells:** Treat as NULL for optional fields, 0 for count fields
- **"NaN" Values:** Convert to NULL in database
- **Template Sheets:** Skip during processing (contain placeholder data)

### Validation Rules

#### Player Statistics
- Minutes played should not exceed match duration (typically 60-70 minutes)
- PSR values should align with defined ranges for event types
- Total possessions should be reasonable relative to minutes played
- Card counts should be 0 or positive integers only

#### Match Statistics  
- Home and away scores should match calculated totals from player stats
- Match dates should be valid and chronologically ordered
- Team names should be consistent across all matches

#### Cross-Sheet Consistency
- Player names should be consistent across all matches
- Jersey numbers should remain constant for each player
- Match results should align between team and player statistics sheets

## Common Data Patterns

### Player Naming Conventions
- **Format:** "FirstName LastName" or "FirstName MiddleInitial LastName"
- **Examples:** "Cahair O Kane", "Seamus O Kane", "Michael Farren"
- **Special Cases:** Some names include apostrophes or hyphens

### Match Identification
- **Sheet Names:** Follow sequential numbering (01, 02, 03...)
- **Competition Types:** League, Championship, Cup, Neal Carlin (specific tournament)
- **Date Formats:** Various formats in sheet names (DD.MM.YY, DD.MM.YYYY)

### Statistical Ranges
- **High Performers:** 20-40+ total events, 15-30+ possessions
- **Average Players:** 10-25 total events, 8-20 possessions  
- **Limited Time:** <30 minutes typically shows proportionally lower stats

## Import Implementation Notes

### Performance Considerations
- **Bulk Processing:** Process all player stats for a match in single transaction
- **Memory Usage:** Load one sheet at a time to minimize memory footprint
- **Validation Caching:** Cache player name mappings to avoid repeated lookups

### Error Handling
- **Sheet Missing:** Log warning and continue with other matches
- **Data Format Issues:** Log specific cell/value errors with context
- **Constraint Violations:** Rollback transaction and provide detailed error info

### Logging Requirements
- **Match Processing:** Log each match sheet processed with row/player counts
- **Validation Failures:** Log specific validation failures with sheet/cell references
- **Performance Metrics:** Track processing time per sheet and total import duration

This comprehensive format reference serves as the foundation for implementing the Excel import functionality and ensures accurate data processing across all statistical categories.