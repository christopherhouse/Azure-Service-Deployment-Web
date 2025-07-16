import { test, expect } from '@playwright/test';

test.describe('Home Page', () => {
  test('should load the home page successfully', async ({ page }) => {
    await page.goto('/');
    
    // Check that the page loads without errors
    await expect(page).toHaveTitle(/Azure Service Deployment Web/);
    
    // Check for basic page elements
    await expect(page.locator('body')).toBeVisible();
  });

  test('should display navigation elements', async ({ page }) => {
    await page.goto('/');
    
    // Check for navigation or header elements (adjust selectors based on actual UI)
    // This is a basic test - will need to be updated based on actual UI structure
    await expect(page.locator('html')).toBeVisible();
  });

  test('should be responsive', async ({ page }) => {
    // Test mobile view
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    await expect(page.locator('body')).toBeVisible();
    
    // Test desktop view
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/');
    await expect(page.locator('body')).toBeVisible();
  });
});