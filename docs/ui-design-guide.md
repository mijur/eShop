# eShop WebApp - UI Design Guide

**Version:** 1.0  
**Last Updated:** January 30, 2026  
**Target:** AI Agents implementing UI features

This guide documents the visual design system, layout patterns, and component placement rules for the eShop WebApp. **Follow this guide strictly when implementing UI features.**

---

## Table of Contents

1. [Layout Architecture](#layout-architecture)
2. [Page Structure](#page-structure)
3. [Spacing & Padding System](#spacing--padding-system)
4. [Component Placement Patterns](#component-placement-patterns)
5. [CSS Conventions](#css-conventions)
6. [Responsive Design](#responsive-design)
7. [Typography](#typography)
8. [Color System](#color-system)
9. [Interactive Elements](#interactive-elements)
10. [Common Mistakes](#common-mistakes)

---

## Layout Architecture

### Master Layout

**File:** [src/WebApp/Components/Layout/MainLayout.razor](../src/WebApp/Components/Layout/MainLayout.razor)

```
┌─────────────────────────────────────────┐
│           HeaderBar                     │
│  (includes page-header sections)        │
├─────────────────────────────────────────┤
│                                         │
│           @Body                         │
│     (Page content goes here)            │
│                                         │
├─────────────────────────────────────────┤
│           FooterBar                     │
└─────────────────────────────────────────┘
```

**Key Points:**
- HeaderBar contains navigation and page-specific headers via `SectionOutlet`
- Body renders page-specific content
- Maximum layout width: **120rem** (1920px)
- All pages render within this structure

---

## Page Structure

### Anatomy of a Page

Every page follows this pattern:

```razor
@page "/page-url"
@inject Services
@attribute [Authorize] <!-- if auth required -->

<!-- 1. PAGE METADATA -->
<PageTitle>Page Title | AdventureWorks</PageTitle>
<SectionContent SectionName="page-header-title">Page Title</SectionContent>
<SectionContent SectionName="page-header-subtitle">Subtitle text</SectionContent>

<!-- 2. PAGE CONTENT CONTAINER -->
<div class='page-specific-container'>
    <!-- Content here -->
</div>

@code {
    // Component logic
}
```

### SectionContent Usage

**`SectionName="page-header-title"`**
- Appears in HeaderBar as `<h1>` element
- Text: Bold, large (3.5rem on desktop)
- Color: Black (#000)
- Location: Bottom-left of header hero image

**`SectionName="page-header-subtitle"`**
- Appears below title as `<p>` element  
- Text: Bold, medium (2rem on desktop)
- Color: Black (#000)
- Location: Below title, same alignment

**Examples:**
- [Catalog.razor](../src/WebApp/Components/Pages/Catalog/Catalog.razor#L7-L8)
- [CartPage.razor](../src/WebApp/Components/Pages/Cart/CartPage.razor#L9)
- [Checkout.razor](../src/WebApp/Components/Pages/Checkout/Checkout.razor#L9)

---

## Spacing & Padding System

### Horizontal Padding Rules

**Desktop (>1024px):**
```css
.page-container {
    padding: 0 10rem;  /* Standard */
}
```

**Tablet (481px - 1024px):**
```css
.page-container {
    padding: 0 3rem;
}
```

**Mobile (≤480px):**
```css
.page-container {
    padding: 0 1rem;
}
```

### Common Page Containers

| Container Class | Used By | Layout Type | Padding Pattern |
|----------------|---------|-------------|-----------------|
| `.catalog` | Catalog page | Flex (2-column) | 10rem / 3rem / 1rem |
| `.cart` | Cart page | Flex (2-column) | 10rem / 3rem / 1rem |
| `.checkout` | Checkout page | Block (single-column) | 10rem / 3rem / 1rem |

**Pattern:** Most pages use `0 10rem` on desktop, reduce to `0 3rem` tablet, `0 1rem` mobile.

### Gaps Between Elements

```css
/* Large sections */
gap: 6rem;     /* Between major layout columns (catalog filters + items) */

/* Medium sections */
gap: 2.5rem;   /* Between item groups */
gap: 2rem;     /* Between form sections */

/* Small sections */
gap: 1.5rem;   /* Within component groups */
gap: 1rem;     /* Between related items */
gap: 0.5rem;   /* Between tightly-coupled elements */
```

---

## Component Placement Patterns

### ⚠️ CRITICAL: Where to Place Components

#### Search/Filter Components

**❌ WRONG:**
```razor
<!-- DO NOT place inside layout containers -->
<div class="catalog">
    <SearchBox />          <!-- ❌ Breaks flex layout! -->
    <CatalogSearch />      <!-- Filter sidebar -->
    <div>Items</div>
</div>
```

**✅ CORRECT:**
```razor
<!-- Option 1: Dedicated section above content (RECOMMENDED) -->
<SectionContent SectionName="page-header-title">Title</SectionContent>
<SectionContent SectionName="page-header-subtitle">Subtitle</SectionContent>

<div class="search-section">  <!-- New semantic section -->
    <SearchBox />
</div>

<div class="catalog">
    <CatalogSearch />  <!-- Filter sidebar -->
    <div>Items</div>
</div>

<!-- Option 2: Within filter sidebar -->
<div class="catalog">
    <CatalogSearch>
        <SearchBox />  <!-- At top of filter component -->
    </CatalogSearch>
    <div>Items</div>
</div>
```

**CSS for search-section:**
```css
.search-section {
    padding: 2rem 10rem;
    margin-bottom: 2rem;
}

@media only screen and (max-width: 480px) {
    .search-section { padding: 1rem; }
}
```

#### Filter/Sidebar Components

**Location:** First child in flex container  
**Example:** [CatalogSearch.razor](../src/WebAppComponents/Catalog/CatalogSearch.razor)

```razor
<div class="catalog">  <!-- Flex container -->
    <CatalogSearch />  <!-- ✅ Sidebar (flex child 1) -->
    <div>Items</div>   <!-- ✅ Main content (flex child 2) -->
</div>
```

**Properties:**
```css
.catalog-search {
    flex-shrink: 0;
    width: 14rem;  /* Fixed width on desktop */
}
```

#### Form Components

**Location:** Inside single-column container  
**Example:** [Checkout.razor](../src/WebApp/Components/Pages/Checkout/Checkout.razor)

```razor
<div class="checkout">
    <EditForm>
        <div class="form">
            <div class="form-section">
                <!-- Form fields -->
            </div>
        </div>
    </EditForm>
</div>
```

#### Summary/Action Panels

**Location:** Second column in two-column flex layouts  
**Examples:** Cart summary, product details panel

```razor
<div class="page-container">
    <div class="main-content">Items</div>
    <div class="summary-panel">Summary</div>  <!-- Right side -->
</div>
```

---

## CSS Conventions

### File Organization

**Scoped CSS:** One `.razor.css` file per component
```
Component.razor
Component.razor.css  ← Scoped to Component only
```

**Global CSS:** [src/WebApp/wwwroot/css/app.css](../src/WebApp/wwwroot/css/app.css)

### Naming Conventions

**Class Names:**
- Use **kebab-case**: `.catalog-search-box`
- Prefix with component name: `.catalog-*`, `.cart-*`, `.checkout-*`
- Descriptive, not presentational: `.search-suggestions` not `.dropdown-list`

**Layout Classes:**
```css
.catalog              /* Page container */
.catalog-items        /* Item grid */
.catalog-search       /* Filter sidebar */
.catalog-search-group /* Filter section */
```

### Layout Techniques

**Two-Column Layouts:**
```css
.container {
    display: flex;
    gap: 6rem;
}

.sidebar {
    flex-shrink: 0;
    width: 14rem;  /* Fixed width */
}

.main {
    flex-grow: 1;  /* Fill remaining space */
}
```

**Grid Layouts:**
```css
.item-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 2.5rem;
}
```

---

## Responsive Design

### Breakpoints

```css
/* Mobile: 0 - 480px */
@media only screen and (max-width: 480px) { }

/* Tablet: 481px - 1024px */
@media only screen and (min-width: 481px) and (max-width: 1024px) { }

/* Desktop: 1025px+ */
/* Default styles */
```

### Common Responsive Patterns

#### 1. Padding Reduction
```css
.container {
    padding: 0 10rem;  /* Desktop */
}

@media only screen and (max-width: 480px) {
    .container { padding: 0 1rem; }
}

@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .container { padding: 0 3rem; }
}
```

#### 2. Flex Direction Change
```css
.container {
    display: flex;
    flex-direction: row;  /* Desktop: side-by-side */
}

@media only screen and (max-width: 1024px) {
    .container {
        flex-direction: column;  /* Mobile/Tablet: stacked */
    }
}
```

**Example:** [Catalog.razor.css](../src/WebApp/Components/Pages/Catalog/Catalog.razor.css#L90-L98)

#### 3. Width Adjustments
```css
.sidebar {
    width: 14rem;  /* Desktop: fixed */
}

@media only screen and (max-width: 1024px) {
    .sidebar {
        width: 100%;  /* Mobile/Tablet: full-width */
    }
}
```

---

## Typography

### Font Families

**Primary:** Plus Jakarta Sans  
**Secondary:** Open Sans

```css
body {
    font-family: 'Plus Jakarta Sans';
}

/* Open Sans used for some UI elements */
.catalog-search-tag {
    font-family: 'Open Sans';
}
```

### Font Sizes

| Element | Desktop | Mobile | Weight |
|---------|---------|--------|--------|
| Page Title (`<h1>`) | 3.5rem | 2rem | 700 |
| Page Subtitle | 2rem | 1.5rem | 700 |
| Section Heading (`<h2>`) | 1.25rem | - | 600 |
| Filter Heading (`<h3>`) | 1rem | - | 600 |
| Body Text | 1rem | - | 400 |
| Small Text | 0.875rem | - | 400 |

### Line Heights

```css
line-height: 100%;  /* Tight (titles) */
line-height: 120%;  /* Compact (headings) */
line-height: 125%;  /* Medium (subtitles) */
line-height: 140%;  /* Standard (section headings) */
line-height: 150%;  /* Relaxed (body text) */
```

---

## Color System

### Primary Colors

```css
--color-black: #000;
--color-white: #FFF;
```

### Grays

```css
--color-gray-light: #F7F7F7;    /* Backgrounds */
--color-gray-medium: #D2D2D2;   /* Borders */
--color-gray-dark: #404040;     /* Inactive text */
--color-gray-darker: #444;      /* Labels */
```

### Usage

| Element | Color | Usage |
|---------|-------|-------|
| Primary button | Black bg, White text | CTAs, submit buttons |
| Secondary button | White bg, Black border | Cancel, back actions |
| Active filter tag | Black bg, White text | Selected state |
| Inactive filter tag | Transparent, Gray text | Default state |
| Borders | #D2D2D2 or #404040 | Separators, dividers |
| Backgrounds | #F7F7F7 | Panels, summaries |

### Validation Colors

```css
--color-success: #26b050;  /* Valid input outline */
--color-error: red;        /* Invalid input outline, error text */
```

---

## Interactive Elements

### Buttons

**Primary Button:**
```css
.button.button-primary {
    background: #000;
    color: #FFF;
    padding: 1rem 0.75rem;
    border: none;
}
```

**Secondary Button:**
```css
.button.button-secondary {
    border: 1px solid #444;
    background: #FFF;
    color: #000;
    padding: 1rem 0.75rem;
}
```

**Example:** [app.css](../src/WebApp/wwwroot/css/app.css#L230-L242)

### Filter Tags/Pills

```css
.catalog-search-tag {
    display: flex;
    padding: 0.5rem 0.75rem;
    border-radius: 1.25rem;
    color: #404040;
    text-decoration: none;
}

.catalog-search-tag:hover {
    background: #ddd;
    cursor: pointer;
}

.catalog-search-tag.active {
    background: #000;
    color: #FFF;
}
```

**Example:** [CatalogSearch.razor.css](../src/WebAppComponents/Catalog/CatalogSearch.razor.css#L37-L58)

### Badges

```css
.filter-badge {
    background: #000;
    color: #FFF;
    font-size: 1rem;
    font-weight: 600;
    border-radius: 0.75rem;
    width: 1.5rem;
    height: 1.5rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}
```

Used for: Item counts in filters, cart quantities

### Form Inputs

```css
input[type="text"],
input[type="number"] {
    border: 1px solid #000;
    background: #FFF;
    color: #000;
    font-size: 1rem;
    padding: 0.5rem;
    width: calc(100% - 1rem);
}

/* Validation states */
.valid.modified:not([type=checkbox]) {
    outline: 1px solid #26b050;
}

.invalid {
    outline: 1px solid red;
}
```

**Example:** [Checkout.razor.css](../src/WebApp/Components/Pages/Checkout/Checkout.razor.css#L37-L47)

---

## Common Mistakes

### ❌ DO NOT

1. **Place components inside layout containers without understanding the layout system**
   ```razor
   <!-- ❌ WRONG: Breaks two-column flex layout -->
   <div class="catalog">
       <SearchBox />
       <CatalogSearch />
       <div>Items</div>
   </div>
   ```

2. **Use inconsistent padding/spacing**
   ```css
   /* ❌ WRONG: Custom padding values */
   .my-component {
       padding: 0 8rem;  /* Should be 10rem, 3rem, or 1rem */
   }
   ```

3. **Forget responsive breakpoints**
   ```css
   /* ❌ WRONG: Desktop-only styles */
   .component {
       width: 800px;
       padding: 0 10rem;
   }
   /* Missing mobile/tablet overrides! */
   ```

4. **Ignore semantic HTML structure**
   ```razor
   <!-- ❌ WRONG: Generic divs everywhere -->
   <div><div><div>Content</div></div></div>
   
   <!-- ✅ CORRECT: Semantic elements -->
   <section><header><h2>Title</h2></header><article>Content</article></section>
   ```

5. **Create one-off color values**
   ```css
   /* ❌ WRONG: Random colors */
   color: #333333;
   background: #F5F5F5;
   
   /* ✅ CORRECT: Use design system colors */
   color: #444;       /* Existing gray */
   background: #F7F7F7;  /* Existing background */
   ```

6. **Nest components inside flex children incorrectly**
   ```razor
   <!-- ❌ WRONG: Adding extra flex children -->
   <div class="two-column-layout">
       <div>Column 1</div>
       <div>Column 2</div>
       <NewComponent />  <!-- ❌ Creates unwanted 3rd column -->
   </div>
   ```

### ✅ DO

1. **Understand existing layout structure before adding components**
   - Inspect parent container CSS (flex, grid, block)
   - Check if existing children rely on specific layout
   - Place new components in semantic locations

2. **Create dedicated semantic sections for new features**
   ```razor
   <!-- ✅ CORRECT: New section above content -->
   <div class="feature-section">
       <NewComponent />
   </div>
   
   <div class="catalog">
       <!-- Existing layout unchanged -->
   </div>
   ```

3. **Follow responsive patterns consistently**
   ```css
   .my-component {
       padding: 0 10rem;
   }
   
   @media only screen and (max-width: 480px) {
       .my-component { padding: 0 1rem; }
   }
   
   @media only screen and (min-width: 481px) and (max-width: 1024px) {
       .my-component { padding: 0 3rem; }
   }
   ```

4. **Use design system values**
   - Spacing: `0.5rem`, `1rem`, `1.5rem`, `2rem`, `2.5rem`, `6rem`, `10rem`
   - Colors: `#000`, `#FFF`, `#F7F7F7`, `#D2D2D2`, `#404040`, `#444`
   - Font sizes: `1rem`, `1.25rem`, `2rem`, `3.5rem`

5. **Test on all breakpoints**
   - Desktop (1920px)
   - Tablet (768px)
   - Mobile (375px)

---

## Component Checklist

Before marking a UI implementation complete, verify:

- [ ] Component placed in correct semantic location (not disrupting existing layouts)
- [ ] CSS uses scoped `.razor.css` file
- [ ] Responsive styles defined for all 3 breakpoints
- [ ] Padding follows standard pattern (10rem / 3rem / 1rem)
- [ ] Spacing uses standard gap values
- [ ] Colors from design system palette
- [ ] Typography matches existing patterns
- [ ] Interactive states defined (hover, active, focus)
- [ ] Accessibility attributes present (aria-label, role, etc.)
- [ ] Component doesn't break existing page layout

---

## Reference Examples

### Well-Implemented Components

| Component | Pattern | Reference File |
|-----------|---------|----------------|
| Filter Sidebar | Flex child, fixed width | [CatalogSearch.razor](../src/WebAppComponents/Catalog/CatalogSearch.razor) |
| Two-Column Layout | Flex container | [Catalog.razor](../src/WebApp/Components/Pages/Catalog/Catalog.razor) |
| Summary Panel | Flex child, sidebar | [CartPage.razor](../src/WebApp/Components/Pages/Cart/CartPage.razor) |
| Form Layout | Single-column block | [Checkout.razor](../src/WebApp/Components/Pages/Checkout/Checkout.razor) |
| Item Grid | Flex wrap | [Catalog.razor.css](../src/WebApp/Components/Pages/Catalog/Catalog.razor.css#L56-L62) |

### Layout Patterns by Page

| Page | Layout Type | Columns | Responsive Behavior |
|------|-------------|---------|---------------------|
| Catalog (`/`) | Flex | 2 (sidebar + grid) | Stack on mobile |
| Cart (`/cart`) | Flex | 2 (items + summary) | Reverse stack on mobile |
| Checkout (`/checkout`) | Block | 1 (form) | Full-width all screens |
| Item Detail | Mixed | Varies | Responsive images |

---

## Quick Reference: Where to Place Things

| Component Type | Placement | CSS Pattern | Example |
|---------------|-----------|-------------|---------|
| **Search Box** | Dedicated section above content | `padding: 0 10rem` | N/A (see postmortem) |
| **Filter Sidebar** | First child in flex container | `flex-shrink: 0; width: 14rem` | CatalogSearch |
| **Primary Content** | Second child in flex container | `flex-grow: 1` | Catalog items |
| **Summary Panel** | Last child in flex container | `flex-shrink: 0` | Cart summary |
| **Header Content** | `SectionContent` elements | N/A (renders in HeaderBar) | All pages |
| **Forms** | Single-column block container | `padding: 0 10rem` | Checkout |
| **Item Grids** | Flex wrap container | `display: flex; flex-wrap: wrap` | Catalog items |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 30, 2026 | Initial design guide created from existing WebApp |

---

**Related Documents:**
- [Placement Postmortem](../features/search/placement-postmortem.md) - Search box placement lessons learned
- [eShop Coding Instructions](../.github/copilot-instructions.md) - Architecture and patterns

**For questions or clarifications, refer to existing component implementations in the codebase.**
