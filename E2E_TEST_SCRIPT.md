# Kịch bản kiểm thử E2E — ShipperMvp

Kịch bản này kiểm tra hai phần riêng biệt:

1. Backend xử lý hai request nhận cùng đơn với kết quả một `200 OK`, một `409 Conflict`.
2. App Flutter nhận `409`, hiện Snackbar thân thiện và tự làm mới chợ đơn.

Mỗi lượt tranh chấp chỉ dùng **một đơn Pending mới**. Khi một shipper nhận thành công, không dùng lại đơn đó cho lượt thử tiếp theo.

## 0. Điều kiện trước khi chạy

- Windows, Docker Desktop đang chạy, Flutter SDK, Android SDK và Postman.
- Chạy lệnh từ thư mục gốc repo `ShipperMvp`.
- Dùng tài khoản, số điện thoại và dữ liệu thử riêng. Không dùng JWT secret hoặc tài khoản production.
- `.env` phải có `SQLSERVER_SA_PASSWORD`, `JWT_SECRET` dài ít nhất 32 byte, `JWT_ISSUER`, `JWT_AUDIENCE` và các port chưa bị ứng dụng khác chiếm.

Nếu chưa có `.env`, tạo từ mẫu rồi sửa các secret trước khi khởi động:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
notepad .env
```

Không đưa nội dung `.env`, JWT hoặc mật khẩu vào log/chia sẻ công khai.

## 1. Khởi động backend bằng Docker Compose

Từ thư mục repo:

```powershell
docker-compose up -d
```

Nếu máy dùng Compose v2 và không có lệnh `docker-compose`, dùng:

```powershell
docker compose up -d --build
```

`--build` bảo đảm API được build từ source hiện tại. Kiểm tra các container và log:

```powershell
docker compose ps
docker compose logs --tail 100 backend-api
```

Đợi `shipper-sqlserver` và `shipper-redis` ở trạng thái healthy, `backend-api` ở trạng thái Up. Mở Swagger để xác nhận API đã khởi động:

```text
http://localhost:8080/swagger/index.html
```

SQL Server dùng volume Compose; chạy lại lệnh không xóa dữ liệu database. Không dùng `docker compose down -v` trong lúc cần giữ dữ liệu.

## 2. Tạo ba tài khoản và hai đơn Pending bằng Postman

Tạo Postman Environment với các biến:

| Variable | Giá trị ban đầu |
|---|---|
| `baseUrl` | `http://localhost:8080` |
| `customerToken` | để trống |
| `shipperAToken` | để trống |
| `shipperBToken` | để trống |
| `customerId` | để trống |
| `shipperAId` | để trống |
| `shipperBId` | để trống |
| `backendRaceOrderId` | để trống |
| `uiRaceOrderId` | để trống |

Tạo các request dưới đây theo thứ tự. Số điện thoại phải khác các tài khoản đã có trong DB; dùng số test 10 chữ số, ví dụ các số trong dải riêng bạn dành cho E2E.

### 2.1 Đăng ký Customer

**POST** `{{baseUrl}}/api/auth/register`, Body → raw → JSON:

```json
{
  "fullName": "E2E Customer",
  "phoneNumber": "09000001001",
  "password": "E2eTest!2026",
  "role": "Customer"
}
```

Trong tab **Tests**:

```javascript
pm.test("Customer created", () => pm.response.to.have.status(201));
const body = pm.response.json();
pm.environment.set("customerToken", body.token);
pm.environment.set("customerId", body.userId);
```

### 2.2 Đăng ký Shipper A và Shipper B

Lặp lại **POST** `{{baseUrl}}/api/auth/register` với hai body khác nhau:

```json
{
  "fullName": "E2E Shipper A",
  "phoneNumber": "09000001002",
  "password": "E2eTest!2026",
  "role": "Shipper",
  "licensePlate": "E2E-A-001"
}
```

```json
{
  "fullName": "E2E Shipper B",
  "phoneNumber": "09000001003",
  "password": "E2eTest!2026",
  "role": "Shipper",
  "licensePlate": "E2E-B-001"
}
```

Không cần gửi `vehicleType`; backend sẽ dùng loại xe mặc định. Trong **Tests** của request Shipper A:

```javascript
pm.test("Shipper A created", () => pm.response.to.have.status(201));
const body = pm.response.json();
pm.environment.set("shipperAToken", body.token);
pm.environment.set("shipperAId", body.userId);
```

