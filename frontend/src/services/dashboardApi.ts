/**
 * Dashboard API Client
 * Provides type-safe HTTP client functions for dashboard endpoints
 */

import axios, { AxiosError } from 'axios';
import type {
  DashboardResponse,
  TeamOverviewDto,
  TopPerformerDto,
  RecentMatchDto,
  PlayerSeasonStatsDto,
  TeamFormDto,
  TeamOverviewParams,
  TopPerformersParams,
  RecentMatchesParams,
  PlayerSeasonStatsParams,
  TeamFormParams,
} from '../types/dashboard.types';

// ============================================================================
// Configuration
// ============================================================================

/**
 * Base API URL - defaults to localhost for development
 * Override with VITE_API_BASE_URL environment variable
 */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5025';

/**
 * Dashboard API endpoints
 */
const ENDPOINTS = {
  TEAM_OVERVIEW: '/api/Dashboard/team-overview',
  TOP_PERFORMERS: '/api/Dashboard/top-performers',
  RECENT_MATCHES: '/api/Dashboard/recent-matches',
  PLAYER_SEASON_STATS: '/api/Dashboard/player-season-stats',
  TEAM_FORM: '/api/Dashboard/team-form',
} as const;

/**
 * Request timeout in milliseconds (30 seconds)
 */
const REQUEST_TIMEOUT = 30000;

// ============================================================================
// Axios Instance Configuration
// ============================================================================

const dashboardApiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: REQUEST_TIMEOUT,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ============================================================================
// Error Handling
// ============================================================================

/**
 * Custom error class for dashboard API errors
 */
export class DashboardApiError extends Error {
  public readonly statusCode: number;
  public readonly errors: Array<{ code: string; message: string }>;
  public readonly warnings: Array<{ code: string; message: string }>;

  constructor(
    message: string,
    statusCode: number,
    errors: Array<{ code: string; message: string }> = [],
    warnings: Array<{ code: string; message: string }> = []
  ) {
    super(message);
    this.name = 'DashboardApiError';
    this.statusCode = statusCode;
    this.errors = errors;
    this.warnings = warnings;
  }
}

/**
 * Handles API errors and converts them to DashboardApiError
 */
function handleApiError(error: unknown): never {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<DashboardResponse<unknown>>;

    // Server responded with error
    if (axiosError.response) {
      const { status, data } = axiosError.response;
      const errors = data?.errors || [];
      const warnings = data?.warnings || [];
      const message =
        errors.length > 0
          ? errors[0].message
          : `Dashboard API error: ${status}`;

      throw new DashboardApiError(message, status, errors, warnings);
    }

    // Network error (no response received)
    if (axiosError.request) {
      throw new DashboardApiError(
        'Network error: Unable to reach dashboard API',
        0,
        [{ code: 'NETWORK_ERROR', message: 'No response from server' }]
      );
    }
  }

  // Unknown error
  throw new DashboardApiError(
    error instanceof Error ? error.message : 'Unknown error occurred',
    500,
    [{ code: 'UNKNOWN_ERROR', message: 'An unexpected error occurred' }]
  );
}

// ============================================================================
// API Client Functions
// ============================================================================

/**
 * Get team performance overview with season totals and averages
 *
 * @param params - Query parameters (seasonId, competitionType)
 * @returns Team overview data
 * @throws DashboardApiError
 *
 * @example
 * const overview = await getTeamOverview({ seasonId: 2025, competitionType: 'League' });
 * console.log(`Win percentage: ${overview.data.winPercentage}`);
 */
export async function getTeamOverview(
  params: TeamOverviewParams = {}
): Promise<DashboardResponse<TeamOverviewDto>> {
  try {
    const response = await dashboardApiClient.get<DashboardResponse<TeamOverviewDto>>(
      ENDPOINTS.TEAM_OVERVIEW,
      { params }
    );
    return response.data;
  } catch (error) {
    return handleApiError(error);
  }
}

/**
 * Get top performers by metric
 *
 * @param params - Query parameters (metricType, seasonId, competitionType, topCount)
 * @returns List of top performers ordered by metric value
 * @throws DashboardApiError
 *
 * @example
 * const topScorers = await getTopPerformers({
 *   metricType: 'scoring',
 *   topCount: 10,
 *   competitionType: 'Championship'
 * });
 */
export async function getTopPerformers(
  params: TopPerformersParams
): Promise<DashboardResponse<TopPerformerDto[]>> {
  try {
    const response = await dashboardApiClient.get<DashboardResponse<TopPerformerDto[]>>(
      ENDPOINTS.TOP_PERFORMERS,
      { params }
    );
    return response.data;
  } catch (error) {
    return handleApiError(error);
  }
}

/**
 * Get recent match results with scores and outcomes
 *
 * @param params - Query parameters (seasonId, competitionType, matchCount)
 * @returns List of recent matches ordered by date descending
 * @throws DashboardApiError
 *
 * @example
 * const recentMatches = await getRecentMatches({ matchCount: 5 });
 */
export async function getRecentMatches(
  params: RecentMatchesParams = {}
): Promise<DashboardResponse<RecentMatchDto[]>> {
  try {
    const response = await dashboardApiClient.get<DashboardResponse<RecentMatchDto[]>>(
      ENDPOINTS.RECENT_MATCHES,
      { params }
    );
    return response.data;
  } catch (error) {
    return handleApiError(error);
  }
}

/**
 * Get aggregated player season statistics
 *
 * @param params - Query parameters (seasonId, competitionType, positionCode)
 * @returns List of player statistics aggregated across all matches
 * @throws DashboardApiError
 *
 * @example
 * const forwardStats = await getPlayerSeasonStatistics({ positionCode: 'FWD' });
 */
export async function getPlayerSeasonStatistics(
  params: PlayerSeasonStatsParams = {}
): Promise<DashboardResponse<PlayerSeasonStatsDto[]>> {
  try {
    const response = await dashboardApiClient.get<DashboardResponse<PlayerSeasonStatsDto[]>>(
      ENDPOINTS.PLAYER_SEASON_STATS,
      { params }
    );
    return response.data;
  } catch (error) {
    return handleApiError(error);
  }
}

/**
 * Get team form (win/loss/draw record for recent matches)
 *
 * @param params - Query parameters (seasonId, competitionType, matchCount)
 * @returns Team form with counts and form string (e.g., "WWLDW")
 * @throws DashboardApiError
 *
 * @example
 * const form = await getTeamForm({ matchCount: 10 });
 * console.log(`Form: ${form.data.formString}`); // "WWLWDLWLWW"
 */
export async function getTeamForm(
  params: TeamFormParams = {}
): Promise<DashboardResponse<TeamFormDto>> {
  try {
    const response = await dashboardApiClient.get<DashboardResponse<TeamFormDto>>(
      ENDPOINTS.TEAM_FORM,
      { params }
    );
    return response.data;
  } catch (error) {
    return handleApiError(error);
  }
}

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Check if API is reachable (health check)
 *
 * @returns true if API is reachable, false otherwise
 */
export async function checkApiHealth(): Promise<boolean> {
  try {
    await dashboardApiClient.get('/health', { timeout: 5000 });
    return true;
  } catch {
    return false;
  }
}

/**
 * Get base API URL (useful for debugging)
 */
export function getApiBaseUrl(): string {
  return API_BASE_URL;
}
