# Concurrent order acceptance check

This console harness sends two simultaneous `POST /api/orders/{id}/accept` requests using separate Shipper bearer tokens. It exits with code 0 only when exactly one response is `200` or `204` and the other is `409 Conflict`.

Requirements:

- The API is running and connected to the SQL Server database used for the test.
- The order ID exists and is still `Pending` with no driver assigned.
- The API's authentication middleware and JWT configuration are enabled.
- Two active Shipper users exist, each with a `Drivers` row.

PowerShell example:

For a quick local test, create one private test key and use the same value in both terminals. Start the API in terminal A:

```powershell
$env:Jwt__Secret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Jwt__Issuer = 'ShipperMvp'
$env:Jwt__Audience = 'ShipperMvp.Mobile'
Write-Host "Keep this test key for terminal B: $env:Jwt__Secret"
dotnet run --project ShipperMvp.API/ShipperMvp.API.csproj --launch-profile https
```

In terminal B, set the same key, the pending order ID, and the two `Users.Id` values for active Shippers with driver profiles:

```powershell
$env:Jwt__Secret = '<copy-the-key-from-terminal-A>'
$env:Jwt__Issuer = 'ShipperMvp'
$env:Jwt__Audience = 'ShipperMvp.Mobile'
$env:SHIPPER_API_BASE_URL = 'https://localhost:7230'
$env:SHIPPER_TEST_ORDER_ID = '123'
$env:SHIPPER_USER_ID_1 = '10'
$env:SHIPPER_USER_ID_2 = '11'
$env:SHIPPER_GENERATE_TEST_TOKENS = 'true'
$env:SHIPPER_ALLOW_UNTRUSTED_CERT = 'true' # local development certificate only
dotnet run --project scripts/AcceptOrderRace/AcceptOrderRace.csproj
```

The harness signs two short-lived, test-only HS256 tokens with different `sub` values and `role=Shipper`, then races the requests with `Task.WhenAll`. This bypasses the need for a login endpoint, which is not part of the current API implementation. Alternatively, provide two tokens through `SHIPPER_TOKEN_1` and `SHIPPER_TOKEN_2` and omit test-token generation.

Expected result: one `200 OK`, one `409 Conflict`, then `PASS`. Any other status (including `401`, `403`, `404`, or `500`) fails the check. The winning request changes the order permanently, so use a fresh Pending order for each run. Keep the test key and tokens out of source control; never reuse this test key in production.