Trong **Tests** của request Shipper B, đổi tên biến thành `shipperBToken` và `shipperBId`.

Response đăng ký phải là `201 Created`, có `token` và `userId`. Mỗi shipper cần có Driver profile; endpoint đăng ký tự tạo profile này.

### 2.3 Tạo đơn cho kiểm thử backend race

Tạo request **POST** `{{baseUrl}}/api/orders`.

- Authorization → Bearer Token: `{{customerToken}}`
- Body → raw → JSON:

```json
{
  "pickupAddress": "E2E pickup backend race",
  "pickupPhone": "09000001001",
  "deliveryAddress": "E2E delivery backend race",
  "receiverName": "E2E Receiver",
  "receiverPhone": "09000001999",
  "itemDescription": "E2E backend race - run 1",
  "shippingFee": 25000,
  "codAmount": 0
}
```

Trong **Tests**:

```javascript
pm.test("Order created", () => pm.response.to.have.status(201));
const body = pm.response.json();
pm.environment.set("backendRaceOrderId", body.id);
pm.test("Order starts Pending", () => pm.expect(body.status).to.eql(0));
```

Xác minh đơn có `status: 0` và `driverId: null`. Có thể kiểm tra **GET** `{{baseUrl}}/api/orders/available` với Bearer `{{shipperAToken}}`; ID này phải xuất hiện trong danh sách.

## 3. Kiểm tra race thật ở backend bằng harness có sẵn

Harness gửi đồng thời hai `POST /api/orders/{id}/accept` với hai JWT khác nhau và chỉ trả exit code 0 nếu nhận được một thành công và một 409.

Mở PowerShell tại repo root, gán token từ Postman Environment vào biến môi trường cục bộ. Không ghi token vào file hoặc commit:

```powershell
$env:SHIPPER_API_BASE_URL = 'http://localhost:8080'
$env:SHIPPER_TEST_ORDER_ID = '<giá trị backendRaceOrderId>'
$env:SHIPPER_TOKEN_1 = '<shipperAToken>'
$env:SHIPPER_TOKEN_2 = '<shipperBToken>'
dotnet run --project .\scripts\AcceptOrderRace\AcceptOrderRace.csproj
```

Kết quả đạt:

```text
Shipper 1: 200 OK
Shipper 2: 409 Conflict
PASS: exactly one Shipper accepted the order and the other received HTTP 409 Conflict.
```

Thứ tự người thắng có thể đảo ngược. Đơn vừa dùng đã được nhận; **không tái sử dụng** cho bước UI. Nếu harness báo `400`, `500`, `401` hoặc `403`, ghi nhận là FAIL và xử lý lỗi backend/auth trước khi tiếp tục.

## 4. Tạo đơn Pending riêng cho hai app Flutter

Tạo request **POST** `{{baseUrl}}/api/orders` lần nữa, vẫn dùng Bearer `{{customerToken}}`, với mô tả khác để dễ nhận diện:

```json
{
  "pickupAddress": "E2E pickup Flutter race",
  "pickupPhone": "09000001001",
  "deliveryAddress": "E2E delivery Flutter race",
  "receiverName": "E2E Receiver UI",
  "receiverPhone": "09000001998",
  "itemDescription": "E2E Flutter UI race - run 1",
  "shippingFee": 25000,
  "codAmount": 0
}
```

Lưu `id` vào `uiRaceOrderId`; xác minh status là `0`. Đơn này được dành riêng cho hai emulator.

## 5. Chạy hai Flutter app cùng lúc

### 5.1 Chuẩn bị hai thiết bị

Khởi động hai AVD từ Android Studio Device Manager hoặc liệt kê thiết bị:

```powershell
flutter devices
adb devices -l
```

Phải thấy hai device ID khác nhau, ví dụ `emulator-5554` và `emulator-5556`. Nếu dùng hai điện thoại thật, cả hai phải cùng mạng LAN với máy chạy Docker.

### 5.2 Build một APK debug dùng URL phù hợp

Đối với Android Emulator, `10.0.2.2` trỏ về máy tính host. Từ thư mục client:

```powershell
Set-Location .\ShipperMvp.Client
flutter build apk --debug --dart-define=API_BASE_URL=http://10.0.2.2:8080
```

Cài APK lên cả hai emulator (thay device ID nếu máy bạn khác):

```powershell
adb -s emulator-5554 install -r .\build\app\outputs\flutter-apk\app-debug.apk
adb -s emulator-5556 install -r .\build\app\outputs\flutter-apk\app-debug.apk
```

