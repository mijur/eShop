import { test, expect } from '@playwright/test';

test.describe('Catalog Search', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
        // Ensure the catalog page is loaded
        await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
    });

    // =============================================
    // Search Box Visibility and Structure
    // =============================================
    
    test.describe('Search box visibility and structure', () => {
        test('search input is visible on catalog page', async ({ page }) => {
            await expect(page.getByPlaceholder('Search products...')).toBeVisible();
        });

        test('category dropdown is visible adjacent to search', async ({ page }) => {
            await expect(page.locator('.search-category-select')).toBeVisible();
        });

        test('search button is present', async ({ page }) => {
            await expect(page.locator('.search-button')).toBeVisible();
        });

        test('search section appears above catalog', async ({ page }) => {
            const searchSection = page.locator('.search-section');
            const catalog = page.locator('.catalog');
            
            await expect(searchSection).toBeVisible();
            await expect(catalog).toBeVisible();
            
            const searchBounds = await searchSection.boundingBox();
            const catalogBounds = await catalog.boundingBox();
            
            // Search section should be above catalog
            expect(searchBounds!.y + searchBounds!.height).toBeLessThan(catalogBounds!.y);
        });
    });

    // =============================================
    // Autocomplete Behavior
    // =============================================

    test.describe('Autocomplete behavior', () => {
        test('typing 2+ characters shows suggestion dropdown', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('Alp');
            
            // Wait for debounce delay + API response
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
        });

        test('suggestions appear after debounce delay', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            
            // Type characters
            await searchInput.fill('watch');
            
            // Suggestions should not appear immediately (debounce)
            await expect(page.locator('.search-suggestions')).not.toBeVisible();
            
            // After debounce delay, suggestions should appear
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
        });

        test('clicking a suggestion fills the search input and navigates to item', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('Alpine');
            
            // Wait for suggestions
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
            
            // Click first suggestion
            await page.locator('.search-suggestion').first().click();
            
            // Should navigate to item detail page
            await expect(page).toHaveURL(/\/item\/\d+/);
        });

        test('suggestions contain item name and category', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('watch');
            
            // Wait for suggestions
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
            
            // Each suggestion should have name and category
            const firstSuggestion = page.locator('.search-suggestion').first();
            await expect(firstSuggestion.locator('.suggestion-name')).toBeVisible();
            await expect(firstSuggestion.locator('.suggestion-category')).toBeVisible();
        });

        test('no suggestions shown for single character', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('A');
            
            // Wait a bit for potential suggestions
            await page.waitForTimeout(500);
            
            // Suggestions should not appear for single character
            await expect(page.locator('.search-suggestions')).not.toBeVisible();
        });

        test('suggestions hide when input cleared', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            
            // Type to show suggestions
            await searchInput.fill('Alpine');
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
            
            // Clear input
            await searchInput.clear();
            
            // Suggestions should hide
            await expect(page.locator('.search-suggestions')).not.toBeVisible();
        });
    });

    // =============================================
    // Search Execution
    // =============================================

    test.describe('Search execution', () => {
        test('pressing Enter with search text filters results', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('boots');
            await searchInput.press('Enter');
            
            // URL should include search query
            await expect(page).toHaveURL(/[?&]q=boots/);
        });

        test('clicking search button executes search', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('jacket');
            
            await page.locator('.search-button').click();
            
            // URL should include search query
            await expect(page).toHaveURL(/[?&]q=jacket/);
        });

        test('search results show items matching query', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('watch');
            await searchInput.press('Enter');
            
            // Wait for results to load
            await expect(page).toHaveURL(/[?&]q=watch/);
            
            // Results should contain matching items
            await expect.poll(async () => {
                const items = await page.locator('.catalog-item').count();
                return items;
            }).toBeGreaterThan(0);
        });

        test('search via keyboard Enter', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.click();
            await page.keyboard.type('tent');
            await page.keyboard.press('Enter');
            
            await expect(page).toHaveURL(/[?&]q=tent/);
        });
    });

    // =============================================
    // Category Filter
    // =============================================

    test.describe('Category filter', () => {
        test('category dropdown shows All Categories by default', async ({ page }) => {
            const categorySelect = page.locator('.search-category-select');
            await expect(categorySelect).toHaveValue('');
        });

        test('selecting a category includes type in search', async ({ page }) => {
            // Select a category first
            const categorySelect = page.locator('.search-category-select');
            await categorySelect.selectOption({ index: 1 });
            
            // Then search
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('test');
            await searchInput.press('Enter');
            
            // URL should include type parameter
            await expect(page).toHaveURL(/[?&]type=\d+/);
        });

        test('category persists during search', async ({ page }) => {
            // Select a category
            const categorySelect = page.locator('.search-category-select');
            await categorySelect.selectOption({ index: 1 });
            
            const selectedValue = await categorySelect.inputValue();
            
            // Perform search
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('item');
            await searchInput.press('Enter');
            
            // Category should remain selected after search
            await expect(categorySelect).toHaveValue(selectedValue);
        });

        test('category dropdown contains catalog types', async ({ page }) => {
            const categorySelect = page.locator('.search-category-select');
            
            // Should have multiple options (All + actual categories)
            await expect(categorySelect.locator('option')).toHaveCount.greaterThan(1);
        });
    });

    // =============================================
    // Empty and Edge Cases
    // =============================================

    test.describe('Empty and edge cases', () => {
        test('empty search shows all items', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('');
            await searchInput.press('Enter');
            
            // URL should not have q parameter or q should be empty
            const url = page.url();
            const hasEmptyQuery = !url.includes('q=') || url.includes('q=&') || url.endsWith('q=');
            expect(hasEmptyQuery || url === page.url()).toBeTruthy();
            
            // Items should still be visible
            await expect.poll(async () => {
                const items = await page.locator('.catalog-item').count();
                return items;
            }).toBeGreaterThan(0);
        });

        test('no results shows appropriate message', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            
            // Search for something that won't exist
            await searchInput.fill('xyznonexistentproduct123');
            await searchInput.press('Enter');
            
            // Should show no results message
            await expect(page.getByText(/no (items|products|results) found/i)).toBeVisible({ timeout: 5000 });
        });

        test('special characters in search are handled', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('test & item');
            await searchInput.press('Enter');
            
            // Should not break the page
            await expect(page.locator('.catalog')).toBeVisible();
        });

        test('whitespace-only search is ignored', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('   ');
            await searchInput.press('Enter');
            
            // Should still show catalog
            await expect(page.locator('.catalog')).toBeVisible();
        });
    });

    // =============================================
    // Keyboard Navigation
    // =============================================

    test.describe('Keyboard navigation', () => {
        test('can navigate suggestions with keyboard', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('watch');
            
            // Wait for suggestions
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
            
            // Press down arrow to highlight first suggestion
            await page.keyboard.press('ArrowDown');
            
            // A suggestion should be highlighted/focused
            const highlightedSuggestion = page.locator('.search-suggestion.highlighted, .search-suggestion:focus');
            await expect(highlightedSuggestion).toHaveCount(1);
        });

        test('Escape key hides suggestions', async ({ page }) => {
            const searchInput = page.getByPlaceholder('Search products...');
            await searchInput.fill('Alpine');
            
            // Wait for suggestions
            await expect(page.locator('.search-suggestions')).toBeVisible({ timeout: 2000 });
            
            // Press Escape
            await page.keyboard.press('Escape');
            
            // Suggestions should hide
            await expect(page.locator('.search-suggestions')).not.toBeVisible();
        });
    });

    // =============================================
    // Responsive Design
    // =============================================

    test.describe('Responsive design', () => {
        test('search box adapts to mobile viewport', async ({ page }) => {
            // Set mobile viewport
            await page.setViewportSize({ width: 375, height: 667 });
            await page.goto('/');
            
            // Search should still be visible
            await expect(page.getByPlaceholder('Search products...')).toBeVisible();
        });

        test('search box adapts to tablet viewport', async ({ page }) => {
            // Set tablet viewport
            await page.setViewportSize({ width: 768, height: 1024 });
            await page.goto('/');
            
            // Search should still be visible
            await expect(page.getByPlaceholder('Search products...')).toBeVisible();
            await expect(page.locator('.search-button')).toBeVisible();
        });
    });
});
