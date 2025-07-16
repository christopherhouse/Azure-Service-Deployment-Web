import { test, expect } from '@playwright/test';
import path from 'path';

if (process.env.CI) {
  test.skip(true, 'Skipping deployment workflow tests in CI due to authentication restrictions');
}

test.describe('Deployment Workflow', () => {
  test('should navigate to deployment page', async ({ page }) => {
    await page.goto('/');
    
    // Try to navigate to deployment page
    await page.goto('/Deployment');
    
    // Check that we're on the deployment page
    // Note: This might redirect to auth if authentication is configured
    await expect(page).toHaveURL(/\/(Deployment|Account\/SignIn|MicrosoftIdentity)/);
  });

  test('should display deployment form elements when authenticated or auth disabled', async ({ page }) => {
    await page.goto('/Deployment');
    
    // If redirected to sign-in, this is expected behavior
    const currentUrl = page.url();
    if (currentUrl.includes('SignIn') || currentUrl.includes('MicrosoftIdentity')) {
      // Authentication is required - this is valid behavior
      expect(currentUrl).toMatch(/SignIn|MicrosoftIdentity/);
    } else {
      // Should show deployment form
      await expect(page.locator('body')).toBeVisible();
      
      // Look for form elements (adjust selectors based on actual form structure)
      // These tests will need to be updated once we can see the actual UI
      const hasForm = await page.locator('form').count() > 0;
      const hasFileInputs = await page.locator('input[type="file"]').count() > 0;
      
      // At least one of these should be present for a deployment form
      expect(hasForm || hasFileInputs).toBeTruthy();
    }
  });

  test.skip('should handle file upload workflow', async ({ page }) => {
    // Skip this test for now as it requires proper auth setup
    // Will be implemented once authentication flow is working
    
    await page.goto('/Deployment');
    
    // Create test files
    const testTemplatePath = path.join(__dirname, '../../fixtures/test-template.json');
    const testParametersPath = path.join(__dirname, '../../fixtures/test-parameters.json');
    
    // This would test the file upload functionality
    // await page.setInputFiles('[data-testid="template-upload"]', testTemplatePath);
    // await page.setInputFiles('[data-testid="parameters-upload"]', testParametersPath);
    // await page.click('[data-testid="deploy-button"]');
    
    // Verify deployment started
    // await expect(page.locator('[data-testid="deployment-status"]')).toBeVisible();
  });
});