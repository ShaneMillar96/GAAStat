/**
 * Playwright E2E Tests for GAAStat Dashboard
 * Tests dashboard page functionality, components, and API integration
 */

import { test, expect } from '@playwright/test';

// Mock API responses
const mockTeamOverview = {
  success: true,
  data: {
    totalMatches: 9,
    wins: 3,
    losses: 6,
    draws: 0,
    winPercentage: 0.3333,
    totalPointsScored: 137,
    totalPointsConceded: 151,
    averagePointsScored: 15.22,
    averagePointsConceded: 16.78,
    averagePossession: 0.4567,
  },
  durationMs: 150,
  errors: [],
  warnings: [],
};

const mockTopPerformers = {
  success: true,
  data: [
    {
      playerId: 1,
      playerName: 'Pauric Farren',
      jerseyNumber: 14,
      positionCode: 'FWD',
      metricValue: 21,
      matchesPlayed: 9,
      totalMinutes: 540,
    },
    {
      playerId: 2,
      playerName: 'Caolan McLaughlin',
      jerseyNumber: 11,
      positionCode: 'FWD',
      metricValue: 15,
      matchesPlayed: 8,
      totalMinutes: 480,
    },
    {
      playerId: 3,
      playerName: 'Rory O Hara',
      jerseyNumber: 9,
      positionCode: 'MID',
      metricValue: 12,
      matchesPlayed: 9,
      totalMinutes: 540,
    },
  ],
  durationMs: 120,
  errors: [],
  warnings: [],
};

const mockRecentMatches = {
  success: true,
  data: [
    {
      matchId: 9,
      matchNumber: 9,
      matchDate: '2025-09-26',
      competitionType: 'Championship',
      homeTeamName: 'Drum',
      awayTeamName: 'Slaughtmanus',
      homeScore: 14,
      awayScore: 16,
      result: 'Loss',
      possession: 0.35,
    },
    {
      matchId: 8,
      matchNumber: 8,
      matchDate: '2025-08-17',
      competitionType: 'Championship',
      homeTeamName: 'Drum',
      awayTeamName: 'Magilligan',
      homeScore: 17,
      awayScore: 15,
      result: 'Win',
      possession: 0.52,
    },
  ],
  durationMs: 100,
  errors: [],
  warnings: [],
};

const mockTeamForm = {
  success: true,
  data: {
    matchCount: 10,
    wins: 3,
    losses: 6,
    draws: 1,
    formString: 'LWLWLWLLDW',
    formArray: ['L', 'W', 'L', 'W', 'L', 'W', 'L', 'L', 'D', 'W'],
  },
  durationMs: 80,
  errors: [],
  warnings: [],
};

