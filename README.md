# StudyRoomBooker

StudyRoomBooker is a web app for booking study rooms at OsloMet. Students can find a free room for a given day and time, see a weekly schedule for each room, and manage their own bookings. Administrators manage buildings and rooms.

The project contains two sub-applications that solve the same task:

| Folder | Description |
|---|---|
| `StudyRoomBooker/` | Sub-app 1: ASP.NET Core 10 MVC (server-rendered views) |
| `StudyRoomBooker.Tests/` | xUnit unit tests for the MVC controllers |
| `api/` | Sub-app 2, backend: ASP.NET Core 10 Web API with JWT authentication |
| `frontend/` | Sub-app 2, frontend: React + TypeScript (Vite) |

Both sub-apps use SQLite. The databases are created and filled with test data automatically on first start.

## Requirements

- .NET SDK 10.0.400
- Node.js 24.19.0 and npm

## Test users

The same users exist in both sub-apps.

| Role | Username | Password |
|---|---|---|
| Admin | admin@oslomet.no | Admin123! |
| Student | test@oslomet.no | Test123! |

Admins can create, edit and delete buildings, rooms and all bookings. Students can book rooms and edit or delete their own bookings. New users who register get the Student role.

## Run sub-app 1 (MVC)

    cd StudyRoomBooker
    dotnet run

Open http://localhost:5189

## Run sub-app 2 (API + React)

Use two terminals.

Terminal 1, the API:

    cd api
    dotnet run

The API runs on http://localhost:5246. API documentation (Scalar): http://localhost:5246/scalar/v1

Terminal 2, the frontend:

    cd frontend
    npm install
    npm run dev

Open http://localhost:4000

## Run the unit tests

    cd StudyRoomBooker.Tests
    dotnet test

## Reset the databases

Stop the apps and delete the database files. They are created again with test data on the next start.

- MVC: `StudyRoomBooker/studyroombooker.db`
- API: `api/studyroombooker.db` and `api/AuthDatabase.db`