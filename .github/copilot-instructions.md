# GitHub Copilot Instructions for Azure Service Deployment Web

## Project Overview

This is a full-stack Azure Service Deployment Web application that enables deployment of Azure resources through multiple interfaces. The solution consists of:

- **.NET 8 MVC Backend**: API and web interface for Azure resource management (`src/dotnet/AzureDeploymentWeb/`)
- **Azure Authentication**: Microsoft Entra ID (Azure AD) with OAuth 2.0 auth code flow
- **Infrastructure as Code**: Azure Bicep templates for deployment (`infra/bicep/`)
- **CI/CD Pipeline**: GitHub Actions workflow for automated deployment

## Development Process Guidelines

### 1. Code Structure and Organization

- Follow the existing project structure:
  ```
  src/dotnet/AzureDeploymentWeb/
  ├── Controllers/           # MVC Controllers
  ├── Models/               # Data models and view models
  ├── Services/             # Business logic and Azure services
  ├── Views/                # Razor views
  ├── Hubs/                 # SignalR hubs
  ├── wwwroot/              # Static files
  ├── Extensions.cs         # Extension methods
  └── Program.cs            # Application entry point
  ```

- Use dependency injection for services
- Follow ASP.NET Core MVC conventions
- Implement proper separation of concerns

### 2. Coding Standards

- Use C# nullable reference types (enabled in project)
- Follow Microsoft C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling and logging
- Use Application Insights for telemetry
- Follow SOLID principles

### 3. Azure Integration

- Use Azure SDK packages (Azure.Identity, Azure.ResourceManager)
- Implement proper credential management with DefaultAzureCredential
- Use Azure SignalR for real-time updates
- Leverage Azure Application Insights for monitoring

## XUnit Testing Guidelines

### Setup and Configuration

When adding XUnit tests for .NET code, follow these guidelines:

#### 1. Test Project Structure

Create test projects following this pattern:
```
src/dotnet/AzureDeploymentWeb.Tests/
├── Controllers/          # Controller unit tests
├── Services/            # Service unit tests  
├── Models/              # Model tests
├── Integration/         # Integration tests
├── Fixtures/           # Test fixtures and helpers
└── AzureDeploymentWeb.Tests.csproj
```

#### 2. Test Project Configuration

Create `AzureDeploymentWeb.Tests.csproj` with these packages:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.0" />
    <PackageReference Include="xunit" Version="2.4.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../AzureDeploymentWeb/AzureDeploymentWeb.csproj" />
  </ItemGroup>
</Project>
```

#### 3. Testing Best Practices

- **Unit Tests**: Test individual components in isolation
  ```csharp
  [Fact]
  public async Task DeploymentService_ShouldCreateDeployment_WhenValidTemplateProvided()
  {
      // Arrange
      var mockCredential = new Mock<DefaultAzureCredential>();
      var service = new DeploymentService(mockCredential.Object);
      
      // Act
      var result = await service.CreateDeploymentAsync(validTemplate);
      
      // Assert
      result.Should().NotBeNull();
      result.Status.Should().Be(DeploymentStatus.Running);
  }
  ```

- **Integration Tests**: Test API endpoints and full workflows
  ```csharp
  public class DeploymentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
  {
      [Fact]
      public async Task PostDeployment_ShouldReturn200_WhenAuthenticatedUser()
      {
          // Test implementation
      }
  }
  ```

- **Controller Tests**: Mock dependencies and test MVC logic
- **Service Tests**: Mock Azure SDK clients and test business logic
- **Model Tests**: Validate data models and validation attributes

#### 4. Test Categories and Organization

- Use `[Trait]` attributes to categorize tests:
  ```csharp
  [Fact]
  [Trait("Category", "Unit")]
  public void ModelValidation_ShouldFail_WhenRequiredFieldMissing() { }
  
  [Fact]
  [Trait("Category", "Integration")]
  public void ApiEndpoint_ShouldReturnData_WhenValidRequest() { }
  ```

#### 5. Azure Service Testing

- Mock Azure SDK clients using interfaces
- Use test doubles for external dependencies
- Create test fixtures for common Azure scenarios
- Test error handling and retry logic

## Playwright UI Testing Guidelines

### Setup and Configuration

#### 1. Playwright Project Structure

Create Playwright tests in this structure:
```
tests/playwright/
├── tests/
│   ├── auth/            # Authentication flow tests
│   ├── deployment/      # Deployment workflow tests
│   ├── ui/              # UI component tests
│   └── e2e/             # End-to-end scenarios
├── fixtures/            # Test fixtures and data
├── utils/               # Test utilities and helpers
├── playwright.config.ts # Playwright configuration
└── package.json        # Node.js dependencies
```

#### 2. Package.json Setup

```json
{
  "name": "azure-deployment-web-e2e-tests",
  "version": "1.0.0",
  "description": "Playwright E2E tests for Azure Service Deployment Web",
  "scripts": {
    "test": "playwright test",
    "test:headed": "playwright test --headed",
    "test:debug": "playwright test --debug",
    "test:ui": "playwright test --ui",
    "report": "playwright show-report"
  },
  "devDependencies": {
    "@playwright/test": "^1.40.0",
    "@types/node": "^20.0.0",
    "typescript": "^5.0.0"
  }
}
```

#### 3. Playwright Configuration

Create `playwright.config.ts`:
```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],

  webServer: {
    command: 'dotnet run --project ../src/dotnet/AzureDeploymentWeb',
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
  },
});
```

#### 4. Test Implementation Guidelines

- **Page Object Pattern**: Create page objects for reusable interactions
  ```typescript
  export class DeploymentPage {
    constructor(private page: Page) {}
    
    async uploadTemplate(filePath: string) {
      await this.page.setInputFiles('[data-testid="template-upload"]', filePath);
    }
    
    async startDeployment() {
      await this.page.click('[data-testid="deploy-button"]');
    }
  }
  ```

- **Authentication Tests**: Test Azure AD login flows
- **File Upload Tests**: Test ARM template and parameter file uploads
- **Deployment Workflow Tests**: End-to-end deployment scenarios
- **Error Handling Tests**: Test error states and user feedback

#### 5. CI/CD Integration

Add Playwright to GitHub Actions workflow:
```yaml
- name: Install Playwright
  run: |
    cd tests/playwright
    npm ci
    npx playwright install --with-deps

