/**
 * RecentMatchesCard Component
 * Displays a list of recent match results with scores and outcomes
 */

import React from 'react';
import type { RecentMatchDto } from '../../types/dashboard.types';

export interface RecentMatchesCardProps {
  /** Title for the card */
  title: string;
  /** List of recent matches */
  matches: RecentMatchDto[];
  /** Optional loading state */
  isLoading?: boolean;
  /** Optional error message */
  error?: string;
  /** Optional CSS class */
  className?: string;
}

/**
 * Formats a date string to a readable format
 */
function formatMatchDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-IE', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

/**
 * Gets the badge color based on match result
 */
function getResultBadgeClass(result: string): string {
  switch (result) {
    case 'Win':
      return 'bg-green-100 text-green-800';
    case 'Loss':
      return 'bg-red-100 text-red-800';
    case 'Draw':
      return 'bg-yellow-100 text-yellow-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
}

/**
 * Skeleton loader for loading state
 */
const SkeletonLoader: React.FC = () => (
  <div className="space-y-4">
    {[1, 2, 3].map((i) => (
      <div key={i} className="animate-pulse border-b border-gray-200 pb-4">
        <div className="flex justify-between items-center mb-2">
          <div className="h-4 w-24 bg-gray-200 rounded" />
          <div className="h-6 w-16 bg-gray-200 rounded-full" />
        </div>
        <div className="h-5 w-48 bg-gray-200 rounded mb-1" />
        <div className="h-4 w-32 bg-gray-200 rounded" />
      </div>
    ))}
  </div>
);

/**
 * Empty state component
 */
const EmptyState: React.FC = () => (
  <div className="text-center py-8">
    <p className="text-gray-500 text-sm">No recent matches found</p>
  </div>
);

/**
 * Error state component
 */
const ErrorState: React.FC<{ message: string }> = ({ message }) => (
  <div className="text-center py-8">
    <p className="text-red-600 text-sm">{message}</p>
  </div>
);

/**
 * RecentMatchesCard - Displays list of recent match results
 *
 * @example
 * <RecentMatchesCard
 *   title="Recent Matches"
 *   matches={recentMatches}
 * />
 */
export const RecentMatchesCard = React.memo<RecentMatchesCardProps>(({
  title,
  matches,
  isLoading = false,
  error,
  className = '',
}) => {
  return (
    <div
      className={`bg-white rounded-lg shadow-md p-6 ${className}`}
      role="region"
      aria-label={title}
    >
      {/* Header */}
      <h2 className="text-xl font-bold text-gray-900 mb-4">
        {title}
      </h2>

      {/* Content */}
      {isLoading ? (
        <SkeletonLoader />
      ) : error ? (
        <ErrorState message={error} />
      ) : matches.length === 0 ? (
        <EmptyState />
      ) : (
        <ul className="space-y-4" aria-label="Recent matches list">
          {matches.map((match) => (
            <li
              key={match.matchId}
              className="border-b border-gray-200 pb-4 last:border-b-0 last:pb-0 hover:bg-gray-50 p-3 -mx-3 rounded-lg transition-colors duration-150"
            >
              {/* Date and Result Badge */}
              <div className="flex justify-between items-center mb-2">
                <time
                  className="text-sm text-gray-500"
                  dateTime={match.matchDate}
                >
                  {formatMatchDate(match.matchDate)}
                </time>
                <span
                  className={`px-3 py-1 rounded-full text-xs font-semibold ${getResultBadgeClass(match.result)}`}
                  aria-label={`Result: ${match.result}`}
                >
                  {match.result}
                </span>
              </div>

              {/* Teams and Score */}
              <div className="mb-2">
                <p className="text-base font-semibold text-gray-900">
                  {match.homeTeamName}{' '}
                  <span className="font-bold text-green-600">{match.homeScore}</span>
                  {' - '}
                  <span className="font-bold text-gray-700">{match.awayScore}</span>
                  {' '}
                  {match.awayTeamName}
                </p>
              </div>

              {/* Competition and Possession */}
              <div className="flex items-center justify-between text-xs text-gray-500">
                <span className="font-medium">
                  {match.competitionType}
                </span>
                <span>
                  Possession: {(match.possession * 100).toFixed(1)}%
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
});

RecentMatchesCard.displayName = 'RecentMatchesCard';
