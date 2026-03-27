# Tasks: Product Completion Linearization

## Block 1 — Administration Base
- [ ] 1.1 Audit and close user management flow end-to-end
  - [ ] 1.1.1 Verify create/update/deactivate UI flows with real admin account
  - [ ] 1.1.2 Verify role restrictions for Admin / Manager / Staff
  - [ ] 1.1.3 Verify hotel access assignments are persisted and enforced
  - [ ] 1.1.4 Add/adjust tests for user management behavior
- [ ] 1.2 Audit and close hotel management flow
  - [ ] 1.2.1 Verify create/edit/delete hotel flows from UI
  - [ ] 1.2.2 Verify validation and duplicate/invalid input handling
  - [ ] 1.2.3 Ensure hotel state changes do not break dependent views
- [ ] 1.3 Audit and close room management flow
  - [ ] 1.3.1 Verify create/edit/delete room flows from UI
  - [ ] 1.3.2 Verify enum mapping for room type and status
  - [ ] 1.3.3 Verify room actions refresh the correct hotel context

## Block 2 — Core Operations
- [ ] 2.1 Audit reservation CRUD end-to-end
- [ ] 2.2 Audit availability and conflict detection
- [ ] 2.3 Audit block-dates flow
- [ ] 2.4 Audit calendar consistency with reservation state
- [ ] 2.5 Add/fix tests for uncovered reservation/calendar paths

## Block 3 — Guest Portal
- [ ] 3.1 Audit guest dashboard against internal reservation/profile data
- [ ] 3.2 Audit guest reservation detail and modification flows
- [ ] 3.3 Audit guest cancellation rules and UX feedback
- [ ] 3.4 Add/fix tests for guest portal critical flows

## Block 4 — External Integrations
- [ ] 4.1 Audit Booking.com against current internal model
  - [ ] 4.1.1 Verify sync flow manually and by tests
  - [ ] 4.1.2 Verify webhook signatures and persistence
  - [ ] 4.1.3 Confirm no remaining legacy-path coupling
- [ ] 4.2 Audit Expedia against current internal model
  - [ ] 4.2.1 Verify sync/import persistence
  - [ ] 4.2.2 Verify webhook signatures and non-happy paths

## Block 5 — Payments
- [ ] 5.1 Close Stripe webhook TODOs (Captured / Failed / Refunded)
- [ ] 5.2 Define and verify reservation/payment state transitions
- [ ] 5.3 Add tests for payment side effects and webhook processing

## Block 6 — Product Polish
- [ ] 6.1 Finish remaining visible i18n outside already-closed areas
- [ ] 6.2 Audit theme/currency/preferences consistency across all screens
- [ ] 6.3 Audit accessibility labels and modal/button interactions

## Block 7 — QA / Hardening / Release Readiness
- [ ] 7.1 Run role-based manual QA matrix (Admin / Manager / Staff / Guest)
- [ ] 7.2 Run critical flow matrix (login, users, hotels, rooms, reservations, calendar, guest portal, integrations, payments)
- [ ] 7.3 Tune rate limiting and operational settings based on real usage
- [ ] 7.4 Review logs/health checks/production configuration
- [ ] 7.5 Final sign-off: build clean, tests green, no major dead-end routes
