# ShipperMvp.Client

Flutter client sample structured by Clean Architecture and feature boundaries:

```text
lib/
  core/                 API client, token storage
  features/
    auth/{data,domain,presentation}
    customer/{data,domain,presentation}
    shipper/{data,domain,presentation}
    shared/domain/       Order and API status mapping
```

State management uses Cubit (`flutter_bloc`). Authentication stores the JWT in secure storage and routes to the Customer or Shipper home from the role returned by the API. Shipper order transitions are constrained in the Cubit to Accepted → PickedUp → Delivering → Completed.

## Run

```powershell
cd ShipperMvp.Client
flutter pub get
flutter run --dart-define=API_BASE_URL=https://10.0.2.2:5001
```

Use your machine's LAN address for a physical device. The default `10.0.2.2` is for an Android emulator. For a local development API using a self-signed HTTPS certificate, configure Android/iOS development trust or use an HTTP development endpoint.

The auth data source expects `POST /api/auth/login` and `POST /api/auth/register` to return a JWT and role. The checked-in API currently has no AuthController, so these screens need the auth endpoints before live sign-in works. Order endpoints follow the routes in `ShipperMvp.API/Controllers/OrdersController.cs`.
