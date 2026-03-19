# Accessibility Audit Report
## Hotel Reservation Management System
**Date:** 2026-03-20  
**Standard:** WCAG 2.1 Level AA  
**Auditor:** Automated + Manual Review  
**Scope:** All Razor views, shared layouts, guest portal, calendar, dashboard, notifications

---

## Executive Summary

The application has a solid accessibility baseline: skip-to-content link, semantic landmark roles, `aria-hidden` on decorative icons, and Bootstrap 5's built-in focus management for modals. However, **15 issues** were identified that prevent full WCAG 2.1 AA conformance. These are prioritised below and mapped to the tasks that will resolve them (14.2, 14.3, 14.4).

| Severity | Count |
|----------|-------|
| Critical | 2     |
| High     | 6     |
| Medium   | 5     |
| Low      | 2     |

---

## Automated Testing Approach

### Tools Used / Recommended

| Tool | Type | How to Run |
|------|------|------------|
| **axe-core 4.8.2** | Automated (browser) | Drop-in script — see §Dev Helper below |
| **WAVE** | Automated (browser ext) | [wave.webaim.org](https://wave.webaim.org) |
| **Lighthouse** | Automated (DevTools) | Chrome DevTools → Lighthouse → Accessibility |
| **NVDA + Firefox** | Screen reader (Windows) | Manual — free download |
| **JAWS + Chrome** | Screen reader (Windows) | Manual — licensed |
| **VoiceOver + Safari** | Screen reader (macOS/iOS) | Built-in — Cmd+F5 |
| **Keyboard-only** | Manual | Tab, Shift+Tab, Enter, Space, Arrow keys |

### axe-core Dev Helper Script

Add the block below to `_Layout.cshtml` just before `</body>` (already added as a comment — uncomment locally):

```html
<!-- ACCESSIBILITY TESTING: Uncomment below for local axe-core audit (remove before production)
<script src="https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.8.2/axe.min.js"></script>
<script>
    window.addEventListener('load', function() {
        axe.run().then(results => {
            if (results.violations.length) {
                console.group('%c♿ axe-core Accessibility Violations (' + results.violations.length + ')', 'color:red;font-weight:bold');
                results.violations.forEach(v => {
                    console.group('%c[' + v.impact.toUpperCase() + '] ' + v.id + ': ' + v.description, 'color:orange');
                    v.nodes.forEach(n => console.log(n.html));
                    console.groupEnd();
                });
                console.groupEnd();
            } else {
                console.log('%c♿ No axe-core violations found!', 'color:green;font-weight:bold');
            }
        });
    });
</script>
-->
```

---

## Issue Register

### CRITICAL

---

#### A-001 — Incorrect ARIA roles on navbar (`role="menubar"` / `role="menuitem"`)
- **File:** `Views/Shared/_Layout.cshtml`
- **Element:** `<ul class="navbar-nav" role="menubar">` and `<li role="menuitem">`
- **WCAG Criterion:** 4.1.2 Name, Role, Value (Level A)
- **Impact:** Screen readers announce the navigation as an application menu (like a desktop app menu bar), causing severe confusion. Users expect `menubar` to respond to arrow-key navigation, which is not implemented.
- **Fix (Task 14.2):** Remove `role="menubar"` and `role="menuitem"`. The `<nav aria-label="Main Navigation">` wrapper already provides the correct semantic. `<ul>` / `<li>` / `<a>` inside a `<nav>` are sufficient per ARIA Authoring Practices.
- **Effort:** Low — attribute removal only.

---

#### A-002 — Form inputs in Calendar modals lack `aria-describedby` for validation errors
- **File:** `Views/Home/Calendar.cshtml` — `#reservationForm`
- **Elements:** `#guestFirstName`, `#guestLastName`, `#reservationHotel`, `#reservationRoom`, `#checkInDate`, `#checkOutDate`, `#numberOfGuests`, `#totalAmount`
- **WCAG Criterion:** 1.3.1 Info and Relationships (A), 3.3.1 Error Identification (A), 3.3.3 Error Suggestion (AA)
- **Impact:** When validation fires, error messages are injected dynamically but are not programmatically associated with their inputs. Screen reader users cannot discover which field has an error or what the error message says.
- **Fix (Task 14.2 / 14.4):** Add `aria-describedby="fieldId-error"` to each required input; inject `<span id="fieldId-error" class="invalid-feedback" role="alert">` alongside each field. Set `aria-invalid="true"` on the input when validation fails.
- **Effort:** Medium.

---

### HIGH

---

#### A-003 — Success alert missing `aria-live` region
- **File:** `Views/Shared/_Layout.cshtml` — `#successAlert`
- **WCAG Criterion:** 4.1.3 Status Messages (AA)
- **Impact:** Success confirmations (e.g., "Reservation saved") are shown visually but never announced to screen readers. Users relying on AT receive no feedback after form submissions.
- **Fix (Task 14.2):** Add `aria-live="polite"` and `aria-atomic="true"` to `#successAlert`. The existing `#errorAlert` already has `aria-live="assertive"` — apply the same pattern.
- **Effort:** Trivial.

---

#### A-004 — Notification badge count changes not announced
- **File:** `Views/Shared/_Layout.cshtml` — `#notification-badge`
- **WCAG Criterion:** 4.1.3 Status Messages (AA)
- **Impact:** When new notifications arrive via SignalR, the badge count updates silently. Screen reader users are unaware of new notifications.
- **Fix (Task 14.2):** Add `aria-live="polite"` and `aria-label` that includes the count (e.g., "3 unread notifications") to `#notification-badge`. Update the label dynamically in `notifications.js` when the count changes.
- **Effort:** Low.

---

#### A-005 — Notification filter tabs missing ARIA tab pattern
- **File:** `Views/Shared/_Layout.cshtml` — `#notif-filter-tabs`
- **WCAG Criterion:** 4.1.2 Name, Role, Value (A)
- **Impact:** The filter buttons (All / Unread / Reservations) look like tabs but have no `role="tablist"` / `role="tab"` / `aria-selected` semantics. Screen readers announce them as plain buttons with no indication of selection state.
- **Fix (Task 14.2):** Add `role="tablist"` to `#notif-filter-tabs`, `role="tab"` to each button, `aria-selected="true/false"`, and `tabindex="0/-1"` for roving tabindex. Add `role="tabpanel"` with `aria-labelledby` to the notifications list.
- **Effort:** Low–Medium.

---

#### A-006 — Mobile bottom nav active state not announced
- **File:** `Views/Shared/_Layout.cshtml` — `.mobile-bottom-nav`
- **WCAG Criterion:** 4.1.2 Name, Role, Value (A)
- **Impact:** The currently active page link in the mobile bottom nav has no `aria-current="page"` attribute. Screen reader users cannot tell which section is active.
- **Fix (Task 14.2 / 14.3):** Add `aria-current="page"` to the active `<a>` element. Set it dynamically in `mobile.js` based on the current URL path.
- **Effort:** Low.

---

#### A-007 — Guest portal mobile nav missing `aria-current`
- **File:** `Views/Shared/_GuestPortalLayout.cshtml`
- **WCAG Criterion:** 4.1.2 Name, Role, Value (A)
- **Impact:** Same issue as A-006 but in the guest portal layout. Active navigation link is not announced.
- **Fix (Task 14.2):** Add `aria-current="page"` to the active nav link. Can be set server-side via Razor using `ViewContext.RouteData`.
- **Effort:** Low.

---

#### A-008 — Loading overlay missing `aria-busy` on main content
- **File:** `Views/Shared/_Layout.cshtml` — `#loadingOverlay` / `#main-content`
- **WCAG Criterion:** 4.1.3 Status Messages (AA), 4.1.2 Name, Role, Value (A)
- **Impact:** When the loading overlay is shown, screen readers continue to read the underlying content. There is no indication that the page is busy loading.
- **Fix (Task 14.2):** When showing `#loadingOverlay`, set `aria-busy="true"` on `#main-content` and `aria-label="Loading, please wait"` on the overlay. Remove `aria-busy` when loading completes.
- **Effort:** Low — JS change in `ui.js`.

---

### MEDIUM

---

#### A-009 — Calendar legend dots have no `aria-hidden`
- **File:** `Views/Home/Calendar.cshtml` — `.cal-legend-dot`
- **WCAG Criterion:** 1.1.1 Non-text Content (A)
- **Impact:** The coloured dots are purely decorative (the text label beside each dot conveys the meaning), but they are not hidden from AT. Screen readers may announce them as unlabelled images or empty elements.
- **Fix (Task 14.2):** Add `aria-hidden="true"` to each `.cal-legend-dot` `<span>`.
- **Effort:** Trivial.

---

#### A-010 — Calendar legend uses `<span>` without list semantics
- **File:** `Views/Home/Calendar.cshtml` — `.cal-legend`
- **WCAG Criterion:** 1.3.1 Info and Relationships (A)
- **Impact:** The legend items are a logical list but are marked up as bare `<span>` elements. Screen readers cannot convey the grouping or count of items.
- **Fix (Task 14.2):** Change `.cal-legend` to `<ul role="list" aria-label="Reservation status legend">` and each `.cal-legend-item` to `<li>`.
- **Effort:** Low.

---

#### A-011 — `btn-close` buttons in alerts missing explicit `aria-label`
- **File:** `Views/Shared/_Layout.cshtml` — `#errorAlert`, `#successAlert`
- **WCAG Criterion:** 4.1.2 Name, Role, Value (A)
- **Impact:** Bootstrap's `.btn-close` has a default accessible name of "Close" via CSS content, but this relies on the browser/AT combination correctly exposing it. An explicit `aria-label="Close alert"` is more robust.
- **Fix (Task 14.2):** Add `aria-label="Close alert"` to both `.btn-close` buttons in the alert divs.
- **Effort:** Trivial.

---

#### A-012 — Dashboard GridStack grid has no keyboard interaction hints
- **File:** `Views/Home/Index.cshtml` — `#dashboardGrid`
- **WCAG Criterion:** 2.1.1 Keyboard (A), 2.4.3 Focus Order (A)
- **Impact:** The GridStack drag-and-drop dashboard has no keyboard instructions. Users who cannot use a mouse have no way to reorder widgets.
- **Fix (Task 14.3):** Add a visually-hidden instruction text (e.g., "Use arrow keys to move widgets after pressing Enter") to `#dashboardGrid`. Implement keyboard move handlers in `dashboard.js` (Enter to grab, arrow keys to move, Escape to cancel).
- **Effort:** Medium–High.

---

#### A-013 — Dashboard grid and notification list missing `aria-live` for dynamic updates
- **File:** `Views/Home/Index.cshtml` — `#dashboardGrid`; `Views/Shared/_Layout.cshtml` — `#notifications-list`
- **WCAG Criterion:** 4.1.3 Status Messages (AA)
- **Impact:** When widgets load or notifications update, the changes are silent to screen readers.
- **Fix (Task 14.2):** Add `aria-live="polite"` to `#notifications-list`. For the dashboard grid, announce widget load completion via a visually-hidden status region.
- **Effort:** Low.

---

### LOW

---

#### A-014 — Color-only status indicators in calendar legend (dots)
- **File:** `Views/Home/Calendar.cshtml`
- **WCAG Criterion:** 1.4.1 Use of Color (A)
- **Impact:** The legend already includes text labels alongside the dots, so this is partially addressed. However, the dots themselves rely solely on color to convey status. For users with color blindness, the dots alone are ambiguous.
- **Fix (Task 14.4):** The text labels already mitigate this. Optionally add a small pattern or shape to each dot (e.g., different border styles) for enhanced color-blind support. Mark dots `aria-hidden="true"` (covered by A-009).
- **Effort:** Low (CSS only).

---

#### A-015 — `<html lang="en">` is hardcoded — guest portal may serve other languages
- **File:** `Views/Shared/_Layout.cshtml`, `Views/Shared/_GuestPortalLayout.cshtml`
- **WCAG Criterion:** 3.1.1 Language of Page (A)
- **Impact:** If the application is internationalised in future, the `lang` attribute must match the content language. Currently hardcoded to `en`.
- **Fix (Task 14.4):** Make `lang` dynamic via Razor: `<html lang="@CultureInfo.CurrentUICulture.TwoLetterISOLanguageName">`.
- **Effort:** Trivial.

---

## What Already Works Well

| Feature | Location | Notes |
|---------|----------|-------|
| Skip-to-content link | `_Layout.cshtml` | Correctly positioned, visible on focus |
| `aria-hidden="true"` on icons | Throughout | Decorative icons correctly hidden |
| `aria-label` on nav / buttons | Throughout | Good coverage |
| `role="main"`, `role="banner"`, `role="navigation"` | `_Layout.cshtml` | Correct landmark structure |
| Focus styles | `site.css` | `outline: 2px solid primary` on all interactive elements |
| `aria-live="assertive"` on error alert | `_Layout.cshtml` | Errors announced immediately |
| `role="alert"` on alert divs | `_Layout.cshtml` | Correct |
| `role="application"` on calendar | `Calendar.cshtml` | Appropriate for complex widget |
| `aria-labelledby` on modals | `Calendar.cshtml` | Correct modal labelling |
| `tabindex="-1"` on modal root | `Calendar.cshtml` | Bootstrap focus trap works correctly |
| `visually-hidden` on spinners | Throughout | Screen reader text present |
| `novalidate` + required attributes | `Calendar.cshtml` | HTML5 validation attributes present |
| `autocomplete` attributes | `Calendar.cshtml` | Helps autofill and AT |

---

## Fix Priority for Upcoming Tasks

### Task 14.2 — Implement ARIA Attributes
Resolve: **A-001, A-003, A-004, A-005, A-006, A-007, A-008, A-009, A-010, A-011, A-013**

### Task 14.3 — Improve Keyboard Navigation
Resolve: **A-002 (partial), A-012**

### Task 14.4 — Enhance Color Contrast and Visual Design
Resolve: **A-002 (error association), A-014, A-015**

---

## WCAG 2.1 AA Criterion Coverage

| Criterion | Description | Status | Issues |
|-----------|-------------|--------|--------|
| 1.1.1 | Non-text Content | Partial | A-009 |
| 1.3.1 | Info and Relationships | Partial | A-002, A-010 |
| 1.4.1 | Use of Color | Partial | A-014 |
| 1.4.3 | Contrast (Minimum) | Pass | — |
| 2.1.1 | Keyboard | Partial | A-012 |
| 2.4.3 | Focus Order | Partial | A-012 |
| 2.4.7 | Focus Visible | Pass | — |
| 3.1.1 | Language of Page | Pass (en only) | A-015 |
| 3.3.1 | Error Identification | Fail | A-002 |
| 3.3.3 | Error Suggestion | Fail | A-002 |
| 4.1.2 | Name, Role, Value | Partial | A-001, A-005, A-006, A-007, A-011 |
| 4.1.3 | Status Messages | Partial | A-003, A-004, A-008, A-013 |

---

## Keyboard Navigation Test Checklist

- [ ] Tab through entire page without mouse — all interactive elements reachable
- [ ] Shift+Tab reverses focus order correctly
- [ ] Enter / Space activates buttons and links
- [ ] Escape closes modals and dropdowns
- [ ] Arrow keys navigate within tab panels and menus
- [ ] Skip-to-content link appears on first Tab press and works
- [ ] Modal focus is trapped while open (Bootstrap handles this)
- [ ] After modal closes, focus returns to the trigger element
- [ ] Calendar events are keyboard-focusable and activatable
- [ ] Notification dropdown is keyboard-navigable
- [ ] Mobile bottom nav is reachable via keyboard (when visible)

---

## Screen Reader Test Checklist

- [ ] Page title is announced on load
- [ ] Landmark regions are announced (banner, navigation, main)
- [ ] Skip link is announced and functional
- [ ] Form labels are read with their inputs
- [ ] Required fields are announced as required
- [ ] Validation errors are announced when they appear
- [ ] Success/error alerts are announced
- [ ] Modal title is announced when modal opens
- [ ] Notification count changes are announced
- [ ] Loading state is announced
- [ ] Dynamic content updates (calendar, dashboard) are announced

---

*End of Accessibility Audit Report*
