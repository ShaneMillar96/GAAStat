/**
 * TopPerformersCard Component
 * Displays a list of top-performing players by a specific metric
 */

import React from 'react';
import type { TopPerformerDto } from '../../types/dashboard.types';

export interface TopPerformersCardProps {
  /** Title for the card */
  title: string;
  /** List of top performers */
  performers: TopPerformerDto[];
  /** Metric name to display (e.g., "Points", "PSR", "Tackles") */
  metricLabel: string;
  /** Optional loading state */
  isLoading?: boolean;
  /** Optional error message */
  error?: string;
  /** Optional CSS class */
  className?: string;
}

/**
 * Skeleton loader for loading state
 */
const SkeletonLoader: React.FC = () => (
  <div className="space-y-3">
    {[1, 2, 3].map((i) => (
      <div key={i} className="animate-pulse flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <div className="h-8 w-8 bg-gray-200 rounded-full" />
          <div>
            <div className="h-4 w-32 bg-gray-200 rounded mb-2" />
            <div className="h-3 w-24 bg-gray-200 rounded" />
          </div>
        </div>
        <div className="h-6 w-12 bg-gray-200 rounded" />
      </div>
    ))}
  </div>
);

/**
 * Empty state component
 */
const EmptyState: React.FC = () => (
  <div className="text-center py-8">
    <p className="text-gray-500 text-sm">No performers found</p>
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
 * TopPerformersCard - Displays ranked list of top-performing players
 *
 * @example
 * <TopPerformersCard
 *   title="Top Scorers"
 *   performers={topScorers}
 *   metricLabel="Points"
 * />
 */
export const TopPerformersCard = React.memo<TopPerformersCardProps>(({
  title,
  performers,
  metricLabel,
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
      ) : performers.length === 0 ? (
        <EmptyState />
      ) : (
        <ol className="space-y-3" aria-label={`${title} list`}>
          {performers.map((performer, index) => (
            <li
              key={performer.playerId}
              className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors duration-150"
            >
              {/* Rank and Player Info */}
              <div className="flex items-center space-x-3">
                {/* Rank Badge */}
                <div
                  className={`flex items-center justify-center h-8 w-8 rounded-full font-bold text-sm ${
                    index === 0
                      ? 'bg-yellow-400 text-gray-900'
                      : index === 1
                      ? 'bg-gray-300 text-gray-900'
                      : index === 2
                      ? 'bg-orange-400 text-white'
                      : 'bg-gray-100 text-gray-700'
                  }`}
                  aria-label={`Rank ${index + 1}`}
                >
                  {index + 1}
                </div>

                {/* Player Details */}
                <div>
                  <p className="font-semibold text-gray-900">
                    {performer.playerName}
                    <span className="ml-2 text-xs text-gray-500">
                      #{performer.jerseyNumber}
                    </span>
                  </p>
                  <p className="text-xs text-gray-500">
                    {performer.positionCode} • {performer.matchesPlayed}{' '}
                    {performer.matchesPlayed === 1 ? 'match' : 'matches'}
                  </p>
                </div>
              </div>

              {/* Metric Value */}
              <div className="text-right">
                <p className="text-lg font-bold text-green-600">
                  {performer.metricValue}
                </p>
                <p className="text-xs text-gray-500">{metricLabel}</p>
              </div>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
});

TopPerformersCard.displayName = 'TopPerformersCard';
