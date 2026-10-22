# Campus Estate Living

Campus Estate Living is a digital estate management platform for the UFS Qwaqwa campus estate. This repository contains the resident and staff web portal, a REST API, shared .NET DTOs, and a Java Android client.

## Projects

| Directory | Purpose |
| --- | --- |
| `CampusEstateLiving/` | ASP.NET Core MVC web portal with cookie authentication |
| `CampusEstateLiving.Api/` | ASP.NET Core REST API with JWT authentication |
| `CampusEstateLiving.Shared/` | DTOs and shared contracts |
| `CampusEstateLivingMobile/` | Native Android client written in Java and XML |

## Features

- Account registration, login, password changes, lockout handling, roles, and account administration.
- Profiles, accommodation assignments, properties, rooms, vehicles, and maintenance requests.
- Payment tariffs, payment submission, history, cancellation, officer review, confirmation, failure, and reporting.
- Saved payment methods, notifications, estate notices, reviews, staff messages, and dashboards.
- Role-aware experiences for students, staff, financial officers, and administrators.

## Roles

| Role | Access |
| --- | --- |
| Student / Staff | Own profile, accommodation, vehicles, payments, notifications, maintenance, notices, and reviews |
| Financial Officer | Payment queue, payment recording, reports, resident lookup, reviews, and account support |
| Administrator | Financial officer capabilities plus user management, room assignment, role authorisation, notices, and estate administration |

## Technology

- .NET 9, ASP.NET Core MVC/Web API, Entity Framework Core 9, and SQL Server
- ASP.NET Core Identity and JWT bearer authentication
- Swagger/OpenAPI for API development
- Android Gradle Plugin 8.7.3, Java, XML layouts, minimum Android API 23

## Prerequisites

- .NET 9 SDK
- SQL Server or a compatible development database
- Android Studio with JDK 17 and Android SDK 35 for mobile development
- Git

## Configuration and security

Do not commit passwords, JWT signing keys, cloud connection strings, keystores, or `local.properties`. Use user secrets, environment variables, or an untracked local settings file.

The web app and API read `ConnectionStrings:DefaultConnection`. The API also requires `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiryMinutes`. Replace development values before deployment and rotate credentials that have ever been exposed.

## Run the web application

```powershell
dotnet restore CampusEstateLiving/CampusEstateLiving.csproj
dotnet run --project CampusEstateLiving/CampusEstateLiving.csproj
```

The URL is printed by ASP.NET Core. The application applies schema checks and seed data during startup.

## Run the API

```powershell
dotnet restore CampusEstateLiving.Api/CampusEstateLiving.Api.csproj
dotnet run --project CampusEstateLiving.Api/CampusEstateLiving.Api.csproj --urls http://localhost:5058
```

In Development, Swagger is available at `/swagger`. The API applies schema checks on startup and exposes controllers under `/api`.

Important API areas include:

| Area | Example routes |
| --- | --- |
| Authentication | `POST /api/Auth/login`, `POST /api/Auth/register`, `POST /api/Auth/change-password` |
| Profile and users | `/api/Profile`, `/api/Users` |
| Accommodation | `/api/Properties`, `/api/Rooms`, `/api/RoomAssignments` |
| Payments | `/api/Payments`, `/api/Payments/tariffs`, `/api/Payments/all`, `/api/Reports` |
| Wallet | `/api/Wallet/cards`, `/api/Wallet/banks` |
| Estate services | `/api/Maintenance`, `/api/Notices`, `/api/Vehicles`, `/api/Notifications` |
| Reviews and messages | `/api/Reviews`, `/api/StaffMessages` |

Authenticated requests use `Authorization: Bearer <jwt>`.

## Run the Android app

Open `CampusEstateLivingMobile` in Android Studio and let Gradle sync. Start the API first.

- Android Emulator: the debug build uses `http://10.0.2.2:5058/`.
- Physical device: use the computer's LAN IP and keep the trailing slash.
- Release builds must use HTTPS and a deployed API host.
- Keep `local.properties`, build output, IDE metadata, and signing credentials local.

Build from Android Studio or, with Android SDK 35 and JDK 17 configured:

```powershell
./gradlew.bat :app:assembleDebug
```

The Android client supports login, registration, resident and staff workflows, finance and administrator screens, payment review, saved payment methods, maintenance, notices, vehicles, notifications, reviews, and profile management. It stores the session token, never the password. The API remains the authority for role permissions.

## Testing

```powershell
dotnet build CampusEstateLiving.Api/CampusEstateLiving.Api.csproj
dotnet build CampusEstateLiving/CampusEstateLiving.csproj
```

For mobile changes, run `:app:assembleDebug` from Android Studio or a machine with Android SDK 35 and JDK 17. Exercise login success and failure, role access, payment submission and review, room assignment constraints, maintenance updates, notice visibility, and session expiry.

## Deployment notes

- Use HTTPS outside local development.
- Store database credentials and JWT keys outside source control.
- Never store CVV or full card numbers.
- Review CORS origins before production; development settings are permissive.
- Use a dedicated production signing key for Android releases.

## Commit style

Use short imperative messages grouped by concern:

```text
feat(api): add maintenance and estate service endpoints
feat(web): expand resident and administration workflows
feat(mobile): improve payment and login feedback
docs: document web, API, shared, and Android projects
```

Keep generated files, local databases, IDE metadata, and secrets out of commits.
