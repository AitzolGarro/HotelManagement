# Hotel Reservation Management System

A centralized hotel reservation management web application built with ASP.NET Core 8.0 and SQL Server.

## Features

- Hotel and room management
- Reservation management with conflict detection
- Booking.com integration (planned)
- Real-time calendar interface
- Comprehensive reporting and analytics
- Role-based access control

## Technology Stack

- **Backend**: ASP.NET Core 8.0 (C#)
- **Database**: SQL Server with Entity Framework Core
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI

## Project Structure

```
HotelReservationSystem/
├── Controllers/          # API Controllers
├── Data/                # Entity Framework DbContext
├── Models/              # Entity models and enums
├── Services/            # Business logic services
│   └── Interfaces/      # Service interfaces
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code

### Setup

1. Clone the repository
2. Navigate to the HotelReservationSystem directory
3. Update the connection string in `appsettings.json` if needed
4. Run the application:
   ```bash
   dotnet run
   ```

The application will:
- Create the database automatically on first run
- Start the API server (typically on https://localhost:7000)
- Provide Swagger UI for API testing

### Database

The application uses Entity Framework Core with Code First approach. The database schema includes:

- **Hotels**: Hotel properties with basic information
- **Rooms**: Individual rooms with type, capacity, and rates
- **Guests**: Guest information and contact details
- **Reservations**: Booking records with dates and status

## API Endpoints

- `GET /api/hotels` - Get all hotels
- `GET /api/hotels/{id}` - Get hotel by ID
- `POST /api/hotels` - Create new hotel
- `PUT /api/hotels/{id}` - Update hotel

More endpoints will be added as development progresses.

## Logging

The application uses Serilog for structured logging with outputs to:
- Console (for development)
- File (logs/hotel-reservation-{date}.txt)

## Next Steps

This is the initial project setup. Future development will include:
- Complete API endpoints for reservations
- Booking.com XML API integration
- Web frontend with calendar interface
- Authentication and authorization
- Comprehensive testing suite