# Test Generation Rules

When generating tests, always match the existing conventions in the codebase:

## Unit Tests
- Use **MSTest.Sdk** (not xUnit or NUnit)
- Use **NSubstitute** for mocking (not Moq or FakeItEasy)
- **Never use FluentAssertions** — use built-in MSTest assertions (`Assert.AreEqual`, `Assert.IsTrue`, etc.)
- Place unit tests in the corresponding `tests/*UnitTests/` project
- Use test **Builders** (e.g., `OrderBuilder`, `AddressBuilder`) for constructing test data instead of raw constructors
- Parallelize at the method level

## Functional Tests
- Use **xUnit v3** via Microsoft.Testing.Platform
- Use Aspire-based fixtures for real container infrastructure
- Place functional tests in `tests/*FunctionalTests/`
- These require Docker running

## General
- Follow existing naming conventions in the test project you're adding to
- Always use PowerShell for any test scripts
