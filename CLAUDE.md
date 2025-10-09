# GAAStat Project Context for Claude

## Project Overview

GAAStat is a comprehensive GAA (Gaelic Athletic Association) statistics tracking and analysis application. The project aims to digitize and analyze match statistics from Excel spreadsheets, storing them in a relational database for querying, reporting, and insights.

**Primary Data Source:** `Drum Analysis 2025.xlsx` - A detailed Excel workbook containing match and player statistics for the Drum GAA team's 2025 season.

---

## Excel File Structure

### Overview

The Excel file (`Drum Analysis 2025.xlsx`) contains 34 sheets organized into several categories:

1. **Match Sheets** - Team-level match statistics
2. **Player Stats Sheets** - Individual player performance metrics
3. **Aggregate Sheets** - Season-level cumulative statistics
4. **Position Sheets** - Position-specific analysis
5. **Analysis Sheets** - Specialized analysis (kickouts, shots, scoreable frees)
6. **Definition Sheets** - KPI and metric definitions

---

### 1. Match Sheets

**Naming Convention:** `[number]. [Competition] vs [Opposition] [Date]`

**Examples:**
- `09. Championship vs Slaughtmanus 26.09.25`
- `08. Championship vs Magilligan 17.08.25`
- `07. League Drum vs Lissan 03.08.25`

**Structure:**
- **Dimensions:** Up to 1054 rows × 42 columns (A1:AP1054)
- **Data Breakdown:** Statistics split by period (1st half, 2nd half, Full time)
- **Teams:** Two columns - Home team (Drum) and Opposition

**Key Metrics Tracked:**

```
Row 1:  Match title and metadata
Row 2:  Team names
Row 3:  Period headers (1st, 2nd, Full)
Row 4:  Scoreline (e.g., "0-04", "1-07", "1-11")
Row 5:  Total Possession (decimal percentages)
Row 6+: Score source breakdown:
        - Kickout Long/Short
        - Opposition Kickout Long/Short
        - Turnover
        - Possession lost
        - Shot Short
        - Throw Up/In
```

**Column Structure:**
- Columns A-G: Team statistics (3 periods each for Drum and Opposition)
- Columns Q-R: Additional metrics (Drum vs Opposition tallies)

---

### 2. Player Stats Sheets

**Naming Convention:** `[number]. Player Stats vs [Opposition] [Date]`

**Examples:**
- `09. Player stats vs Slaughtmanus 26.09.25`
- `08. Player stats vs Magilligan 17.08.25`

**Structure:**
- **Dimensions:** Up to 1005 rows × 86 columns (A1:CH1005)
- **Headers:** 3 rows of nested headers
  - Row 1: Numeric weights/values
  - Row 2: Category headers
  - Row 3: Field abbreviations

**86 Column Categories:**

#### Summary Statistics (Columns A-H)
- `#` - Jersey number
- `Player Name` - Full name
- `Min` - Minutes played
- `TE` - Total Engagements
- `TE/PSR` - Total Engagements per PSR
- `Scores` - Score notation (e.g., "1-03(1f)" = 1 goal, 3 points, 1 free)
- `PSR` - Possession Success Rate
- `PSR/TP` - PSR per Total Possessions

#### Possession Play (Columns I-V)
- `TP` - Total Possessions
- `ToW` - Turnover Won
- `Int` - Interceptions
- `TPL` - Turnover Possession Lost
- `KP` - Kick Pass
- `HP` - Hand Pass
- `Ha` - Hand Pass Attempted
- `TO` - Turnover
- `In` - Ineffective
- `SS` - Shot Short
- `S Save` - Shot Save
- `Fo` - Fouled
- `Ww` - Woodwork

#### Kickout Analysis (Columns W-AB)
**Drum Kickouts:**
- `KoW` - Kickout Won
- `WC` - Won Clean
- `BW` - Break Won
- `SW` - Short Won

**Opposition Kickouts:**
- Same fields for opposition kickout analysis

#### Attacking Play (Columns AC-AH)
- `TA` - Total Attacks
- `KR` - Kick Retained
- `KL` - Kick Lost
- `CR` - Carry Retained
- `CL` - Carry Lost

