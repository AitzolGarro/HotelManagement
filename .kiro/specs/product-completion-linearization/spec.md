# Spec: Product Completion Linearization

## Requirement 1 — Administration Base must be fully operational
The system SHALL provide a complete internal administration base before other dependent product layers are expanded.

### Includes
- user management route and UI
- create/update/deactivate users
- role behavior for Admin / Manager / Staff
- hotel access assignment behavior
- hotel creation/edit/delete UX and API correctness
- room creation/edit/delete/status UX and API correctness

### Acceptance
- admins can create users and assign accessible hotels
- managers/staff permissions behave as expected
- hotels and rooms can be created from the UI without dead routes or broken modals
- room types/statuses map correctly to backend enums

## Requirement 2 — Core reservation operations must be stable
The system SHALL ensure reservation and calendar behavior is internally consistent before guest-facing or integration flows rely on it.

### Includes
- create/update/cancel reservation
- room availability checks
- room date blocking
- reservation detail consistency
- calendar navigation and edit flows

### Acceptance
- reservation changes are reflected in calendar state
- invalid date/state transitions are rejected correctly
- availability and room status logic remain coherent

## Requirement 3 — Guest portal must reflect stable internal data
The system SHALL expose a guest portal only on top of already-stable reservation and profile behaviors.

### Includes
- guest dashboard
- reservation list/detail
- profile/preferences
- guest modification/cancellation rules

### Acceptance
- guest views match internal reservation data
- guest actions respect business rules and state transitions

## Requirement 4 — External channel integrations must align with internal models
The system SHALL keep Booking.com and Expedia integrations aligned with the canonical internal reservation/room model.

### Includes
- sync correctness
- webhook validation
- persistence correctness
- retry/error handling

### Acceptance
- incoming/outgoing channel data maps correctly to internal states
- webhook signature checks work reliably
- sync does not create inconsistent reservations/rooms

## Requirement 5 — Payment handling must be modeled on stable reservation states
The system SHALL finalize payment flows only after reservation state transitions are stable.

### Includes
- Stripe webhook completion
- captured/failed/refunded transitions
- reservation/payment coupling

### Acceptance
- payment state changes are reflected in reservation/business state correctly

## Requirement 6 — Product polish must not be blocked by unstable flows
The system SHALL complete i18n, UX, accessibility, and preferences after major behaviors are stable enough to avoid repeated rework.

### Includes
- full visible i18n
- theme/divisa/settings consistency
- accessibility labels and interaction polish

### Acceptance
- visible UI strings and placeholders are localized in supported flows
- theme and currency affect UI consistently

## Requirement 7 — Release readiness requires full-system validation
The system SHALL not be considered complete until all implemented blocks are validated together.

### Acceptance
- clean build
- green test suite
- manual QA scenarios across roles and critical flows
- no dead-end routes in major product areas
