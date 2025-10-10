/**
 * Dashboard TypeScript Type Definitions
 * Maps directly to backend API DTOs from GAAStat.Api.Models and GAAStat.Services.Dashboard.Models
 */

// ============================================================================
// API Response Wrapper Types
// ============================================================================

/**
 * Generic dashboard API response wrapper
 * Matches DashboardResponseDto<T> from backend
 */
export interface DashboardResponse<T> {
  success: boolean;
  data: T | null;
  durationMs: number;
  errors: ErrorDto[];
  warnings: WarningDto[];
}

/**
 * Error details in API responses
 */
export interface ErrorDto {
  code: string;
  message: string;
}

/**
 * Warning details in API responses
 */
export interface WarningDto {
  code: string;
  message: string;
}

// ============================================================================
// Dashboard Data Types
// ============================================================================

/**
 * Team performance overview with season totals and averages
 * Matches TeamOverviewDto from backend
 */
export interface TeamOverviewDto {
  totalMatches: number;
  wins: number;
  losses: number;
  draws: number;
  winPercentage: number;
  totalPointsScored: number;
  totalPointsConceded: number;
  averagePointsScored: number;
  averagePointsConceded: number;
  averagePossession: number;
}

/**
 * Top performer data by metric
 * Matches TopPerformerDto from backend
 */
export interface TopPerformerDto {
  playerId: number;
  playerName: string;
  jerseyNumber: number;
  positionCode: string;
  metricValue: number;
  matchesPlayed: number;
  totalMinutes: number;
}

/**
 * Recent match result data
 * Matches RecentMatchDto from backend
 */
export interface RecentMatchDto {
  matchId: number;
  matchNumber: number;
  matchDate: string;
  competitionType: string;
  homeTeamName: string;
  awayTeamName: string;
  homeScore: number;
  awayScore: number;
  result: 'Win' | 'Loss' | 'Draw';
  possession: number;
}

/**
 * Player season statistics aggregated across all matches
 * Matches PlayerSeasonStatsDto from backend
 */
export interface PlayerSeasonStatsDto {
  playerId: number;
  playerName: string;
  jerseyNumber: number;
  positionCode: string;
  matchesPlayed: number;
  totalMinutes: number;
  averageMinutes: number;
  totalPsr: number;
  averagePsr: number;
  totalEngagements: number;
  averageEngagements: number;
  totalPointsScored: number;
  averagePointsScored: number;
  totalAssists: number;
  averageAssists: number;
  totalTackles: number;
  averageTackles: number;
  tackleSuccessRate: number;
  shotsTotal: number;
  shotsOnTarget: number;
  shootingPercentage: number;
  freesTotal: number;
  freesSuccessful: number;
  freeSuccessRate: number;
  turnovers: number;
  averageTurnovers: number;
  freesConceded: number;
  averageFreesConceded: number;
  yellowCards: number;
  blackCards: number;
  redCards: number;
}

/**
 * Team form (recent match outcomes)
 * Matches TeamFormDto from backend
 */
export interface TeamFormDto {
  lastNMatches: number;
  wins: number;
  losses: number;
  draws: number;
  formString: string; // e.g., "WWLDW"
  winPercentage: number;
}

// ============================================================================
// Request Parameter Types
// ============================================================================

/**
 * Query parameters for team overview endpoint
 */
export interface TeamOverviewParams {
  seasonId?: number;
  competitionType?: string;
}

/**
 * Query parameters for top performers endpoint
 */
export interface TopPerformersParams {
  metricType: 'scoring' | 'psr' | 'tackles' | 'assists' | 'possession' | 'interceptions';
  seasonId?: number;
  competitionType?: string;
  topCount?: number;
}

/**
 * Query parameters for recent matches endpoint
 */
export interface RecentMatchesParams {
  seasonId?: number;
  competitionType?: string;
  matchCount?: number;
}

/**
 * Query parameters for player season statistics endpoint
 */
export interface PlayerSeasonStatsParams {
  seasonId?: number;
  competitionType?: string;
  positionCode?: 'GK' | 'DEF' | 'MID' | 'FWD';
}

/**
 * Query parameters for team form endpoint
 */
export interface TeamFormParams {
  seasonId?: number;
  competitionType?: string;
  matchCount?: number;
}

// ============================================================================
// Utility Types
// ============================================================================

/**
 * Competition type enum
 */
export type CompetitionType = 'League' | 'Championship' | 'Cup' | 'Friendly' | 'All';

/**
 * Position code enum
 */
export type PositionCode = 'GK' | 'DEF' | 'MID' | 'FWD';

/**
 * Match result enum
 */
export type MatchResult = 'Win' | 'Loss' | 'Draw';

/**
 * Metric type for top performers
 */
export type MetricType = 'scoring' | 'psr' | 'tackles' | 'assists' | 'possession' | 'interceptions';
