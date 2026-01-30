---
name: frontend-test
description: Writes frontend tests (E2E with Playwright, MAUI unit tests). TDD "Red" phase.
tools: ['execute/runInTerminal', 'read/readFile', 'edit/createFile', 'edit/editFiles', 'search']
---
# Frontend Test Agent

Write tests for frontend features. Tests should fail initially (TDD Red phase).

## Test Types

| Type | Framework | Location | Use For |
|------|-----------|----------|---------|
| E2E | Playwright (TS) | `e2e/` | User workflows in browser |
| MAUI Unit | MSTest | `tests/ClientApp.UnitTests/` | ViewModels, services |

## E2E Tests (Playwright)

### Basic Pattern
```typescript
// e2e/AddItemTest.spec.ts
test('Add item to cart', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' }))
    .toBeVisible();

  await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();

  // Use polling for async updates
  await expect.poll(() => page.getByLabel('product quantity').count())
    .toBeGreaterThan(0);
});
```

### Key Conventions
- **Selectors**: Prefer semantic (`getByRole`, `getByLabel`, `getByText`)
- **Async**: Use `expect.poll()` for async UI updates
- **Auth**: Tests requiring login depend on `setup` project (see `playwright.config.ts`)
- **Credentials**: Use `process.env.USERNAME1`, `process.env.PASSWORD`

### Auth Setup
```typescript
// e2e/login.setup.ts
setup('Login', async ({ page }) => {
  await page.goto('/');
  await page.getByLabel('Sign in').click();
  await page.getByPlaceholder('Username').fill(process.env.USERNAME1!);
  await page.getByPlaceholder('Password').fill(process.env.PASSWORD!);
  await page.getByRole('button', { name: 'Login' }).click();
  await page.context().storageState({ path: STORAGE_STATE });
});
```

## MAUI Unit Tests

### ViewModel Test Pattern
```csharp
[TestClass]
public class CatalogViewModelTests
{
    private readonly IAppEnvironmentService _env;

    public CatalogViewModelTests()
    {
        _env = new AppEnvironmentService(
            new BasketMockService(), ..., new CatalogMockService(), ...);
        _env.UpdateDependencies(useMockServices: true);
    }

    [TestMethod]
    public async Task Products_populated_after_init()
    {
        var vm = new CatalogViewModel(_env, _nav);
        await vm.InitializeAsync();
        Assert.IsNotNull(vm.Products);
    }

    [TestMethod]
    public async Task PropertyChanged_raised_for_BadgeCount()
    {
        bool raised = false;
        var vm = new CatalogViewModel(_env, _nav);
        vm.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(vm.BadgeCount)) raised = true;
        };
        await vm.InitializeAsync();
        Assert.IsTrue(raised);
    }
}
```

### Key Conventions
- **Manual mocks**: Use mock service implementations (no NSubstitute)
- **Async**: Add `Task.Delay(10)` in mock methods to simulate latency
- **Commands**: Use `ExecuteUntilComplete()` helper for async commands
- **Messaging**: Test `WeakReferenceMessenger` pub/sub

## Layout Validation (E2E)

For UI components, verify placement:
```typescript
const search = page.locator('.search-section');
const catalog = page.locator('.catalog');
const searchBounds = await search.boundingBox();
const catalogBounds = await catalog.boundingBox();
expect(searchBounds!.y + searchBounds!.height).toBeLessThan(catalogBounds!.y);
```

## Workflow

1. Read findings and plan (check UI Integration Details for placement)
2. Write tests for current task
3. Verify tests compile/run (should fail)
4. Report: "Tests ready. Failing as expected. Ready for implementation."
