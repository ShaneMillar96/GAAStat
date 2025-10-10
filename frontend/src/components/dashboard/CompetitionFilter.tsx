/**
 * CompetitionFilter Component
 * Dropdown filter for selecting competition type
 */

import React from 'react';
import type { CompetitionType } from '../../types/dashboard.types';

export interface CompetitionFilterProps {
  /** Currently selected competition type */
  selectedCompetition: CompetitionType;
  /** Callback when competition selection changes */
  onCompetitionChange: (competition: CompetitionType) => void;
  /** Optional CSS class */
  className?: string;
  /** Optional label text */
  label?: string;
}

/**
 * Available competition types
 */
const COMPETITION_OPTIONS: CompetitionType[] = [
  'All',
  'League',
  'Championship',
  'Cup',
  'Friendly',
];

/**
 * CompetitionFilter - Dropdown for filtering dashboard data by competition type
 *
 * @example
 * <CompetitionFilter
 *   selectedCompetition={competition}
 *   onCompetitionChange={setCompetition}
 * />
 */
export const CompetitionFilter = React.memo<CompetitionFilterProps>(({
  selectedCompetition,
  onCompetitionChange,
  className = '',
  label = 'Competition',
}) => {
  const handleChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    onCompetitionChange(event.target.value as CompetitionType);
  };

  return (
    <div className={`flex items-center space-x-2 ${className}`}>
      <label
        htmlFor="competition-filter"
        className="text-sm font-medium text-gray-700"
      >
        {label}:
      </label>
      <select
        id="competition-filter"
        value={selectedCompetition}
        onChange={handleChange}
        className="block w-40 rounded-md border-gray-300 shadow-sm focus:border-green-500 focus:ring-green-500 text-sm py-2 px-3 bg-white border"
        aria-label="Filter by competition type"
      >
        {COMPETITION_OPTIONS.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </div>
  );
});

CompetitionFilter.displayName = 'CompetitionFilter';
