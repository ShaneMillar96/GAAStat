/**
 * StatCard Component
 * Displays a single statistic with label, value, and optional description
 */

import React from 'react';

export interface StatCardProps {
  /** Label for the statistic */
  label: string;
  /** Numeric value to display */
  value: number | string;
  /** Optional description or subtitle */
  description?: string;
  /** Optional icon element */
  icon?: React.ReactNode;
  /** Optional CSS class for customization */
  className?: string;
  /** Optional format for percentage values */
  formatAsPercentage?: boolean;
  /** Optional number of decimal places */
  decimalPlaces?: number;
  /** Optional aria-label for accessibility */
  ariaLabel?: string;
}

/**
 * Formats a number with specified decimal places and optional percentage
 */
function formatValue(
  value: number | string,
  formatAsPercentage: boolean,
  decimalPlaces: number
): string {
  if (typeof value === 'string') return value;

  const formattedNumber = value.toFixed(decimalPlaces);

  if (formatAsPercentage) {
    const percentage = (value * 100).toFixed(decimalPlaces);
    return `${percentage}%`;
  }

  return formattedNumber;
}

/**
 * StatCard - Displays a single statistic in a card format
 *
 * @example
 * <StatCard
 *   label="Win Percentage"
 *   value={0.667}
 *   formatAsPercentage={true}
 *   description="Season 2025"
 * />
 */
export const StatCard = React.memo<StatCardProps>(({
  label,
  value,
  description,
  icon,
  className = '',
  formatAsPercentage = false,
  decimalPlaces = 0,
  ariaLabel,
}) => {
  const displayValue = formatValue(value, formatAsPercentage, decimalPlaces);

  return (
    <div
      className={`bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow duration-200 ${className}`}
      role="article"
      aria-label={ariaLabel || `${label}: ${displayValue}`}
    >
      {/* Icon and Label Row */}
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-sm font-medium text-gray-600 uppercase tracking-wide">
          {label}
        </h3>
        {icon && (
          <div className="text-green-600" aria-hidden="true">
            {icon}
          </div>
        )}
      </div>

      {/* Value */}
      <p className="text-3xl font-bold text-gray-900 mb-1">
        {displayValue}
      </p>

      {/* Description */}
      {description && (
        <p className="text-sm text-gray-500">
          {description}
        </p>
      )}
    </div>
  );
});

StatCard.displayName = 'StatCard';