- name: Run Playwright tests
  run: |
    cd tests/playwright
    npm run test
  env:
    BASE_URL: ${{ steps.webapp-deploy.outputs.webapp-url }}
```

## README Update Requirements

### When Making Changes

Always update the README.md when making significant changes:

#### 1. New Features
- Add feature descriptions to the "🚀 Features" section
- Update setup instructions if new dependencies are added
- Add usage examples for new functionality

#### 2. Dependencies
- Update prerequisites section for new tools or versions
- Modify installation instructions for new packages
- Update environment variable documentation

#### 3. Testing
- Add or update the "🧪 Testing" section with:
  ```markdown
  ## 🧪 Testing

  ### Unit Tests
  Run .NET unit tests:
  ```bash
  dotnet test src/dotnet/AzureDeploymentWeb.Tests/
  ```

  ### E2E Tests
  Run Playwright tests:
  ```bash
  cd tests/playwright
  npm test
  ```
  ```

#### 4. Project Structure
- Update the project structure diagram when adding new directories
- Document new configuration files or important scripts

#### 5. Troubleshooting
- Add common issues and solutions
- Update environment setup troubleshooting
- Document test-specific troubleshooting steps

## CI/CD Integration Notes

### GitHub Actions Workflow Updates

When adding tests, update `.github/workflows/deploy.yml`:

#### 1. Add Test Steps to Build Job
```yaml
- name: Run Unit Tests
  run: dotnet test ./src/dotnet/AzureDeploymentWeb.Tests/ --configuration Release --logger trx --results-directory TestResults

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: success() || failure()
  with:
    name: .NET Tests
    path: TestResults/*.trx
    reporter: dotnet-trx
```

#### 2. Add Playwright Testing Job
```yaml
playwright-tests:
  needs: deploy
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: '18'
    - name: Install dependencies
      run: |
        cd tests/playwright
        npm ci
    - name: Install Playwright Browsers
      run: |
        cd tests/playwright
        npx playwright install --with-deps
    - name: Run Playwright tests
      run: |
        cd tests/playwright
        npm run test
      env:
        BASE_URL: ${{ needs.deploy.outputs.webapp-url }}
```

### Environment Variables for Testing

Add test-specific environment variables:
- `TEST_USER_EMAIL`: Test Azure AD user email
- `TEST_USER_PASSWORD`: Test Azure AD user password (use GitHub secrets)
- `TEST_SUBSCRIPTION_ID`: Azure subscription for testing
- `TEST_RESOURCE_GROUP`: Test resource group name

## Error Handling and Logging

### Logging Guidelines
- Use structured logging with Application Insights
- Log deployment operations and their outcomes
- Include correlation IDs for request tracing
- Log authentication and authorization events

### Error Handling Patterns
- Use try-catch blocks for Azure SDK operations
- Implement retry logic for transient failures
- Provide meaningful error messages to users
- Log errors with sufficient context for debugging

## Security Considerations

### Authentication and Authorization
- Validate Azure AD tokens properly
- Implement proper permission checks for Azure operations
- Use least privilege principle for Azure service principal
- Secure storage of sensitive configuration

### Input Validation
- Validate ARM template uploads
- Sanitize user inputs
- Implement file type and size restrictions
- Validate Azure resource parameters

## Performance Considerations

- Use async/await for all I/O operations
- Implement proper caching strategies
- Use SignalR for real-time updates instead of polling
- Optimize Azure SDK client usage with proper disposal

Remember: Always write tests before implementing new features, follow the existing code patterns, and update documentation with any changes made to the application.