# UI Implementation Quick Reference Card

**For AI Agents** - Keep this handy when implementing UI features

---

## Before You Start

☑️ Read: [UI Design Guide](ui-design-guide.md) - Layout Architecture & Component Placement  
☑️ Read: [Agent UI Guide](agent-ui-guide.md) - Your role-specific instructions  
☑️ Inspect: Target page `.razor` and `.razor.css` files

---

## The Golden Rules

### 1. Never Disrupt Existing Layouts
```razor
<!-- ❌ WRONG: Adding 3rd child to 2-column flex -->
<div class="catalog">
    <Sidebar />
    <Content />
    <YourComponent />  <!-- Breaks layout! -->
</div>

<!-- ✅ CORRECT: New section above -->
<div class="your-section">
    <YourComponent />
</div>
<div class="catalog">
    <Sidebar />
    <Content />
</div>
```

### 2. Use Standard Padding Pattern
```css
.your-component {
    padding: 0 10rem;  /* Desktop */
}

@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .your-component { padding: 0 3rem; }  /* Tablet */
}

@media only screen and (max-width: 480px) {
    .your-component { padding: 0 1rem; }  /* Mobile */
}
```

### 3. Use Design System Values Only

**Colors:**
- `#000` (black), `#FFF` (white)
- `#F7F7F7` (light gray bg), `#D2D2D2` (border)
- `#404040` (inactive text), `#444` (labels)

**Spacing:**
- `0.5rem`, `1rem`, `1.5rem`, `2rem`, `2.5rem`, `6rem`, `10rem`

**Font sizes:**
- `1rem` (body), `1.25rem` (h2), `2rem` (subtitle), `3.5rem` (h1)

---

## Decision Tree

```
Need to add component?
│
├─ Page header/title? → SectionContent
│
├─ Filter/sidebar? → First child in flex container
│
├─ Page has 2-column layout? → Create new section (don't add 3rd child!)
│
├─ Form? → Single-column .form pattern
│
├─ Summary panel? → Last child in flex container
│
└─ Else → Dedicated semantic section
```

---

## Component Checklist

- [ ] Correct semantic location (not inside flex if breaks layout)
- [ ] Scoped `.razor.css` file created
- [ ] 3 responsive breakpoints (mobile/tablet/desktop)
- [ ] Standard padding (10rem → 3rem → 1rem)
- [ ] Design system colors/spacing/typography
- [ ] `@rendermode InteractiveServer` if has events
- [ ] Accessibility attributes (aria-label, role)
- [ ] Build succeeds: `dotnet build eShop.slnx`

---

## Common Patterns

### Two-Column Layout
```razor
<div class="page-container">
    <div class="sidebar">Filters</div>
    <div class="content">Items</div>
</div>
```

```css
.page-container {
    display: flex;
    gap: 6rem;
    padding: 0 10rem;
}

.sidebar {
    flex-shrink: 0;
    width: 14rem;
}

.content {
    flex-grow: 1;
}
```

### Button Styles
```razor
<button class="button button-primary">Submit</button>
<button class="button button-secondary">Cancel</button>
```

### Filter Tags
```razor
<a href="@Url" class="catalog-search-tag @(IsActive ? "active" : "")">
    Filter Name
</a>
```

---

## Red Flags 🚩

**STOP if you see:**
- Adding child to flex container without checking column count
- Custom padding values (not 10rem/3rem/1rem)
- No responsive breakpoints
- Colors not in design system (#333, #F5F5F5, etc.)
- Not using scoped CSS

**→ Review [UI Design Guide](ui-design-guide.md#common-mistakes)**

---

## Examples to Reference

| Component Type | Example File |
|---------------|--------------|
| Filter Sidebar | [CatalogSearch.razor](../src/WebAppComponents/Catalog/CatalogSearch.razor) |
| Two-Column Layout | [Catalog.razor](../src/WebApp/Components/Pages/Catalog/Catalog.razor) |
| Form | [Checkout.razor](../src/WebApp/Components/Pages/Checkout/Checkout.razor) |
| Summary Panel | [CartPage.razor](../src/WebApp/Components/Pages/Cart/CartPage.razor) |

---

## Quick CSS Template

```css
/* your-component.razor.css */

/* Desktop (default) */
.your-component {
    padding: 0 10rem;
    display: flex;
    gap: 1.5rem;
}

.your-element {
    color: #000;
    font-size: 1rem;
    padding: 0.5rem 0.75rem;
}

/* Tablet */
@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .your-component {
        padding: 0 3rem;
    }
}

/* Mobile */
@media only screen and (max-width: 480px) {
    .your-component {
        padding: 0 1rem;
        flex-direction: column;
    }
}
```

---

**Full Details:** [UI Design Guide](ui-design-guide.md) | [Agent UI Guide](agent-ui-guide.md)
