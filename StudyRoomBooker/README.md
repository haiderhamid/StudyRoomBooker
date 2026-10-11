# Study Room Booker

A web application for booking study rooms at OsloMet, built with ASP.NET Core MVC for the course ITPE3200 Web Applications.

## Features
- View all study rooms with room number, building and capacity
- Create, view, edit and delete bookings
- Pick a room from a dropdown list when booking
- Prevents double booking: the same room cannot be booked for overlapping times
- Server-side input validation (required fields, end time must be after start time)
- Error handling and logging with Serilog, to the console and to log files in `Logs/`

## Technologies
- .NET 10.0 (ASP.NET Core MVC)
- Entity Framework Core with SQLite
- Serilog for logging
- Bootstrap for styling

## How to run
1. Install the [.NET 10.0 SDK](https://dotnet.microsoft.com/download).
2. Open a terminal in the project folder (the one with `StudyRoomBooker.csproj`).
3. Restore packages:
```bash
   dotnet restore
```
4. Start the application:
```bash
   dotnet run
```
5. Open the URL shown in the terminal, for example `http://localhost:5189`.

The database (`studyroombooker.db`) is created automatically from the migrations the first time the application starts, and three example rooms are added.

## Project structure
- `Controllers/`: `HomeController`, `RoomController`, `BookingController`
- `Models/`: `Room` and `Booking`
- `ViewModels/`: `BookingViewModel` (a booking plus the list of rooms for the dropdown)
- `Views/`: Razor views
- `Data/AppDbContext.cs`: the Entity Framework Core database context
- `Migrations/`: database migrations

## Author
Haider Hamid, ITPE3200 Web Applications, OsloMet, 2026