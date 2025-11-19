# Unit Testing Setup Guide

This guide will help you set up and run the unit tests for the Backend Technical Assets Management system.

## Prerequisites

- .NET 8 SDK installed
- Git installed
- Visual Studio 2022, VS Code, or Rider (optional but recommended)

## Project Structure

The solution consists of two projects:
- **BackendTechnicalAssetsManagement**: Main API project
- **BackendTechnicalAssetsManagementTest**: Unit test project (separate repository)

## Setup Instructions

### 1. Clone the Main Repository

```bash
git clone <main-repository-url>
cd BackendTechnicalAssetsManagement
```

### 2. Clone the Test Repository

The test project is maintained in a separate repository and should be cloned as a sibling directory to the main project:

```bash
# Navigate to the parent directory
cd ..

# Clone the test repository
git clone <test-repository-url> BackendTechnicalAssetsManagementTest

# Your directory structure should now look like:
# parent-folder/
# ├── BackendTechnicalAssetsManagement/
# └── BackendTechnicalAssetsManagementTest/
```

### 3. Restore NuGet Packages

Navigate back to the main project and restore dependencies:

```bash
cd BackendTechnicalAssetsManagement
dotnet restore
```

### 4. Build the Solution

Build both projects to ensure everything compiles:

```bash
dotnet build
```

## Running Tests

### Option 1: Using .NET CLI (Recommended)

Run all tests from the solution directory:

```bash
dotnet test
```

Run tests with detailed output:

```bash
dotnet test --verbosity detailed
```

Run tests with code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Option 2: Using Visual Studio

1. Open `BackendTechnicalAssetsManagement.sln` in Visual Studio
2. Open **Test Explorer** (Test > Test Explorer)
3. Click **Run All** to execute all tests
4. View results in the Test Explorer window

### Option 3: Using VS Code

1. Install the **C# Dev Kit** extension
2. Open the solution folder in VS Code
3. Use the Testing sidebar to discover and run tests
4. Or use the terminal: `dotnet test`

### Option 4: Using Rider

1. Open `BackendTechnicalAssetsManagement.sln` in Rider
2. Open **Unit Tests** window (View > Tool Windows > Unit Tests)
3. Click **Run All** to execute all tests

## Test Project Details

### Testing Framework & Libraries

The test project uses:
- **xUnit**: Testing framework
- **Moq**: Mocking library for dependencies
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing
- **coverlet.collector**: Code coverage collection

### Current Test Coverage

The test suite includes comprehensive tests for:

#### UserService Tests (`UserServiceTests.cs`)
- **GetUserProfileByIdAsync**: Tests for Student, Teacher, Staff profiles and non-existent users
- **GetAllUsersAsync**: Tests for retrieving all users and empty lists
- **GetUserByIdAsync**: Tests for valid and invalid user IDs
- **UpdateUserProfileAsync**: Tests for successful updates and non-existent users
- **UpdateStudentProfileAsync**: Tests for student-specific updates and validation
- **UpdateStaffOrAdminProfileAsync**: Tests for authorization rules (Admin updating Staff, SuperAdmin updating Admin, users updating own profiles, unauthorized access)
- **DeleteUserAsync**: Tests for successful archiving and failure scenarios

## Troubleshooting

### Issue: Test project not found

**Error**: `The project file 'BackendTechnicalAssetsManagementTest.csproj' does not exist`

**Solution**: Ensure the test repository is cloned in the correct location (as a sibling directory to the main project).

### Issue: Build fails with missing references

**Solution**: Run `dotnet restore` in both project directories:

```bash
cd BackendTechnicalAssetsManagement
dotnet restore

cd ../BackendTechnicalAssetsManagementTest
dotnet restore
```

### Issue: Tests fail due to missing dependencies

**Solution**: Ensure all NuGet packages are restored and the main project builds successfully before running tests.

## Writing New Tests

### Test Structure

Follow the Arrange-Act-Assert (AAA) pattern:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange: Set up test data and mocks
    var userId = Guid.NewGuid();
    _mockRepository.Setup(x => x.GetByIdAsync(userId))
        .ReturnsAsync(mockData);

    // Act: Execute the method being tested
    var result = await _service.MethodName(userId);

    // Assert: Verify the results
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result.Property);
    _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
}
```

### Naming Conventions

- Test class: `{ServiceName}Tests`
- Test method: `{MethodName}_{Scenario}_{ExpectedBehavior}`
- Use descriptive names that explain what's being tested

### Mock Setup

Use Moq to create mock dependencies:

```csharp
private readonly Mock<IUserRepository> _mockRepository;

public UserServiceTests()
{
    _mockRepository = new Mock<IUserRepository>();
    _service = new UserService(_mockRepository.Object);
}
```

## Continuous Integration

To integrate tests into your CI/CD pipeline:

```yaml
# Example GitHub Actions workflow
- name: Run tests
  run: dotnet test --no-build --verbosity normal

- name: Generate coverage report
  run: dotnet test --collect:"XPlat Code Coverage"
```

## Best Practices

1. **Keep tests isolated**: Each test should be independent and not rely on other tests
2. **Use meaningful test data**: Create mock data that represents realistic scenarios
3. **Test edge cases**: Include tests for null values, empty collections, and boundary conditions
4. **Verify mock interactions**: Use `Verify()` to ensure methods are called with expected parameters
5. **Keep tests fast**: Use in-memory databases and mocks instead of real dependencies
6. **Organize tests**: Group related tests using regions or nested classes

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## Support

If you encounter issues with the test setup:
1. Verify your directory structure matches the expected layout
2. Ensure all dependencies are restored (`dotnet restore`)
3. Check that you're using .NET 8 SDK
4. Review the test output for specific error messages