#### Shots from Play (Columns AI-AT)
- `Tot` - Total shots
- `Pts` - Points scored
- `2 Pts` - 2-point scores (outside 40m arc)
- `Gls` - Goals
- `Wid` - Wide
- `Sh` - Short
- `Save` - Saved by goalkeeper
- `Ww` - Woodwork
- `Bd` - Blocked
- `45` - 45-yard free awarded
- `%` - Shooting percentage

#### Scoreable Frees (Columns AU-BF)
- Same structure as shots from play
- `QF` - Quick Free
- `%` - Free-taking percentage

#### Total Shots (Columns BG-BH)
- `TS` - Total Shots
- `%` - Overall shooting percentage

#### Assists (Columns BI-BK)
- `TA` - Total Assists
- `Point` - Point assists
- `Goal` - Goal assists

#### Tackles (Columns BL-BO)
- `Tot` - Total tackles
- `Con` - Contested
- `Mis` - Missed
- `%` - Tackle success rate

#### Frees Conceded (Columns BP-BT)
- `Tot` - Total frees conceded
- `Att` - In attacking third
- `Mid` - In midfield
- `Def` - In defensive third
- `Pen` - Penalty conceded

#### 50m Free Conceded (Columns BU-BX)
- `Tot` - Total 50m frees
- `Delay` - For delaying the game
- `Diss` - For dissent
- `3v3` - For 3v3 infringement

#### Bookings (Columns BY-CA)
- `Yel` - Yellow cards
- `Bla` - Black cards
- `Red` - Red cards

#### Throw Up (Columns CB-CC)
- `Won` - Throw-ups won
- `Los` - Throw-ups lost

#### Goalkeeper Stats (Columns CD-CH)
- `TKo` - Total Kickouts
- `KoR` - Kickout Retained
- `KoL` - Kickout Lost
- `%` - Kickout retention percentage
- `Saves` - Saves made

---

### 3. Aggregate Sheets

#### Cumulative Stats 2025

**Structure:**
- **Dimensions:** 1000 rows × 154 columns (A1:EX1000)
- **Purpose:** Season-level aggregations across all matches
- **Layout:** One row per match with cumulative totals

**Key Sections:**
1. Score summary (Drum vs Opposition)
2. Possession play totals
3. Kickout analysis (Drum and Opposition)
4. Attacking play metrics
5. Shots and scoreable frees
6. Defensive statistics
7. Score breakdown per 10-minute intervals
8. Game averages and totals (bottom rows)

**Special Rows:**
- Row with "Game Average" - Average statistics across all matches
- Row with "Totals" - Sum totals for the season

---

### 4. Position-Specific Sheets

Four sheets providing filtered views by position:

1. **Goalkeepers** - GK-specific metrics
2. **Defenders** - Defensive player statistics
3. **Midfielders** - Midfielder performance
4. **Forwards** - Forward/attacking player stats

Each sheet contains the same 86-column structure as player stats sheets, filtered by position.

---

### 5. Analysis Sheets

#### Player Matrix
- Cross-reference of players and their key statistics
- Used for quick player comparisons

#### Kickout Analysis Data
- Raw data for kickout performance
- Tracks both Drum and opposition kickout outcomes

#### Kickout Stats
- Aggregated kickout performance metrics
- Success rates by type (long/short/clean/break)

#### Shots from Play Data
- Detailed shot-by-shot data
- Includes location, outcome, and context

#### Shots from Play Stats
- Aggregated shooting statistics
- Conversion rates and shot quality metrics

#### Scoreable Frees Data
- Raw data for all scoreable free kicks
- Distance, outcome, player

#### Scoreable Free Stats
- Aggregated free-taking performance
- Success rates by distance and type

---

### 6. Definition Sheets

#### KPI Definitions

**Structure:**
- Columns: Event Name, Outcome, Assign to which team, PSR Value, Definition
- Rows: Individual event definitions

**Example Entries:**

| Event # | Event Name | Outcome | Team Assignment | PSR Value | Definition |
|---------|------------|---------|-----------------|-----------|------------|
| 1 | Kickout | Won clean | Home | 1.0 | A kickout won clean in the air by a home player |
| 1 | Kickout | Lost clean | Opposition | 1.0 | A kickout won clean in the air by an opposition player |
| 1 | Kickout | Break won | Home | 1.0 | A kickout breaking ball won by a home player |
| 2 | Attacks | Kick retained | Home | 1.0 | A player who wins the ball in the forward line |
| 3 | Shot from play | Point | Home | 1.0 | A shot over the bar |
| 3 | Shot from play | 2 Pointer | Home | 2.0 | A shot over the bar outside the 40m arc |
| 3 | Shot from play | Goal | Home | 3.0 | A shot underneath the bar for a goal |
| 4 | Scoreable free | Point | Home | 1.0 | A scoreable free that is put over the bar |

