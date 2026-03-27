# Design: Product Completion Linearization

## Architectural Principle
Finish the product from the inside out:
- first core authority and structure
- then operational workflows
- then guest-facing projections
- then external connectors
- then monetization
- then polish and deployment hardening

## Dependency Graph

### Block 1 — Administration Base
Scope:
- user management
- role enforcement
- hotel access assignments
- hotels CRUD
- rooms CRUD and status consistency

Dependencies: none
Downstream dependents:
- reservations
- calendar
- guest portal
- integrations
- reporting

### Block 2 — Core Operations
Scope:
- reservations CRUD
- conflict detection
- availability consistency
- date blocking
- reservation ↔ room ↔ hotel rules
- calendar parity with reservation state

Depends on:
- Block 1

Downstream dependents:
- guest portal
- Booking/Expedia sync
- payments
- reporting

### Block 3 — Guest Portal
Scope:
- guest dashboard
- reservation list/detail
- profile/preferences
- guest-side modification/cancellation behavior

Depends on:
- Block 2

### Block 4 — External Integrations
Scope:
- Booking.com real sync/webhook correctness
- Expedia real sync/webhook correctness
- mapping and persistence guarantees

Depends on:
- Block 2

### Block 5 — Payments
Scope:
- Stripe webhook completeness
- payment/reservation state transitions
- capture/refund/failure handling

Depends on:
- Block 2
- preferably after Block 4, but can be parallelized only once reservation state model is stable

### Block 6 — Global Product Polish
Scope:
- remaining i18n
- accessibility
- consistency of UX patterns
- settings/preferences coherence

Depends on:
- Blocks 1–5 stabilizing screens and flows

### Block 7 — QA / Hardening / Release Readiness
Scope:
- end-to-end validation
- non-happy-path checks
- logging/health/rate limiting tuning
- production configuration review

Depends on:
- all previous blocks

## Validation Strategy
Each block must be closed with:
1. build clean
2. test suite green
3. targeted manual acceptance scenarios
4. no open routing dead-ends or placeholder UI in that block

## Key Decision
Do not expand horizontally across many modules at once.
Close each block vertically before advancing.
