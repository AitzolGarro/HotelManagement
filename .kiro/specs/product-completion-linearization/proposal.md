# Proposal: Product Completion Linearization

## Intent
Create a single master change that organizes the remaining product work into one linear, low-rework sequence.

## Problem
The project now has strong technical foundations (clean builds, green tests, major features implemented), but the remaining work spans multiple modules with dependency overlap:
- users/roles/hotel access
- hotels and rooms CRUD/UX
- reservations and calendar consistency
- guest portal parity
- external integrations
- payments
- remaining i18n/polish
- QA/hardening

If these are tackled opportunistically, later phases will force UI, service, permission, and test rewrites.

## Goal
Define one unified SDD track that:
1. Completes the product in dependency order
2. Minimizes future rewrites
3. Requires each layer to be stable before the next depends on it
4. Uses clear "done" criteria for each block

## Non-Goals
- This change does not immediately implement all remaining work
- This change does not replace already completed sub-changes
- This change does not reopen solved areas unless needed for linear consistency

## Why this order
The lowest-rework path is:
1. Internal administration base (users, roles, hotels, rooms)
2. Core operations (reservations, availability, calendar)
3. Guest-facing flows (guest portal)
4. External channels (Booking.com, Expedia)
5. Payments (Stripe)
6. Global polish (i18n, UX, accessibility)
7. QA, hardening, production readiness

This keeps downstream layers from depending on unstable upstream behaviors.
