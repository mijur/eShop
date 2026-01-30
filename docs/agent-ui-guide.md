# Agent Instructions: Using the UI Design Guide

**Audience:** AI Agents (planner, frontend-implement, frontend-test)  
**Purpose:** How to use the UI Design Guide when implementing features  
**Reference:** [UI Design Guide](ui-design-guide.md)

---

## For Planning Agents

When creating implementation plans for UI features:

### 1. Include UI Integration Details Section

**REQUIRED in every task that involves UI placement:**

```markdown
#### UI Integration Details
- **Placement**: Exact DOM location (e.g., "dedicated section between SectionContent and .catalog, NOT inside .catalog")
- **Container**: Semantic element + CSS class (e.g., `<div class="search-section">`)
- **Layout Impact**: Explain how component fits existing layout
  - "Does not affect existing .catalog flex layout (new section above)"
  - "Third child in .cart flex container"
  - "Replaces existing X component"
- **Responsive**: Mobile/tablet/desktop behavior
  - Desktop: Full-width above content
  - Tablet: Same as desktop
  - Mobile: Same as desktop with reduced padding
- **CSS Pattern**: Which padding/spacing pattern to use
  - Standard page padding: `0 10rem` → `0 3rem` → `0 1rem`
  - Component-specific spacing: document explicitly
- **Similar Examples**: Point to existing components with similar placement
  - "Similar to CatalogSearch.razor placement in sidebar"
  - "Similar to cart summary panel"
```

### 2. Check Layout Compatibility BEFORE Planning

**Steps:**
1. Read the target page's `.razor` and `.razor.css` files
2. Identify the layout system:
   - Flex container? → How many columns? What are they?
   - Grid? → How's it structured?
   - Block? → Can add sections freely?
3. Determine if adding a child will break the layout
4. Document in the plan how to avoid disruption

**Example Analysis:**
```markdown
**Current Catalog.razor Layout:**
- Container: `.catalog` (flex, 2 columns)
- Column 1: `CatalogSearch` (filter sidebar, flex-shrink: 0, 14rem)
- Column 2: Item grid (flex-grow: 1)
- Gap: 6rem

**Implication for Search Box:**
- ❌ Cannot add as 3rd child in .catalog (breaks 2-column layout)
- ✅ Must add as separate section above .catalog
- ✅ Alternative: nest inside CatalogSearch at top
```

### 3. Reference Design Guide Sections

In task descriptions, link to relevant design guide sections:

```markdown
**Implementation Requirements:**
- Follow [Component Placement Patterns](ui-design-guide.md#component-placement-patterns)
- Use [Standard Padding Pattern](ui-design-guide.md#spacing--padding-system)
- Implement [Responsive Breakpoints](ui-design-guide.md#responsive-design)
```

### 4. Provide Visual Context

Since agents can't see screenshots, describe the layout:

```markdown
**Visual Structure:**
```
Current:
┌─────────────────────────────┐
│ Header (with SectionContent)│
├─────────────────────────────┤
│ ┌─────────┬────────────────┐│
│ │ Filters │  Item Grid     ││ ← .catalog flex container
│ │ (14rem) │  (flex-grow)   ││
│ └─────────┴────────────────┘│
└─────────────────────────────┘

After adding search:
┌─────────────────────────────┐
│ Header (with SectionContent)│
├─────────────────────────────┤
│ ┌─────────────────────────┐ │ ← NEW: .search-section
│ │   Search Box            │ │
│ └─────────────────────────┘ │
├─────────────────────────────┤
│ ┌─────────┬────────────────┐│
│ │ Filters │  Item Grid     ││ ← Existing .catalog (unchanged)
│ │ (14rem) │  (flex-grow)   ││
│ └─────────┴────────────────┘│
└─────────────────────────────┘
```
```

---

## For Frontend Implementation Agents

### Before Writing Code

**Step 1: Read the Design Guide**

