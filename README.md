# SteamBackEnd

ASP.NET Core 10 API. Existing controllers, services, repositories, database entities and SQL Server migrations are retained.

## Run Locally on macOS

Install the .NET 10 SDK and clone the backend. In one terminal:

```sh
git clone https://github.com/qqwwalls/SteamBackEnd.git
cd SteamBackEnd
dotnet run --project Steam.Api --launch-profile local
```

Swagger: <http://localhost:5067/swagger/index.html>

Install Node.js and start the frontend in a second terminal, from the same parent directory:

```sh
git clone https://github.com/qqwwalls/VStoreFrontend.git
cd VStoreFrontend
npm ci
npm run dev -- --port 5173 --strictPort
```

Store: <http://localhost:5173/>. Keep both terminals open while demonstrating the project.

The explicit `local` profile uses SQLite. Users, games, wishlists and library entries are saved in `Steam.Api/steam.local.db` and survive restarts. This is a separate local database, not a connection to an existing SQL Server database. The local profile adds a demonstration catalogue with images to the database without changing existing games. Disable `SeedDemoData` to skip it; the SQL Server profiles do not seed it.

## SQL Server

The original `http` and `https` profiles continue to use SQL Server. Set your own connection string; the committed Windows machine name is only an example.

```sh
export ConnectionStrings__SqlServerConnection='Server=localhost,1433;Database=SteamDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True'
dotnet run --project Steam.Api --launch-profile http
```

For a new database, explicitly opt into applying the existing migrations:

```sh
ApplyMigrations=true dotnet run --project Steam.Api --launch-profile http
```

Existing SQL Server databases are not migrated automatically. No migrations or existing database tables were changed by these fixes. A schema diagram alone does not provide a live database connection.

## Check Login and Wishlist in Swagger

1. Call `POST /api/Auth/register` with a username, email and password of at least six characters. Alternatively, `POST /api/User` still creates a user, followed by `POST /api/Auth/login`.
2. Copy `accessToken` from the response into Swagger's **Authorize** dialog. Paste the token itself; Swagger adds the Bearer prefix.
3. Call `POST /api/Game`. Set `developerId` to the `user.id` returned by registration, for example:

```json
{
  "title": "My Game",
  "description": "My first game",
  "price": 120,
  "developerId": "YOUR-USER-GUID",
  "genreIds": [1],
  "tagIds": [2]
}
```

4. Use the returned game GUID with `POST /api/Wishlist/{gameId}`.
5. Call `GET /api/Wishlist` to read saved games. Use `DELETE /api/Wishlist/{gameId}` to remove one.

Repeated additions return `409`, unknown games return `404`, and unauthenticated requests return `401`. Each user sees only their own wishlist. Game updates and related media, achievements and requirements can be written by the game's developer or an administrator.

Login accepts `{ "email": "you@example.com", "password": "your-password" }`. Responses include `accessToken`, `refreshToken`, the access-token expiry in `expiresAt`, the refresh-token expiry in `refreshTokenExpiresAt`, and `user`. Refresh tokens are single-use: `POST /api/Auth/refresh` rotates one; `POST /api/Auth/logout` revokes one. An already issued access token remains valid until expiry unless the user is deactivated.

## Tests

```sh
dotnet test Steam.slnx
```

Tests create an isolated temporary SQLite database. To run the same tests against SQL Server, provide `STEAM_TEST_SQL_CONNECTION` pointing to a **dedicated empty test database**. The test host explicitly applies migrations there and creates test records. Never point tests at an existing personal or production database.

## Library and Checkout

`GET /api/Library` returns games owned by the authenticated user, including artwork and acquisition dates. `POST /api/Library/acquire` accepts `{ "gameIds": ["GAME-GUID"] }` and saves free games in the existing `UserGames` table. Both endpoints require JWT authentication; the user is always taken from the token.

The service looks up prices in the database. Client-supplied prices, totals and user IDs cannot grant ownership. By default paid games return `503` until a real payment provider is integrated. Duplicate acquisitions return `409`, missing games return `404`, and empty selections return `400`. No schema change is needed for the library.

The Development configuration enables `Checkout:EnableDemoPurchases` for the local, HTTP and HTTPS development profiles. `GET /api/Library/checkout-options` exposes this mode to the frontend. In demo mode, an authenticated request to `acquire` with `demoPurchase: true` can save paid games or a mixed selection to the library without taking payment. Omitting this acknowledgement returns `400`. The frontend labels the action and result as a demo purchase; no card details are collected. A request flag alone cannot enable demo purchases when the server setting is off.

The local catalogue uses separate portrait covers and wide banners, selected from the existing Media records by filename. Known old demo thumbnails are upgraded without changing game IDs, prices, names, purchases or custom artwork. No database migration is needed.

## Integration Status

- The [VStore frontend](https://github.com/qqwwalls/VStoreFrontend) uses this backend for the catalogue, game details, authentication, wishlist and library. Vite proxies its API requests to port `5067`.
- This API uses GUID game identifiers and numeric prices. The frontend API adapter converts prices for the existing display components.
- Default CORS origins: `http://localhost:5173`, `http://localhost:5174`, and `http://localhost:3000`. Override `Cors:AllowedOrigins` for other frontends.
- External login returns `501` until a provider and provider-token verification are implemented. The original endpoint trusted the email supplied by the client, which cannot establish identity.
- The cart is browser state, isolated by account; it is not proof of ownership or price. Demo checkout is supported, while real payment processing, friends and unlocking achievements are not implemented. The existing database entities are retained.
- No default administrator account is created. The original `DbInitializer` is retained but is not called; its `CHANGE_ME` placeholder is not a usable password hash.
- Before production, provide a private JWT key of at least 32 bytes via configuration, for example `JwtSettings__Key`. The committed development key is deliberately rejected outside Development/Testing.
