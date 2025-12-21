# Hotel Reservation System - Login Instructions

## Why Data Requests Are Not Working

The data requests are not being made when entering sections because **authentication is required**. All API endpoints return `401 Unauthorized` for unauthenticated users.

## Demo Login Credentials

The system includes three demo users that are automatically created. **Use email addresses to log in:**

### Admin User
- **Email**: `admin@demo.com`
- **Password**: `Demo123!`
- **Access**: Full system access

### Manager User
- **Email**: `manager@demo.com`
- **Password**: `Demo123!`
- **Access**: Management features (properties, reports, etc.)

### Staff User
- **Email**: `staff@demo.com`
- **Password**: `Demo123!`
- **Access**: Basic reservation management

## How to Test the System

1. **Start the application**:
   ```powershell
   cd HotelReservationSystem
   dotnet run
   ```

2. **Open the login page**: http://localhost:5000/login

3. **Log in with any of the demo credentials above** (use email addresses, not usernames)

4. **Navigate to different sections**:
   - Dashboard: http://localhost:5000/
   - Calendar: http://localhost:5000/Home/Calendar
   - Properties: http://localhost:5000/Home/Properties
   - Reservations: http://localhost:5000/Home/Reservations
   - Reports: http://localhost:5000/Home/Reports

## What Happens After Login

Once logged in:
- JWT token is stored in localStorage
- API requests include the Authorization header
- Data requests work properly
- Real-time updates via SignalR are enabled
- Role-based UI elements are shown/hidden

## Demo Data Available

The system includes:
- 3 hotels with multiple rooms
- 5 sample guests
- 3 sample reservations
- Various room types and rates

## Troubleshooting

If data still doesn't load after login:
1. Check browser console for JavaScript errors
2. Check network tab for failed API requests
3. Verify JWT token exists in localStorage
4. Check application logs for server errors