**Purpose:** Provides standardized definitions for all tracked events and their PSR (Possession Success Rate) values.

---

### 7. Template Sheets

#### Blank Team Stats
- Empty template for match team statistics
- Used for data entry consistency

#### Blank Player Stats
- Empty template for player statistics
- Ensures uniform data structure

---

## Database Schema Mapping

The database schema is designed to directly mirror the Excel file structure, enabling seamless ETL (Extract, Transform, Load) operations.

### Schema Overview

```
┌──────────────────┐
│     seasons      │
│   (2025, etc.)   │
└────────┬─────────┘
         │
         ├──→ competitions (Championship, League)
         │
         └──→ matches ──┬──→ match_team_statistics (3 periods × 2 teams)
                        │
                        └──→ player_match_statistics (86 fields per player)
                             ↑
                             │
                        players (linked to positions)
```

---

### Table-to-Excel Mapping

#### 1. `seasons` Table
**Purpose:** Track different seasons of competition

**Excel Mapping:**
- Derived from match dates in sheet names
- Example: "26.09.25" → Season 2025

**Fields:**
- `season_id` - Primary key
- `year` - 2025
- `name` - "2025 Season"
- `is_current` - TRUE for active season

---

#### 2. `competitions` Table
**Purpose:** Track competition types within a season

**Excel Mapping:**
- Extracted from match sheet names
- "09. **Championship** vs Slaughtmanus" → Competition type
- "07. **League** Drum vs Lissan" → Competition type

**Fields:**
- `competition_id` - Primary key
- `season_id` - Foreign key to seasons
- `name` - "Championship", "League", "Cup", "Friendly"
- `type` - ENUM matching name

---

#### 3. `teams` Table
**Purpose:** Store all teams (Drum and opponents)

**Excel Mapping:**
- Extracted from match sheet names
- "09. Championship vs **Slaughtmanus**" → Team name
- "08. Championship vs **Magilligan**" → Team name
- "Drum" is always the home team (is_drum = TRUE)

**Fields:**
- `team_id` - Primary key
- `name` - "Drum", "Slaughtmanus", "Magilligan", etc.
- `abbreviation` - Short code (optional)
- `is_drum` - TRUE only for Drum team
- `is_active` - TRUE for currently active teams

---

#### 4. `positions` Table
**Purpose:** Define player positions

**Excel Mapping:**
- Derived from position-specific sheets:
  - "Goalkeepers" → GK
  - "Defenders" → DEF
  - "Midfielders" → MID
  - "Forwards" → FWD

**Fields:**
- `position_id` - Primary key
- `name` - "Goalkeeper", "Defender", "Midfielder", "Forward"
- `code` - "GK", "DEF", "MID", "FWD"
- `display_order` - 1, 2, 3, 4

**Seed Data:**
```sql
INSERT INTO positions (name, code, display_order) VALUES
    ('Goalkeeper', 'GK', 1),
    ('Defender', 'DEF', 2),
    ('Midfielder', 'MID', 3),
    ('Forward', 'FWD', 4);
```

---

#### 5. `players` Table
**Purpose:** Store player roster information