Mở app trên mỗi máy. Đăng nhập lần lượt bằng số điện thoại/mật khẩu Shipper A và Shipper B. Mỗi emulator có secure storage riêng nên token đăng nhập không bị dùng chung. Kéo xuống làm mới chợ đơn trên cả hai máy; đơn `uiRaceOrderId` phải xuất hiện ở trạng thái Pending.

> Với hai điện thoại thật, dùng `http://<LAN-IP-của-máy-tính>:8080` thay cho `10.0.2.2`, bảo đảm Windows Firewall cho phép kết nối vào cổng 8080. Cleartext HTTP hiện chỉ bật trong Android debug manifest; không dùng cấu hình test này cho bản phát hành.

### 5.3 Thực hiện race trên UI

1. Trên cả hai máy, xác nhận cùng `orderCode`/địa chỉ của `uiRaceOrderId`.
2. Mở chi tiết chính đơn đó trên cả hai app trước khi thao tác.
3. Một người đếm “3–2–1”; hai shipper bấm **Nhận đơn** gần như đồng thời.
4. Chờ tối đa vài giây để hai request hoàn tất.

**Kết quả mong đợi nếu loser nhận HTTP 409:**

- Một app chuyển đơn sang trạng thái đã nhận (`Accepted`).
- App nhận 409 không crash; hiện Snackbar: **“Rất tiếc, đơn hàng này đã có tài xế khác nhanh tay nhận mất rồi!”**
- App thua cuộc quay lại chợ đơn và tự làm mới; `uiRaceOrderId` biến mất khỏi danh sách vì trạng thái không còn Pending.
- API chỉ gán đơn cho đúng một Shipper.

## 6. Đánh giá kết quả và xử lý trường hợp lệch

| Kết quả | Đánh giá |
|---|---|
| Một `200`, một `409`; client thua hiện đúng Snackbar và đơn biến mất khỏi chợ | PASS toàn bộ race + UI |
| Một `200`, một `409`; app crash hoặc không refresh | Backend PASS, UI FAIL |
| Một `200`, một `400` | FAIL: nhánh nhận đơn đã hết Pending phải được ánh xạ thành `409 Conflict`; kiểm tra backend exception mapping và lặp lại với đơn Pending mới |
| Hai request thành công | FAIL: kiểm tra concurrency/RowVersion và transaction backend |
| Có `401`/`403` | FAIL: kiểm tra token, role và Authorization Bearer |
| Có `500` | FAIL: kiểm tra log API và cấu hình EF Core/trigger SQL Server |

Backend trả `409 Conflict` cho request nhận một đơn không còn Pending, kể cả khi request thứ hai tới sau khi request đầu đã commit. Vì vậy cả tranh chấp RowVersion lẫn request đến muộn đều đi qua cùng nhánh xử lý Snackbar và refresh ở client. Không dùng lại đơn đã nhận để retry.

## 7. Hoàn tất vòng đời đơn hàng

Sau khi xác nhận race, dùng token của Shipper thắng để kiểm tra các chuyển trạng thái tiếp theo. Tạo Postman request **PUT** `{{baseUrl}}/api/orders/{{uiRaceOrderId}}/status`, Authorization Bearer là token shipper thắng, Body JSON:

```json
{ "newStatus": 2 }
```

Gửi lần lượt các giá trị `2`, `3`, `4`; mỗi request phải trả `200 OK` và status lần lượt là `PickedUp`, `Delivering`, `Completed`. Sau đó:

- **GET** `{{baseUrl}}/api/orders/{{uiRaceOrderId}}` với token Shipper thắng: xác nhận status `4` và driver đúng.
- **GET** `{{baseUrl}}/api/orders/my-orders` với `customerToken`: xác nhận Customer thấy đơn hoàn thành.
- Trên app Shipper thua, đơn không xuất hiện trong chợ Pending.

Để kiểm tra nhánh hủy của Customer riêng, tạo thêm một đơn mới rồi gửi **PUT** `{{baseUrl}}/api/orders/{{cancelTestOrderId}}/cancel` với `customerToken`. Kỳ vọng `204 No Content`, status `5`, và đơn không xuất hiện ở `/api/orders/available`.

## 8. Kết thúc

Tài khoản và đơn E2E được lưu trong SQL volume để có thể xem lại. Sau khi xong, có thể dừng container nhưng giữ dữ liệu bằng:

```powershell
docker compose down
```

Không thêm `-v` nếu muốn giữ database.
