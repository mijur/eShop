---
name: test-runner
description: Run .NET tests in the eShop solution. Use when you need to run all tests, a specific test project, or a specific test by name.
argument-hint: '[project-or-filter]'
allowed-tools: Bash(dotnet test:*), Glob, Read
---

Run tests in the eShop solution based on the provided arguments.

## Usage

- `/test-runner` — run all tests in the solution
- `/test-runner Ordering.UnitTests` — run a specific test project
- `/test-runner Ordering.FunctionalTests` — run functional tests (requires Docker)
- `/test-runner MethodName` — run tests matching a name filter

## Execution

Determine what the user wants to test from `$ARGUMENTS`:

1. **No arguments** — run the full solution:
   ```bash
   dotnet test eShop.slnx --verbosity normal
   ```

2. **Argument matches a test project name** (contains "Tests") — find and run that project:
   ```bash
   dotnet test tests/$ARGUMENTS/ --verbosity normal
   ```

3. **Otherwise** — treat as a test name filter and run against the full solution:
   ```bash
   dotnet test eShop.slnx --filter "FullyQualifiedName~$ARGUMENTS" --verbosity normal
   ```

## Output

After running, provide a concise summary:
- Total tests: passed / failed / skipped
- If any tests failed, show the failure details (test name, error message, stack trace)
- Keep it brief — don't repeat passing test names
