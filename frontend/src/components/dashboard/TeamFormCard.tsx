/**
 * TeamFormCard Component
 * Displays team form (recent win/loss/draw record) with visual indicators
 */

import React from 'react';
import type { TeamFormDto } from '../../types/dashboard.types';

export interface TeamFormCardProps {
  /** Title for the card */
  title: string;
  /** Team form data */
  form: TeamFormDto | null;
  /** Optional loading state */
  isLoading?: boolean;
  /** Optional error message */
  error?: string;
  /** Optional CSS class */
  className?: string;
}

/**
 * Gets the color class for a form result
 */
function getFormBadgeClass(result: 'W' | 'L' | 'D'): string {
  switch (result) {
    case 'W':
      return 'bg-green-500 text-white';
    case 'L':
      return 'bg-red-500 text-white';
    case 'D':
      return 'bg-yellow-500 text-white';
    default:
      return 'bg-gray-500 text-white';
  }
}

/**
 * Gets the full word for a form letter
 */
function getFormLabel(result: 'W' | 'L' | 'D'): string {
  switch (result) {
    case 'W':
      return 'Win';
    case 'L':
      return 'Loss';
    case 'D':
      return 'Draw';
    default:
      return 'Unknown';
  }
}

/**
 * Skeleton loader for loading state
 */
const SkeletonLoader: React.FC = () => (
  <div className="space-y-4">
    <div className="flex space-x-2">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="h-12 w-12 bg-gray-200 rounded animate-pulse" />
      ))}
    </div>
    <div className="grid grid-cols-3 gap-4">
      {[1, 2, 3].map((i) => (
        <div key={i} className="animate-pulse">
          <div className="h-4 w-16 bg-gray-200 rounded mb-2" />
          <div className="h-8 w-12 bg-gray-200 rounded" />
        </div>
      ))}
    </div>
  </div>
);

/**
 * Empty state component
 */
const EmptyState: React.FC = () => (
  <div className="text-center py-8">
    <p className="text-gray-500 text-sm">No form data available</p>
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
 * TeamFormCard - Displays team's recent form with win/loss/draw record
 *
 * @example
 * <TeamFormCard
 *   title="Recent Form"
 *   form={teamForm}
 * />
 */
export const TeamFormCard = React.memo<TeamFormCardProps>(({
  title,
  form,
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
      ) : !form ? (
        <EmptyState />
      ) : (
        <div className="space-y-6">
          {/* Form String Visualization */}
          <div className="flex flex-wrap gap-2" role="list" aria-label="Match results">
            {form.formString.split('').map((result, index) => (
              <div
                key={index}
                className={`flex items-center justify-center h-12 w-12 rounded font-bold text-lg ${getFormBadgeClass(result as 'W' | 'L' | 'D')} shadow-sm`}
                role="listitem"
                aria-label={`Match ${index + 1}: ${getFormLabel(result as 'W' | 'L' | 'D')}`}
                title={getFormLabel(result as 'W' | 'L' | 'D')}
              >
                {result}
              </div>
            ))}
          </div>

          {/* Statistics */}
          <div className="grid grid-cols-3 gap-4 pt-4 border-t border-gray-200">
            {/* Wins */}
            <div className="text-center">
              <p className="text-sm text-gray-600 mb-1">Wins</p>
              <p className="text-2xl font-bold text-green-600">{form.wins}</p>
            </div>

            {/* Draws */}
            <div className="text-center">
              <p className="text-sm text-gray-600 mb-1">Draws</p>
              <p className="text-2xl font-bold text-yellow-600">{form.draws}</p>
            </div>

            {/* Losses */}
            <div className="text-center">
              <p className="text-sm text-gray-600 mb-1">Losses</p>
              <p className="text-2xl font-bold text-red-600">{form.losses}</p>
            </div>
          </div>

          {/* Form Summary */}
          <div className="text-center pt-2">
            <p className="text-xs text-gray-500">
              Last {form.lastNMatches} {form.lastNMatches === 1 ? 'match' : 'matches'}
            </p>
          </div>
        </div>
      )}
    </div>
  );
});

TeamFormCard.displayName = 'TeamFormCard';
