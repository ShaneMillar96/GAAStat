/**
 * Dashboard Page
 * Main dashboard view with team overview, top performers, recent matches, and form
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  StatCard,
  TopPerformersCard,
  RecentMatchesCard,
  TeamFormCard,
  CompetitionFilter,
} from '../components/dashboard';
import {
  getTeamOverview,
  getTopPerformers,
  getRecentMatches,
  getTeamForm,
} from '../services/dashboardApi';
import type {
  TeamOverviewDto,
  TopPerformerDto,
  RecentMatchDto,
  TeamFormDto,
  CompetitionType,
} from '../types/dashboard.types';

/**
 * Dashboard Page Component
 * Displays comprehensive team and player statistics
 */
export const Dashboard: React.FC = () => {
  // State for competition filter
  const [selectedCompetition, setSelectedCompetition] = useState<CompetitionType>('All');

  // State for team overview
  const [teamOverview, setTeamOverview] = useState<TeamOverviewDto | null>(null);
  const [overviewLoading, setOverviewLoading] = useState(true);
  const [overviewError, setOverviewError] = useState<string | null>(null);

  // State for top performers
  const [topPerformers, setTopPerformers] = useState<TopPerformerDto[]>([]);
  const [performersLoading, setPerformersLoading] = useState(true);
  const [performersError, setPerformersError] = useState<string | null>(null);

  // State for recent matches
  const [recentMatches, setRecentMatches] = useState<RecentMatchDto[]>([]);
  const [matchesLoading, setMatchesLoading] = useState(true);
  const [matchesError, setMatchesError] = useState<string | null>(null);

  // State for team form
  const [teamForm, setTeamForm] = useState<TeamFormDto | null>(null);
  const [formLoading, setFormLoading] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);

  /**
   * Fetch team overview data
   */
  const fetchTeamOverview = useCallback(async () => {
    setOverviewLoading(true);
    setOverviewError(null);

    try {
      const response = await getTeamOverview({
        competitionType: selectedCompetition === 'All' ? undefined : selectedCompetition,
      });

      if (response.success && response.data) {
        setTeamOverview(response.data);
      } else {
        setOverviewError(
          response.errors[0]?.message || 'Failed to load team overview'
        );
      }
    } catch (error) {
      setOverviewError('Failed to load team overview');
      console.error('Team overview error:', error);
    } finally {
      setOverviewLoading(false);
    }
  }, [selectedCompetition]);

  /**
   * Fetch top performers data
   */
  const fetchTopPerformers = useCallback(async () => {
    setPerformersLoading(true);
    setPerformersError(null);

    try {
      const response = await getTopPerformers({
        metricType: 'scoring',
        topCount: 5,
        competitionType: selectedCompetition === 'All' ? undefined : selectedCompetition,
      });

      if (response.success && response.data) {
        setTopPerformers(response.data);
      } else {
        setPerformersError(
          response.errors[0]?.message || 'Failed to load top performers'
        );
      }
    } catch (error) {
      setPerformersError('Failed to load top performers');
      console.error('Top performers error:', error);
    } finally {
      setPerformersLoading(false);
    }
  }, [selectedCompetition]);

  /**
   * Fetch recent matches data
   */
  const fetchRecentMatches = useCallback(async () => {
    setMatchesLoading(true);
    setMatchesError(null);

    try {
      const response = await getRecentMatches({
        matchCount: 5,
        competitionType: selectedCompetition === 'All' ? undefined : selectedCompetition,
      });

      if (response.success && response.data) {
        setRecentMatches(response.data);
      } else {
        setMatchesError(
          response.errors[0]?.message || 'Failed to load recent matches'
        );
      }
    } catch (error) {
      setMatchesError('Failed to load recent matches');
      console.error('Recent matches error:', error);
    } finally {
      setMatchesLoading(false);
    }
  }, [selectedCompetition]);

  /**
   * Fetch team form data
   */
  const fetchTeamForm = useCallback(async () => {
    setFormLoading(true);
    setFormError(null);

    try {
      const response = await getTeamForm({
        matchCount: 10,
        competitionType: selectedCompetition === 'All' ? undefined : selectedCompetition,
      });

      if (response.success && response.data) {
        setTeamForm(response.data);
      } else {
        setFormError(
          response.errors[0]?.message || 'Failed to load team form'
        );
      }
    } catch (error) {
      setFormError('Failed to load team form');
      console.error('Team form error:', error);
    } finally {
      setFormLoading(false);
    }
  }, [selectedCompetition]);

  /**
   * Fetch all dashboard data when component mounts or competition changes
   */
  useEffect(() => {
    fetchTeamOverview();
    fetchTopPerformers();
    fetchRecentMatches();
    fetchTeamForm();
  }, [
    selectedCompetition,
    fetchTeamOverview,
    fetchTopPerformers,
    fetchRecentMatches,
    fetchTeamForm,
  ]);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">
                GAAStat Dashboard
              </h1>
              <p className="mt-1 text-sm text-gray-500">
                Team performance and player statistics
              </p>
            </div>

            {/* Competition Filter */}
            <CompetitionFilter
              selectedCompetition={selectedCompetition}
              onCompetitionChange={setSelectedCompetition}
            />
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Team Overview Stats Grid */}
        <section aria-label="Team overview statistics">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">
            Team Overview
          </h2>
          {overviewLoading ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="h-32 bg-gray-200 animate-pulse rounded-lg" />
              ))}
            </div>
          ) : overviewError ? (
            <div className="bg-red-50 p-4 rounded-lg mb-8">
              <p className="text-red-600 text-sm">{overviewError}</p>
            </div>
          ) : teamOverview ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              <StatCard
                label="Matches Played"
                value={teamOverview.totalMatches}
                description={`${teamOverview.wins}W - ${teamOverview.draws}D - ${teamOverview.losses}L`}
              />
              <StatCard
                label="Win Percentage"
                value={teamOverview.winPercentage}
                formatAsPercentage={true}
                decimalPlaces={1}
              />
              <StatCard
                label="Avg Points Scored"
                value={teamOverview.averagePointsScored}
                decimalPlaces={1}
                description={`${teamOverview.totalPointsScored} total points`}
              />
              <StatCard
                label="Avg Possession"
                value={teamOverview.averagePossession}
                formatAsPercentage={true}
                decimalPlaces={1}
              />
            </div>
          ) : null}
        </section>

        {/* Two Column Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Left Column */}
          <div className="space-y-8">
            {/* Top Performers */}
            <TopPerformersCard
              title="Top Scorers"
              performers={topPerformers}
              metricLabel="Points"
              isLoading={performersLoading}
              error={performersError || undefined}
            />

            {/* Team Form */}
            <TeamFormCard
              title="Recent Form"
              form={teamForm}
              isLoading={formLoading}
              error={formError || undefined}
            />
          </div>

          {/* Right Column */}
          <div>
            {/* Recent Matches */}
            <RecentMatchesCard
              title="Recent Matches"
              matches={recentMatches}
              isLoading={matchesLoading}
              error={matchesError || undefined}
            />
          </div>
        </div>
      </main>
    </div>
  );
};

export default Dashboard;