Mandatory reading sections:
- [Layout Architecture](ui-design-guide.md#layout-architecture)
- [Component Placement Patterns](ui-design-guide.md#component-placement-patterns)
- [CSS Conventions](ui-design-guide.md#css-conventions)
- [Responsive Design](ui-design-guide.md#responsive-design)

**Step 2: Inspect Existing Page Structure**

```bash
# Read the target page
Read: src/WebApp/Components/Pages/{Page}/{Page}.razor
Read: src/WebApp/Components/Pages/{Page}/{Page}.razor.css

# Check parent layout CSS
Look for: display: flex | grid | block
Check: How many children? What are their roles?
```

**Step 3: Follow the Checklist**

From [Component Checklist](ui-design-guide.md#component-checklist):
- [ ] Correct semantic location identified
- [ ] Won't disrupt existing layout
- [ ] CSS scoped to `.razor.css` file
- [ ] Responsive styles planned for 3 breakpoints
- [ ] Using design system values (colors, spacing, typography)

### During Implementation

**CSS File Creation:**

```css
/* Component.razor.css */

/* 1. Desktop styles (default) */
.component-name {
    padding: 0 10rem;  /* Standard pattern */
    display: flex;
    gap: 1.5rem;       /* From design system */
}

.component-element {
    color: #000;       /* From design system */
    font-size: 1rem;   /* From design system */
    padding: 0.5rem 0.75rem;  /* From design system */
}

/* 2. Tablet breakpoint */
@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .component-name {
        padding: 0 3rem;
    }
}

/* 3. Mobile breakpoint */
@media only screen and (max-width: 480px) {
    .component-name {
        padding: 0 1rem;
        flex-direction: column;  /* If needed */
    }
}
```

**Razor Component Structure:**

```razor
@inject Services
@rendermode InteractiveServer  <!-- If interactive -->

<!-- Wrapper div with CSS class -->
<div class="component-name">
    <!-- Component content -->
</div>

@code {
    // Logic
}
```

**Placement in Page:**

```razor
<!-- CORRECT PATTERNS -->

<!-- Pattern A: New section above content -->
<SectionContent SectionName="page-header-title">Title</SectionContent>
<SectionContent SectionName="page-header-subtitle">Subtitle</SectionContent>

<div class="feature-section">  <!-- NEW -->
    <YourComponent />
</div>

<div class="existing-container">
    <!-- Existing layout unchanged -->
</div>

<!-- Pattern B: Inside existing container (only if appropriate) -->
<div class="existing-container">
    <ExistingChild1 />
    <YourComponent />  <!-- Add as designed -->
    <ExistingChild2 />
</div>
```

### After Implementation

**Validation Steps:**

1. **Build Check:**
   ```bash
   dotnet build eShop.slnx
   ```

2. **Visual Inspection:** (if app is running)
   - Desktop (1920px): Does layout look correct?
   - Tablet (768px): Does it adapt properly?
   - Mobile (375px): Is it usable?

3. **Layout Integrity:**
   - Existing page elements still in correct positions?
   - No unintended column additions?
   - Responsive behavior works?

4. **Design System Compliance:**
   - Colors from palette?
   - Spacing from standard values?
   - Typography matches?

---

## For Frontend Test Agents

### E2E Tests: Beyond Functionality

When writing Playwright tests, include layout assertions:

**Functional Tests (Always Required):**
```typescript
test('search box is visible', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByPlaceholder('Search...')).toBeVisible();
});
```

**Layout Tests (Add for complex placements):**
```typescript
test('search box is positioned correctly relative to catalog', async ({ page }) => {
    await page.goto('/');
    
    const searchBox = page.locator('.search-section');
    const catalogDiv = page.locator('.catalog');
    
    const searchBounds = await searchBox.boundingBox();
    const catalogBounds = await catalogDiv.boundingBox();
    
    // Verify search box is ABOVE catalog, not inside it
    expect(searchBounds!.y + searchBounds!.height).toBeLessThan(catalogBounds!.y);
});

test('search box spans expected width', async ({ page }) => {
    await page.goto('/');
    await page.setViewportSize({ width: 1920, height: 1080 });
    
    const searchBox = page.locator('.search-section');
    const width = await searchBox.evaluate(el => el.offsetWidth);
    
    // With 10rem padding on each side (320px total), ~1600px content width
    expect(width).toBeGreaterThan(1500);
});
```

**Responsive Tests:**
```typescript
test('component adapts to mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    
    // Check mobile-specific behavior
    const searchBox = page.locator('.search-section');
    // Padding should be 1rem (16px) on mobile
    const padding = await searchBox.evaluate(el => 
        window.getComputedStyle(el).paddingLeft
    );
    expect(padding).toBe('16px');  // 1rem = 16px
});
```

---

## Common Scenarios & Solutions

### Scenario 1: Adding a Search Feature

**Question:** Where should the search box go?

**Answer:**
1. **Check:** Is there a global header/nav that makes sense? → Use that
2. **If no:** Create dedicated section above main content
3. **Never:** Add as child in existing two-column layout

**Implementation:**
```markdown
**Location:** Dedicated section between header and catalog

**Structure:**
```razor
<div class="search-section">
    <SearchBox />
</div>
```

**CSS:**
```css
.search-section {
    padding: 0 10rem;
    margin-bottom: 2rem;
}
```
```

### Scenario 2: Adding a Filter to Sidebar

**Question:** Where in the sidebar?

**Answer:**
- Add as child of existing filter component (CatalogSearch)
- OR add as sibling section below existing filters
- Maintain flex-shrink: 0 and fixed width

**Implementation:**
```razor
<!-- Inside CatalogSearch.razor -->
<div class="catalog-search">
    <div class="catalog-search-header">Filters</div>
    <ExistingFilterGroup />
    <NewFilterGroup />  <!-- Add here -->
</div>
```

### Scenario 3: Adding Modal/Overlay

**Question:** Where in the DOM?

**Answer:**
- Add at root level (body child) via portal/teleport
- OR add inside page but with fixed/absolute positioning
- Never nest deeply in component tree

**Implementation:**
```razor
@page "/"
<!-- Page content -->

@if (showModal)
{
    <div class="modal-overlay">
        <div class="modal-content">
            <!-- Modal content -->
        </div>
    </div>
}
```

### Scenario 4: Two-Column Form

**Question:** Should I use flex or grid?

**Answer:**
- For simple side-by-side: flex with `.form-group` pattern
- Follow existing [Checkout.razor](../src/WebApp/Components/Pages/Checkout/Checkout.razor) pattern

**Implementation:**
```razor
<div class="form-group">
    <div class="form-group-item">
        <label>Field 1</label>
    </div>
    <div class="form-group-item">
        <label>Field 2</label>
    </div>
</div>
```

```css
.form-group {
    display: flex;
    gap: 1.5rem;
}

.form-group-item {
    flex: 1 0 0;  /* Equal width columns */
}
```

---

## Decision Tree: Component Placement

```
START: Need to add UI component
│
├─ Is it part of page header (title/subtitle)?
│  └─ YES → Use SectionContent with "page-header-title" or "page-header-subtitle"
│  └─ NO → Continue
│
├─ Is it a filter/sidebar component?
│  └─ YES → First child in flex container (e.g., .catalog)
│  └─ NO → Continue
│
├─ Does page have two-column flex layout?
│  ├─ YES → DON'T add as 3rd child!
│  │       → Create new section above container OR
│  │       → Nest inside existing child
│  └─ NO → Continue
│
├─ Is it a form?
│  └─ YES → Use single-column .form container pattern
│  └─ NO → Continue
│
├─ Is it a summary/action panel?
│  └─ YES → Last child in flex container (right side)
│  └─ NO → Continue
│
└─ Default: Create dedicated semantic section
   └─ <div class="feature-section">
       └─ Follow standard padding pattern
       └─ Add responsive breakpoints
```

---

## Red Flags: Stop and Review

If you encounter any of these, **STOP** and review the design guide:

🚩 **Adding 3rd child to two-column flex container**
   → Will break layout! Create new section instead.

🚩 **Using custom padding values (e.g., `0 8rem`)**
   → Should be 10rem, 3rem, or 1rem. Use standard pattern.

🚩 **No responsive breakpoints defined**
   → All pages need mobile/tablet styles. Add them.

🚩 **Placing component deep in nested structure without understanding parent layout**
   → Read parent CSS first. Understand flex/grid/block behavior.

🚩 **Creating color values not in design system**
   → Use #000, #FFF, #F7F7F7, #D2D2D2, #404040, #444 only.

🚩 **Not using scoped CSS (`.razor.css`)**
   → Create scoped file. Don't add to global app.css.

---

## Summary Checklist for Agents

### Planning Phase
- [ ] Read UI Design Guide before creating plan
- [ ] Inspect target page structure
- [ ] Include UI Integration Details in task
- [ ] Document layout compatibility analysis
- [ ] Provide visual structure diagram
- [ ] Link to relevant design guide sections

### Implementation Phase
- [ ] Read design guide sections for component type
- [ ] Inspect existing page .razor and .css
- [ ] Identify semantic placement location
- [ ] Create scoped .razor.css file
- [ ] Use design system values (colors, spacing, typography)
- [ ] Implement 3 responsive breakpoints
- [ ] Verify build succeeds

### Testing Phase
- [ ] Write functional E2E tests
- [ ] Add layout assertions for complex placements
- [ ] Test on all 3 breakpoints
- [ ] Verify existing layout unaffected

---

**Related Documents:**
- [UI Design Guide](ui-design-guide.md) - Complete design system reference
- [Placement Postmortem](../features/search/placement-postmortem.md) - Lessons learned
- [eShop Coding Instructions](../.github/copilot-instructions.md) - Architecture patterns

**Questions?** Reference existing components in the codebase for real-world examples.