**Excel Mapping:**
- **Source:** Player Stats sheets, Column B (Player Name)
- **Jersey Number:** Column A (#)
- **Position:** Matched to positions table based on position-specific sheets

**Example Excel Data:**
```
Row 4:  [1.0] [Cahair O Kane] [63.0] ...
Row 5:  [2.0] [Seamus O Kane] [59.0] ...
Row 6:  [3.0] [Alex Moore] [63.0] ...
```

**Fields:**
- `player_id` - Primary key
- `jersey_number` - Excel Column A: `#`
- `first_name` - Parsed from Column B (first word)
- `last_name` - Parsed from Column B (remaining words)
- `full_name` - Excel Column B: `Player Name`
- `position_id` - Foreign key (determined from position sheets)
- `is_active` - TRUE for current roster

---

#### 6. `matches` Table
**Purpose:** Store match information

**Excel Mapping:**
- **Sheet Name:** "09. Championship vs Slaughtmanus 26.09.25"
  - `match_number` = 09
  - `competition_id` = lookup "Championship"
  - `away_team_id` = lookup "Slaughtmanus"
  - `match_date` = 2025-09-26
  - `venue` = 'Home' (Drum is always home in sheet name)

- **Scores:** From Row 4, Columns B-G
  ```
  Scoreline | Drum 1st | Drum 2nd | Drum Full | Opp 1st | Opp 2nd | Opp Full
  Row 4:    | "0-04"   | "1-07"   | "1-11"    | "0-10"  | "0-06"  | "0-16"
  ```

**Fields:**
- `match_id` - Primary key
- `competition_id` - Foreign key
- `match_number` - Integer from sheet name
- `home_team_id` - Always Drum (is_drum = TRUE)
- `away_team_id` - Opposition team from sheet name
- `match_date` - Parsed from sheet name date
- `venue` - 'Home', 'Away', or 'Neutral'
- `home_score_first_half` - Row 4, Column B ("0-04")
- `home_score_second_half` - Row 4, Column C ("1-07")
- `home_score_full_time` - Row 4, Column D ("1-11")
- `away_score_first_half` - Row 4, Column E ("0-10")
- `away_score_second_half` - Row 4, Column F ("0-06")
- `away_score_full_time` - Row 4, Column G ("0-16")

---

#### 7. `match_team_statistics` Table
**Purpose:** Store team-level match statistics by period

**Excel Mapping:**
- **Source:** Match sheets (e.g., "09. Championship vs Slaughtmanus")
- **One row per team per period** (6 total: Drum×3 periods + Opposition×3 periods)

**Column Mapping:**

| Database Field | Excel Row | Excel Column | Description |
|----------------|-----------|--------------|-------------|
| `period` | Row 3 | B/C/D (or E/F/G) | '1st', '2nd', 'Full' |
| `scoreline` | Row 4 | B/C/D (or E/F/G) | "0-04", "1-07", "1-11" |
| `total_possession` | Row 5 | B/C/D (or E/F/G) | 0.3545 (decimal) |
| `score_source_kickout_long` | Row 7 | B/C/D (or E/F/G) | Integer |
| `score_source_kickout_short` | Row 8 | B/C/D (or E/F/G) | Integer |
| `score_source_opp_kickout_long` | Row 9 | B/C/D (or E/F/G) | Integer |
| `score_source_opp_kickout_short` | Row 10 | B/C/D (or E/F/G) | Integer |
| `score_source_turnover` | Row 11 | B/C/D (or E/F/G) | Integer |
| `score_source_possession_lost` | Row 12 | B/C/D (or E/F/G) | Integer |
| `score_source_shot_short` | Row 13 | B/C/D (or E/F/G) | Integer |
| `score_source_throw_up_in` | Row 14 | B/C/D (or E/F/G) | Integer |
| `shot_source_*` (8 fields) | Rows 16-23 | B/C/D (or E/F/G) | Same pattern as score_source |

**Example:**
For "09. Championship vs Slaughtmanus 26.09.25", 1st half, Drum:
```sql
INSERT INTO match_team_statistics VALUES (
    match_id: [lookup match],
    team_id: [Drum team_id],
    period: '1st',
    scoreline: '0-04',
    total_possession: 0.3545,
    score_source_kickout_long: 1,
    score_source_kickout_short: 0,
    -- ... 14 more fields
);
```

---

#### 8. `player_match_statistics` Table
**Purpose:** Store individual player performance for each match

**Excel Mapping:**
- **Source:** Player Stats sheets (e.g., "09. Player stats vs Slaughtmanus")
- **One row per player per match**
- **86+ database fields** map directly to Excel columns

**Complete Column Mapping:**

| Database Field | Excel Col | Header | Example Value |
|----------------|-----------|--------|---------------|
| `player_id` | A + B | # + Player Name | Lookup player by jersey + name |
| `minutes_played` | C | Min | 63 |
| `total_engagements` | D | TE | 25 |
| `te_per_psr` | E | TE/PSR | 0.52 |
| `scores` | F | Scores | "1-03(1f)" |
| `psr` | G | PSR | 13 |
| `psr_per_tp` | H | PSR/TP | 13.0 |
| `tp` | I | TP | 1 |
| `tow` | J | ToW | 0 |
| `interceptions` | K | Int | 2 |
| `tpl` | L | TPL | 0 |
| `kp` | M | KP | 0 |
| `hp` | N | HP | 0 |
| `ha` | O | Ha | 0 |
| `turnovers` | P | TO | 0 |
| `ineffective` | Q | In | 0 |
| `shot_short` | R | SS | 0 |
| `shot_save` | S | S Save | 0 |
| `fouled` | T | Fo | 0 |
| `woodwork` | U | Ww | 0 |
| `ko_drum_kow` | V | KoW (Drum) | 0 |
| `ko_drum_wc` | W | WC (Drum) | 0 |
| `ko_drum_bw` | X | BW (Drum) | 0 |
| `ko_drum_sw` | Y | SW (Drum) | 0 |
| `ko_opp_kow` | Z | KoW (Opp) | 0 |
| `ko_opp_wc` | AA | WC (Opp) | 0 |
| `ko_opp_bw` | AB | BW (Opp) | 0 |
| `ko_opp_sw` | AC | SW (Opp) | 0 |
| `ta` | AD | TA | 0 |
| `kr` | AE | KR | 0 |
| `kl` | AF | KL | 0 |
| `cr` | AG | CR | 0 |
| `cl` | AH | CL | 0 |
| `shots_play_total` | AI | Tot (Shots) | 1 |
| `shots_play_points` | AJ | Pts | 0 |
| `shots_play_2points` | AK | 2 Pts | 0 |
| `shots_play_goals` | AL | Gls | 0 |
| `shots_play_wide` | AM | Wid | 1 |
| `shots_play_short` | AN | Sh | 0 |
| `shots_play_save` | AO | Save | 0 |
| `shots_play_woodwork` | AP | Ww | 0 |
| `shots_play_blocked` | AQ | Bd | 0 |
| `shots_play_45` | AR | 45 | 0 |
| `shots_play_percentage` | AS | % (Shots) | 0.0 |
| `frees_total` | AT | Tot (Frees) | 3 |
| `frees_points` | AU | Pts | 1 |
| `frees_2points` | AV | 2 Pts | 0 |
| `frees_goals` | AW | Gls | 0 |
| `frees_wide` | AX | Wid | 2 |
| `frees_short` | AY | Sh | 0 |
| `frees_save` | AZ | Save | 0 |
| `frees_woodwork` | BA | Ww | 0 |
| `frees_45` | BB | 45 | 0 |
| `frees_qf` | BC | QF | 0 |
| `frees_percentage` | BD | % (Frees) | 0.3333 |
| `total_shots` | BE | TS | 7 |
| `total_shots_percentage` | BF | % (Total) | 0.5714 |
| `assists_total` | BG | TA (Assists) | 2 |
| `assists_point` | BH | Point | 1 |
| `assists_goal` | BI | Goal | 1 |
| `tackles_total` | BJ | Tot (Tackle) | 4 |
| `tackles_contested` | BK | Con | 4 |
| `tackles_missed` | BL | Mis | 0 |
| `tackles_percentage` | BM | % (Tackle) | 1.0 |
| `frees_conceded_total` | BN | Tot (FC) | 3 |
| `frees_conceded_attack` | BO | Att | 0 |
| `frees_conceded_midfield` | BP | Mid | 1 |
| `frees_conceded_defense` | BQ | Def | 0 |
| `frees_conceded_penalty` | BR | Pen | 0 |
| `frees_50m_total` | BS | Tot (50m) | 0 |
| `frees_50m_delay` | BT | Delay | 0 |
| `frees_50m_dissent` | BU | Diss | 0 |
| `frees_50m_3v3` | BV | 3v3 | 0 |
| `yellow_cards` | BW | Yel | 0 |
| `black_cards` | BX | Bla | 0 |
| `red_cards` | BY | Red | 0 |
| `throw_up_won` | BZ | Won | 2 |
| `throw_up_lost` | CA | Los | 0 |
| `gk_total_kickouts` | CB | TKo | 24 |
| `gk_kickout_retained` | CC | KoR | 18 |
| `gk_kickout_lost` | CD | KoL | 6 |
| `gk_kickout_percentage` | CE | % (GK) | 0.75 |
| `gk_saves` | CF | Saves | 0 |

**Data Extraction Example:**
For player "Cahair O Kane" (#1) in "09. Player stats vs Slaughtmanus":
```sql
INSERT INTO player_match_statistics VALUES (
    match_id: [lookup "09. Championship vs Slaughtmanus"],
    player_id: [lookup "Cahair O Kane"],
    minutes_played: 63,
    total_engagements: 25,
    te_per_psr: 0.52,
    scores: NULL,
    psr: 13,
    psr_per_tp: 13.0,
    tp: 1,
    -- ... 78 more fields from columns
    gk_total_kickouts: 24,
    gk_kickout_retained: 18,
    gk_kickout_lost: 6,
    gk_kickout_percentage: 0.75,
    gk_saves: 0
);
```

---

#### 9. `kpi_definitions` Table
**Purpose:** Store metric definitions and PSR values

**Excel Mapping:**
- **Source:** "KPI Definitions" sheet
- **Columns:** Event Name (B), Outcome (C), Assign to which team (D), PSR Value (E), Definition (F)

**Example Mappings:**

| event_number | event_name | outcome | team_assignment | psr_value | definition |
|--------------|------------|---------|-----------------|-----------|------------|
| 1 | Kickout | Won clean | Home | 1.0 | A kickout won clean in the air by a home player |
| 1 | Kickout | Lost clean | Opposition | 1.0 | A kickout won clean in the air by an opposition player |
| 2 | Attacks | Kick retained | Home | 1.0 | A player who wins the ball in the forward line |
| 3 | Shot from play | Point | Home | 1.0 | A shot over the bar |
| 3 | Shot from play | 2 Pointer | Home | 2.0 | A shot over the bar outside the 40m arc |
| 3 | Shot from play | Goal | Home | 3.0 | A shot underneath the bar for a goal |

**Data Extraction:**
Read rows starting from Row 3 (after headers) until empty row.

---

## ETL Process Overview

### Phase 1: Reference Data
1. Extract season from match dates → Insert into `seasons`
2. Extract competition types from sheet names → Insert into `competitions`
3. Extract team names from sheet names → Insert into `teams`
4. Load position definitions → Insert into `positions` (seed data)

### Phase 2: Core Data
5. Extract player roster from player stats sheets → Insert into `players`
6. Extract match information from match sheet names → Insert into `matches`

### Phase 3: Statistics Data
7. For each match sheet:
   - Parse team statistics (rows 4-23)
   - Create 6 records in `match_team_statistics` (3 periods × 2 teams)

8. For each player stats sheet:
   - Iterate through player rows (starting row 4)
   - Extract 86 columns per player
   - Insert one record per player into `player_match_statistics`

### Phase 4: Definition Data
9. Parse "KPI Definitions" sheet → Insert into `kpi_definitions`

---

## Data Validation Rules

### Match Sheet Validation
- Sheet name must match pattern: `\d+\. \w+ vs \w+ \d{2}\.\d{2}\.\d{2}`
- Scoreline format: `\d+-\d+` (e.g., "0-04", "1-11")
- Total possession must be between 0 and 1
- All stat fields must be non-negative integers

### Player Stats Validation
- Jersey numbers must be positive integers
- Player names must not be empty
- Minutes played: 0 ≤ minutes ≤ 90 (allowing extra time)
- Percentages: 0 ≤ percentage ≤ 1
- All count fields must be non-negative

### Relationship Validation
- Every match must have exactly 6 team statistics records (3 periods × 2 teams)
- Player statistics can only reference existing players and matches
- Full-time statistics should equal sum of 1st + 2nd half (with minor floating-point tolerance)

---

## Query Examples

### Get all player statistics for a specific match
```sql
SELECT
    p.full_name,
    p.jersey_number,
    pms.*
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN matches m ON pms.match_id = m.match_id
WHERE m.match_number = 9
  AND m.match_date = '2025-09-26';
```

### Get team possession statistics by period
```sql
SELECT
    m.match_number,
    t.name AS team_name,
    mts.period,
    mts.total_possession,
    mts.scoreline
FROM match_team_statistics mts
JOIN matches m ON mts.match_id = m.match_id
JOIN teams t ON mts.team_id = t.team_id
WHERE m.match_date = '2025-09-26'
ORDER BY t.name, mts.period;
```

### Get season shooting statistics by player
```sql
SELECT
    p.full_name,
    SUM(pms.total_shots) AS total_shots,
    SUM(pms.shots_play_points + pms.shots_play_goals) AS scores,
    AVG(pms.total_shots_percentage) AS avg_shooting_pct
FROM player_match_statistics pms
JOIN players p ON pms.player_id = p.player_id
JOIN matches m ON pms.match_id = m.match_id
JOIN competitions c ON m.competition_id = c.competition_id
WHERE c.season_id = 2025
GROUP BY p.player_id, p.full_name
ORDER BY scores DESC;
```

### Get cumulative team statistics for the season
```sql
SELECT
    t.name,
    COUNT(DISTINCT m.match_id) AS matches_played,
    SUM(mts.score_source_kickout_long +
        mts.score_source_kickout_short +
        mts.score_source_opp_kickout_long +
        mts.score_source_opp_kickout_short +
        mts.score_source_turnover +
        mts.score_source_possession_lost) AS total_scores
FROM match_team_statistics mts
JOIN matches m ON mts.match_id = m.match_id
JOIN teams t ON mts.team_id = t.team_id
JOIN competitions c ON m.competition_id = c.competition_id
WHERE c.season_id = 2025
  AND mts.period = 'Full'
GROUP BY t.team_id, t.name;
```

---

## Special Considerations

### Score Notation
Scores in Excel use GAA notation: `G-PP(Ff)`
- `G` = Goals (3 points each)
- `PP` = Points (1 point each, unless 2-pointer)
- `(Ff)` = Number from frees (optional)

**Examples:**
- `1-03(1f)` = 1 goal, 3 points, 1 from free = 6 total points
- `0-05` = 0 goals, 5 points = 5 total points
- `2-07` = 2 goals, 7 points = 13 total points

Store as VARCHAR in database; parse when calculating totals.

### Period Aggregation
- **1st half + 2nd half ≈ Full time** (should match within rounding errors)
- Always validate full-time stats against sum of halves during ETL
- Use full-time values for season aggregations

### Missing Data
- Empty cells in Excel should map to:
  - `0` for numeric count fields
  - `NULL` for percentages when denominator is 0
  - `NULL` for optional text fields
- Minutes played = 0 indicates player on bench (not used)

### Goalkeeper Fields
- Only populate goalkeeper stats for players in GK position
- Non-goalkeepers should have `NULL` or `0` for GK-specific fields

---

## Database Schema Files

**Location:** `database/migrations/`

**Key Files:**
- `V2.0__drum_analysis_schema.sql` - Schema creation migration
- `V2.0__drum_analysis_schema_rollback.sql` - Rollback script

**Model Files:** `backend/src/GAAStat.Dal/Models/`
- `Season.cs`
- `Competition.cs`
- `Team.cs`
- `Position.cs`
- `Player.cs`
- `Match.cs`
- `MatchTeamStatistics.cs`
- `PlayerMatchStatistics.cs`
- `KpiDefinition.cs`

**DbContext:** `backend/src/GAAStat.Dal/Contexts/GAAStatDbContext.cs`

---

## Future Enhancements

### Planned Features
1. **Automated ETL Pipeline** - Scheduled Excel import jobs
2. **Data Validation Layer** - Pre-import validation and error reporting
3. **Aggregation Views** - Materialized views for season/player totals
4. **Time-Series Analysis** - Player performance trends over time
5. **Comparison Queries** - Match-by-match and season-by-season comparisons
6. **Export Functionality** - Generate reports back to Excel format

### Potential Schema Extensions
- Add `user` table for tracking who created/updated records
- Add `audit_log` table for change tracking
- Add `match_events` table for play-by-play data (if source becomes available)
- Add `training_sessions` table to track practice statistics
- Add `injuries` table to correlate performance with player health

---

## Contact & References

**Project Board:** [GAAStat JIRA](https://caddieaiapp.atlassian.net/jira/software/projects/GAAS/boards/35)

**Related JIRA Tickets:**
- GAAS-4: Create Comprehensive Database Schema for Drum Analysis 2025 Excel Data Structure

**Excel Source File:** `/Users/shane.millar/Desktop/Drum Analysis 2025.xlsx`

---

*Last Updated: 2025-10-09*
*Document Version: 1.0*
