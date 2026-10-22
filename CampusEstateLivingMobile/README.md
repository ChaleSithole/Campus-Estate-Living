# Campus Estate Living Mobile

Java Android client for the existing Campus Estate Living ASP.NET Core API.
The web application was not modified. No new API routes were added.

Language is Java, with the existing XML resources and Gradle files. Kotlin was not introduced.

## Open and run

1. Open this folder in Android Studio (Android Gradle Plugin 8.7.3, JDK 17).
2. Let Gradle sync. Android Studio writes `local.properties` with your SDK path. Do not commit that file.
3. Start the existing API so it listens on port **5058**.
4. Emulator: `ApiClient.BASE_URL` is `http://10.0.2.2:5058/` (the emulator's address for the computer). Cleartext HTTP is allowed because that development URL is HTTP. Production must use HTTPS and this flag must be turned off.
5. Physical device: change `BASE_URL` in `app/src/main/java/za/co/campusestateliving/mobile/api/ApiClient.java` to the computer's LAN address, with a trailing slash.
6. Run the `app` configuration, or build a debug APK with **Build > Build Bundle(s) / APK(s) > Build APK(s)**.

A debug APK was **not** built in the environment that prepared this source. There is no Android SDK there, so do not treat this folder as a tested APK.

Release builds: use your own upload keystore. Do not put passwords, the JWT signing key, or the database connection string in this project.

## What the app does

Sign-in and registration use the API you already connected. The phone stores the JWT only, never the password. An expired `expiresAt` clears the session. HTTP 401 sends the user back to sign-in. Buttons hide actions the signed-in role should not use. The API still decides.

Roles, unchanged:

| Role | What this app shows |
| --- | --- |
| Student, Staff | Own profile, room, EFT payments, alerts, saved cards and bank accounts, own reviews |
| FinancialOfficer | Payment queue, month report, residents who already have an estate account, all reviews, password reset |
| Administrator | Everything the officer can do, plus create accounts, deactivate, authorise or revoke an officer, assign a room |

## Feature matrix

| Web workflow | Android screen | API | Roles | Status |
| --- | --- | --- | --- | --- |
| Login | Login | `POST api/Auth/login` | Public | Implemented |
| Register student or staff | Register | `POST api/Auth/register` | Public. Not Administrator or FinancialOfficer | Implemented |
| Session | SessionManager | `GET api/Auth/me` is not required after login | Authenticated | Token kept until expiry or sign-out |
| Change password | Profile | `POST api/Auth/change-password` | Authenticated | Implemented |
| Profile | Profile | `GET` / `PUT api/Profile` | Own profile | Implemented |
| Accommodation | Stay | `GET api/RoomAssignments/me`, `GET api/Properties`, `GET api/Rooms/property/{id}` | Authenticated. Residents do not book | Implemented |
| Assign room | Assign room | `POST api/Rooms/{roomId}/assign/{userId}` | Administrator | Implemented. Capacity, one active assignment, and sharing gender are enforced by the API |
| Payments | Pay | `GET api/Payments`, `GET api/Payments/tariffs`, `POST api/Payments` | Student, Staff | EFT only |
| Payment detail / cancel | Payment detail | `GET api/Payments/{id}`, `POST api/Payments/{id}/cancel` | Owner, or officer/admin for detail | Implemented |
| Finance queue | Finance | `GET api/Payments/all`, `POST .../confirm`, `POST .../fail` | Administrator, FinancialOfficer | Implemented |
| Record payment | Record payment | `POST api/Payments/record` | Administrator, FinancialOfficer | Open a person. `accountId` now comes from `GET api/Users` |
| Report | Finance header | `GET api/Reports` | Administrator, FinancialOfficer | Implemented |
| Notifications | Alerts | `GET api/Notifications`, `GET api/Notifications/unread-count`, `POST api/Notifications/{id}/read` | Own notifications | Loaded when the screen opens. Not push |
| Cards and banks | Payment methods | `GET/POST api/Wallet/cards`, `GET/POST api/Wallet/banks` | Student, Staff | Last four digits only. No CVV field |
| Reviews | Reviews | `GET api/Reviews/mine`, `POST api/Reviews`, `GET api/Reviews` | Residents submit. Officers and admins read all | Implemented |
| People | People | `GET api/Users`, `GET api/Users/{id}`, `POST api/Users` | Officer sees residents with an estate account. Admin creates users | Implemented |
| Deactivate / authorise / reset | Person | `POST api/Users/{id}/deactivate`, `.../authorise`, `.../reset-password` | Deactivate and authorise: Administrator. Reset: officer or admin, as the API allows | Implemented |
| Maintenance | Maintenance | `GET/POST api/Maintenance`, `PUT api/Maintenance/{id}` | Residents log their own. Officers and admins see all and update status | Added |
| Notices | Notices | `GET api/Notices`, `POST api/Notices`, `DELETE api/Notices/{id}` | Audience filter matches the website. Only an administrator publishes or removes | Added |
| Vehicles | Vehicles | `GET/POST api/Vehicles`, `DELETE api/Vehicles/{id}` | Student, Staff. Maximum three. Parking still requires two | Added |
| Record payment account | Person | `accountId` on `GET api/Users` | Administrator, FinancialOfficer | Added so a payment can be recorded without guessing an id |
| Card charge | Not offered | `POST api/Payments` returns an error for `Card` until a gateway exists | — | The app does not pretend a card payment succeeded |

## Testing

Not executed here (no Android SDK, API not running in this environment):

- Login success, wrong password, inactive account, lockout message
- Register validation (10-digit student number, 7-digit staff number, password rules)
- Session restore and expiry
- Resident cannot open another person's payment (API 403)
- Officer confirm and fail of a pending payment only
- Administrator assign rejected when the room is full, the resident already has a room, or sharing genders differ
- Card `POST` stays rejected

## Security notes

- Password is not saved.
- Card number is sent once and cleared. The phone does not keep it.
- Token is in private app preferences, not encrypted storage. A rooted device can read it. Sign-out clears it.
- `android:usesCleartextTraffic` is for the development HTTP API only.
- No production secrets are in this project.

## Visual refresh (web look on Android)

The Android client now uses the web app's palette and type pairing (`site.css`): navy header and tab bar, gold accents, cream background, serif headings, and card and hero styling. The palette lives in `Ui.java` and `res/values/colors.xml`.

The launcher now opens `SplashActivity`, which draws the crest, a gold orbit and a sparkle, then reveals the wordmark. After about three seconds it routes to Sign in, or straight to Home if a session is still valid. Tapping the splash skips it. The crest image is `res/drawable-nodpi/brand_mark.png`, and the launcher icon is `res/mipmap-nodpi/ic_launcher.png`.

No API routes or request logic changed.
