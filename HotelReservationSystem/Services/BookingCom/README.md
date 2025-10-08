# Booking.com Integration - Reservation Synchronization

This document describes the reservation synchronization implementation for Booking.com integration.

## Overview

The reservation synchronization system provides bidirectional integration with Booking.com, ensuring that reservations are kept in sync between the external platform and the internal hotel management system.

## Components

### 1. BookingIntegrationService

The main service that handles all synchronization logic:

- **SyncReservationsAsync()**: Performs full synchronization for all hotels
- **SyncReservationsForHotelAsync()**: Syncs reservations for a specific hotel
- **ProcessExternalReservationAsync()**: Processes individual reservations from Booking.com
- **PushAvailabilityUpdateAsync()**: Sends availability updates to Booking.com
- **HandleWebhookAsync()**: Processes real-time webhook notifications

### 2. ReservationSyncBackgroundService

A background service that runs periodic synchronization:

- Configurable sync interval (default: 15 minutes)
- Handles failures gracefully and continues running
- Uses scoped services to ensure proper dependency injection

### 3. BookingComWebhookController

REST API controller for handling webhook notifications:

- **POST /api/webhooks/booking-com**: Processes webhook notifications
- **GET /api/webhooks/booking-com/health**: Health check endpoint
- **GET /api/webhooks/booking-com/validate**: Webhook validation endpoint

## Synchronization Flow

### Periodic Sync (Pull)

1. Background service triggers sync every N minutes
2. Service fetches reservations from Booking.com for each hotel
3. For each external reservation:
   - Check if reservation exists locally
   - If exists: Update local reservation with external changes
   - If not exists: Create new local reservation
   - Handle guest creation/updates automatically

### Real-time Sync (Push via Webhooks)

1. Booking.com sends webhook notification for reservation changes
2. Webhook controller receives and validates the payload
3. BookingIntegrationService processes the notification:
   - **reservation_created/updated**: Process the reservation
   - **reservation_cancelled**: Cancel the local reservation

### Availability Push

When room availability changes locally:
1. Call `PushAvailabilityUpdateAsync()` with room, date, and availability count
2. Service formats the data for Booking.com XML API
3. Sends update to Booking.com availability endpoint

## Configuration

Add the following to `appsettings.json`:

```json
{
  "BookingCom": {
    "BaseUrl": "https://secure-supply-xml.booking.com/",
    "Username": "your-username",
    "Password": "your-password",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 2,
    "SyncIntervalMinutes": 15
  }
}
```

## Error Handling

- **Network failures**: Automatic retry with exponential backoff
- **API errors**: Logged and reported, sync continues for other reservations
- **Data conflicts**: Overbooking warnings logged, external reservations take precedence
- **Invalid data**: Validation errors logged, problematic reservations skipped

## Testing

The implementation includes comprehensive tests:

- **Unit tests**: Individual service methods
- **Integration tests**: End-to-end synchronization scenarios
- **Controller tests**: Webhook endpoint behavior
- **Background service tests**: Periodic sync behavior

## Usage Examples

### Manual Sync

```csharp
// Sync all hotels
await bookingIntegrationService.SyncReservationsAsync();

// Sync specific hotel
await bookingIntegrationService.SyncReservationsForHotelAsync(hotelId);
```

### Push Availability

```csharp
// Update availability for room 101 on 2024-12-01 (1 room available)
await bookingIntegrationService.PushAvailabilityUpdateAsync(roomId: 101, 
    date: new DateTime(2024, 12, 1), availableCount: 1);
```

### Process Webhook

```csharp
// Webhook controller automatically handles this
// POST /api/webhooks/booking-com with XML payload
```

## Monitoring

The system provides extensive logging:

- **Information**: Successful operations, sync statistics
- **Warning**: Non-critical issues, availability conflicts
- **Error**: Failed operations, API errors, data validation issues

Monitor the logs for:
- Sync completion messages
- Error rates
- Processing times
- Webhook delivery success rates

## Requirements Satisfied

This implementation satisfies the following requirements:

- **1.1**: Automatic import of Booking.com reservations within 5 minutes (via webhooks + periodic sync)
- **1.2**: Push availability updates to Booking.com within 2 minutes
- **1.3**: Handle reservation modifications from Booking.com