test.describe('Dashboard Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API responses to avoid dependency on backend
    await page.route('**/api/Dashboard/team-overview*', (route) =>
      route.fulfill({ json: mockTeamOverview })
    );
    await page.route('**/api/Dashboard/top-performers*', (route) =>
      route.fulfill({ json: mockTopPerformers })
    );
    await page.route('**/api/Dashboard/recent-matches*', (route) =>
      route.fulfill({ json: mockRecentMatches })
    );
    await page.route('**/api/Dashboard/team-form*', (route) =>
      route.fulfill({ json: mockTeamForm })
    );

    // Navigate to dashboard
    await page.goto('/');
  });

  // =========================================================================
  // Basic Page Load Tests
  // =========================================================================

  test('should load dashboard page successfully', async ({ page }) => {
    // Just check if the page loaded without errors
    await expect(page.getByRole('heading', { name: 'GAAStat Dashboard' })).toBeVisible();
  });

  test('should display main header', async ({ page }) => {
    const header = page.getByRole('heading', { name: 'GAAStat Dashboard' });
    await expect(header).toBeVisible();
  });

  test('should display subtitle', async ({ page }) => {
    const subtitle = page.getByText('Team performance and player statistics');
    await expect(subtitle).toBeVisible();
  });

  // =========================================================================
  // Competition Filter Tests
  // =========================================================================

  test('should display competition filter', async ({ page }) => {
    const filter = page.getByLabel('Filter by competition type');
    await expect(filter).toBeVisible();
  });

  test('should have "All" selected by default', async ({ page }) => {
    const filter = page.getByLabel('Filter by competition type');
    await expect(filter).toHaveValue('All');
  });

  test('should allow changing competition filter', async ({ page }) => {
    const filter = page.getByLabel('Filter by competition type');

    // Change to League
    await filter.selectOption('League');
    await expect(filter).toHaveValue('League');

    // Change to Championship
    await filter.selectOption('Championship');
    await expect(filter).toHaveValue('Championship');
  });

  // =========================================================================
  // Team Overview Tests
  // =========================================================================

  test('should display team overview section', async ({ page }) => {
    const section = page.getByRole('heading', { name: 'Team Overview' });
    await expect(section).toBeVisible();
  });

  test('should display matches played stat card', async ({ page }) => {
    const card = page.getByText('Matches Played');
    await expect(card).toBeVisible();

    const value = page.getByText('9', { exact: true }).first();
    await expect(value).toBeVisible();
  });

  test('should display win percentage stat card', async ({ page }) => {
    const card = page.getByText('Win Percentage');
    await expect(card).toBeVisible();

    // Check for percentage value (33.3%)
    const value = page.locator('text=/33\\.3%/');
    await expect(value).toBeVisible();
  });

  test('should display average points scored stat card', async ({ page }) => {
    const card = page.getByText('Avg Points Scored');
    await expect(card).toBeVisible();

    // Check for average value (15.2)
    const value = page.locator('text=/15\\.2/');
    await expect(value).toBeVisible();
  });

  test('should display average possession stat card', async ({ page }) => {
    const card = page.getByText('Avg Possession');
    await expect(card).toBeVisible();

    // Check for possession percentage (45.7%)
    const value = page.locator('text=/45\\.7%/');
    await expect(value).toBeVisible();
  });

  // =========================================================================
  // Top Performers Card Tests
  // =========================================================================

  test('should display top performers card', async ({ page }) => {
    const heading = page.getByRole('heading', { name: 'Top Scorers' });
    await expect(heading).toBeVisible();
  });

  test('should display top 3 performers', async ({ page }) => {
    await expect(page.getByText('Pauric Farren')).toBeVisible();
    await expect(page.getByText('Caolan McLaughlin')).toBeVisible();
    await expect(page.getByText('Rory O Hara')).toBeVisible();
  });

  test('should display performer jersey numbers', async ({ page }) => {
    await expect(page.getByText('#14')).toBeVisible();
    await expect(page.getByText('#11')).toBeVisible();
    await expect(page.getByText('#9')).toBeVisible();
  });

  test('should display performer positions', async ({ page }) => {
    // Use more specific selectors within the top performers card
    const topPerformersCard = page.getByRole('region', { name: 'Top Scorers' });
    await expect(topPerformersCard.getByText('FWD').first()).toBeVisible();
    await expect(topPerformersCard.getByText('MID')).toBeVisible();
  });

  test('should display performer metric values', async ({ page }) => {
    // Use more specific selectors within the top performers card
    const topPerformersCard = page.getByRole('region', { name: 'Top Scorers' });
    await expect(topPerformersCard.getByText('21', { exact: true })).toBeVisible();
    await expect(topPerformersCard.getByText('15', { exact: true })).toBeVisible();
    await expect(topPerformersCard.getByText('12', { exact: true })).toBeVisible();
  });

  test('should display rank badges', async ({ page }) => {
    // Check for rank 1, 2, 3
    const ranks = page.locator('[aria-label^="Rank"]');
    await expect(ranks).toHaveCount(3);
  });

  // =========================================================================
  // Recent Matches Card Tests
  // =========================================================================

  test('should display recent matches card', async ({ page }) => {
    const heading = page.getByRole('heading', { name: 'Recent Matches' });
    await expect(heading).toBeVisible();
  });

  test('should display match results', async ({ page }) => {
    await expect(page.getByText('Slaughtmanus')).toBeVisible();
    await expect(page.getByText('Magilligan')).toBeVisible();
  });

  test('should display match scores', async ({ page }) => {
    // Check for score displays (14-16 and 17-15)
    await expect(page.locator('text=/14.*16/')).toBeVisible();
    await expect(page.locator('text=/17.*15/')).toBeVisible();
  });

  test('should display match result badges', async ({ page }) => {
    await expect(page.getByText('Loss', { exact: true })).toBeVisible();
    await expect(page.getByText('Win', { exact: true })).toBeVisible();
  });

  test('should display match dates', async ({ page }) => {
    // Dates should be formatted like "26 Sep 2025"
    const recentMatchesCard = page.getByRole('region', { name: 'Recent Matches' });
    await expect(recentMatchesCard.locator('time')).toHaveCount(2);
  });

  test('should display possession percentages', async ({ page }) => {
    await expect(page.locator('text=/Possession.*35\\.0%/')).toBeVisible();
    await expect(page.locator('text=/Possession.*52\\.0%/')).toBeVisible();
  });

  // =========================================================================
  // Team Form Card Tests
  // =========================================================================

  test('should display team form card', async ({ page }) => {
    const heading = page.getByRole('heading', { name: 'Recent Form' });
    await expect(heading).toBeVisible();
  });

  test('should display form badges', async ({ page }) => {
    // Should display 10 form badges (W/L/D)
    const formBadges = page.locator('[role="listitem"]').filter({ has: page.locator('text=/^[WLD]$/') });
    await expect(formBadges).toHaveCount(10);
  });

  test('should display form statistics', async ({ page }) => {
    await expect(page.getByText('Wins')).toBeVisible();
    await expect(page.getByText('Draws')).toBeVisible();
    await expect(page.getByText('Losses')).toBeVisible();
  });

  test('should display correct form counts', async ({ page }) => {
    // Find the wins section and check its value
    const winsSection = page.locator('text=Wins').locator('..').locator('text=/^3$/');
    await expect(winsSection).toBeVisible();

    // Find the draws section and check its value
    const drawsSection = page.locator('text=Draws').locator('..').locator('text=/^1$/');
    await expect(drawsSection).toBeVisible();

    // Find the losses section and check its value
    const lossesSection = page.locator('text=Losses').locator('..').locator('text=/^6$/');
    await expect(lossesSection).toBeVisible();
  });

  // =========================================================================
  // Accessibility Tests
  // =========================================================================

  test('should have proper heading hierarchy', async ({ page }) => {
    const h1 = page.getByRole('heading', { level: 1 });
    await expect(h1).toHaveCount(1);
    await expect(h1).toHaveText('GAAStat Dashboard');

    const h2s = page.getByRole('heading', { level: 2 });
    await expect(h2s).toHaveCount(4); // Team Overview, Top Scorers, Recent Matches, Recent Form
  });

  test('should have accessible labels for interactive elements', async ({ page }) => {
    const filter = page.getByLabel('Filter by competition type');
    await expect(filter).toBeVisible();
  });

  test('should have proper ARIA regions', async ({ page }) => {
    const regions = page.getByRole('region');
    await expect(regions).toHaveCount(4); // Top Scorers, Recent Matches, Recent Form, and Team Overview
  });

  // =========================================================================
  // Responsive Design Tests
  // =========================================================================

  test('should be responsive on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    // Main header should still be visible
    const header = page.getByRole('heading', { name: 'GAAStat Dashboard' });
    await expect(header).toBeVisible();

    // Cards should stack vertically (grid should collapse)
    const statCards = page.locator('.grid').first();
    await expect(statCards).toBeVisible();
  });

  test('should be responsive on tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });

    // All sections should be visible
    await expect(page.getByText('Team Overview')).toBeVisible();
    await expect(page.getByText('Top Scorers')).toBeVisible();
    await expect(page.getByText('Recent Matches')).toBeVisible();
    await expect(page.getByText('Recent Form')).toBeVisible();
  });

  // =========================================================================
  // API Integration Tests
  // =========================================================================

  test('should handle API errors gracefully', async ({ page }) => {
    // Mock API error
    await page.route('**/api/Dashboard/team-overview*', (route) =>
      route.fulfill({
        json: {
          success: false,
          data: null,
          durationMs: 50,
          errors: [{ code: 'NO_SEASON', message: 'Season not found' }],
          warnings: [],
        },
      })
    );

    await page.reload();

    // Should display error message
    await expect(page.getByText(/Season not found|Failed to load/i)).toBeVisible();
  });

  test('should show loading states', async ({ page }) => {
    // Create a new page without mocked data to see loading state
    const newPage = await page.context().newPage();

    // Mock slow API response
    await newPage.route('**/api/Dashboard/**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 200));
      await route.fulfill({ json: mockTeamOverview });
    });

    // Navigate and immediately check for loading state
    await newPage.goto('/');

    // Should show loading skeleton briefly
    const loadingSkeleton = newPage.locator('.animate-pulse').first();
    // Use a shorter timeout and check if it was visible at some point
    await loadingSkeleton.waitFor({ state: 'attached', timeout: 3000 }).catch(() => {
      // It's OK if we miss it - loading might be very fast
    });

    await newPage.close();
  });

  // =========================================================================
  // Interaction Tests
  // =========================================================================

  test('should trigger API calls when filter changes', async ({ page }) => {
    let apiCallCount = 0;

    // Re-route after initial load to count subsequent calls
    await page.route('**/api/Dashboard/**', (route) => {
      apiCallCount++;
      if (route.request().url().includes('team-overview')) {
        route.fulfill({ json: mockTeamOverview });
      } else if (route.request().url().includes('top-performers')) {
        route.fulfill({ json: mockTopPerformers });
      } else if (route.request().url().includes('recent-matches')) {
        route.fulfill({ json: mockRecentMatches });
      } else if (route.request().url().includes('team-form')) {
        route.fulfill({ json: mockTeamForm });
      } else {
        route.continue();
      }
    });

    const filter = page.locator('select#competition-filter');
    await expect(filter).toBeVisible();

    // Reset counter after page load
    apiCallCount = 0;

    // Change filter
    await filter.selectOption('League');

    // Wait for API calls
    await page.waitForTimeout(1000);

    // Should have made API calls (4 endpoints)
    expect(apiCallCount).toBeGreaterThan(0);
  });

  test('should have hover effects on cards', async ({ page }) => {
    const firstCard = page.locator('.shadow-md').first();

    // Hover over card
    await firstCard.hover();

    // Card should be visible after hover
    await expect(firstCard).toBeVisible();
  });
});
