# Campus Estate Living

Campus Estate Living is a project I built to help manage the UFS Qwaqwa campus estate in one place. It includes a web portal for residents and staff, a REST API, shared .NET contracts, and an Android mobile app.

## What is included

This repository contains four main parts:

- `CampusEstateLiving/` is the ASP.NET Core web application.
- `CampusEstateLiving.Api/` is the REST API used by the web and mobile clients.
- `CampusEstateLiving.Shared/` contains DTOs and contracts shared by the .NET projects.
- `CampusEstateLivingMobile/` is the Android application written in Java and XML.

## What the system can do

I built the project around the daily work involved in running a student estate. The current features include:

- Student and staff registration, login, password changes, lockout handling, and profiles.
- Accommodation, properties, rooms, room assignments, and vehicle management.
- Payment tariffs, payment submission, payment history, cancellations, payment review, confirmation, failure, and reports.
- Saved cards and bank accounts, notifications, maintenance requests, notices, reviews, and staff messages.
- Separate experiences for residents, financial officers, and administrators.
- Web and Android clients using the same API rules and role permissions.

## User roles

Students and staff can manage their own profile, accommodation information, vehicles, payments, notifications, maintenance requests, notices, and reviews.

Financial officers can review payments, record payments, view reports, look up residents, and manage payment-related support tasks.

Administrators can do everything a financial officer can do, as well as manage users, assign rooms, manage roles, publish notices, and handle estate administration.

## Technology I used

- .NET 9 and ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core 9 and SQL Server
- ASP.NET Core Identity
- JWT authentication for API clients
- Swagger/OpenAPI for API development
- Android Gradle Plugin 8.7.3, Java, and XML layouts

## What I need to run it

For the web application and API, I use the .NET 9 SDK and a SQL Server database. For the Android application, I use Android Studio, JDK 17, and Android SDK 35.

I keep passwords, database credentials, JWT keys, Android signing keys, and `local.properties` out of source control. These values should be supplied through local configuration, user secrets, or environment variables.

## Running the web application

From the repository folder, I run:

```powershell
dotnet restore CampusEstateLiving/CampusEstateLiving.csproj
dotnet run --project CampusEstateLiving/CampusEstateLiving.csproj
```

The application prints the local URL when it starts. It also applies its schema checks and seed data during startup.

## Running the API

I start the API with:

```powershell
dotnet restore CampusEstateLiving.Api/CampusEstateLiving.Api.csproj
dotnet run --project CampusEstateLiving.Api/CampusEstateLiving.Api.csproj --urls http://localhost:5058
```

When the API is running in Development mode, I can open `/swagger` to inspect and try the endpoints.

The main API areas are:

- `api/Auth` for registration, login, and password changes.
- `api/Profile` and `api/Users` for profiles and account administration.
- `api/Properties`, `api/Rooms`, and `api/RoomAssignments` for accommodation.
- `api/Payments` and `api/Reports` for payments and financial reporting.
- `api/Wallet` for saved payment methods.
- `api/Maintenance`, `api/Notices`, `api/Vehicles`, and `api/Notifications` for estate services.
- `api/Reviews` and `api/StaffMessages` for communication and feedback.

Protected requests use a JWT bearer token:

```http
Authorization: Bearer <jwt>
```

## Running the Android app

I open `CampusEstateLivingMobile` in Android Studio, let Gradle sync, and start the API before running the app.

The debug build points an Android Emulator at `http://10.0.2.2:5058/`. When I use a physical phone, I point the app at the computer's LAN IP address instead. Release builds must use HTTPS and a deployed API host.

With the Android SDK and JDK configured, the debug APK can be built with:

```powershell
./gradlew.bat :app:assembleDebug
```

The Android app includes login, registration, resident and staff screens, finance and administrator screens, payment review, saved payment methods, maintenance, notices, vehicles, notifications, reviews, and profile management. The app stores the session token but never stores the user's password. The API remains responsible for enforcing permissions.

## Testing

I build both .NET applications with:

```powershell
dotnet build CampusEstateLiving.Api/CampusEstateLiving.Api.csproj
dotnet build CampusEstateLiving/CampusEstateLiving.csproj
```

For mobile changes, I run `:app:assembleDebug` from Android Studio or from a machine with Android SDK 35 and JDK 17. I also check login success and failure, role access, payment submission and review, room assignment rules, maintenance updates, notices, and session expiry.

## Security notes

I use HTTPS outside local development and keep database credentials and JWT keys outside the repository. I do not store CVV numbers or full card numbers. Before production, I also review the CORS policy and use a separate Android release signing key.

## Commit style

I keep commits short and grouped by purpose. Examples:

```text
feat(api): add maintenance and estate service endpoints
feat(web): improve resident and administration workflows
feat(mobile): improve payment and login feedback
docs: update project setup and architecture notes
```
