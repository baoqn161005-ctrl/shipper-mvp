# ĐẶC TẢ KỸ THUẬT MVP: ỨNG DỤNG GIAO HÀNG (SHIPPER APP)

- **Thời gian thực hiện:** 2 tuần (14 ngày)
- **Công nghệ sử dụng:**
  - **Backend:** C# .NET Core Web API (.NET 8) + Entity Framework Core
  - **Database:** Microsoft SQL Server
  - **Mobile App:** Flutter (1 codebase dùng chung cho cả Khách hàng & Shipper, phân luồng theo Role)
- **Mục tiêu:** Hoàn thiện luồng cốt lõi từ lúc tạo đơn đến khi giao hàng thành công.

---

## 1. PHẠM VI DỰ ÁN (SCOPE)

### ✅ Trong phạm vi (In-Scope)
- Đăng ký / Đăng nhập cơ bản (Email/SĐT + Mật khẩu, phân quyền `Customer` hoặc `Shipper`).
- Khách hàng: Tạo đơn hàng (nhập tay địa chỉ), xem danh sách đơn, theo dõi trạng thái, hủy đơn (khi chưa có Shipper nhận).
- Shipper: Xem danh sách đơn đang chờ (`Pending`), bấm nhận đơn, cập nhật trạng thái giao hàng, xem lịch sử đơn đã nhận.

### ❌ Ngoài phạm vi (Out-of-Scope - Cắt bỏ hoàn toàn)
- Ví điện tử, thanh toán online, đối soát dòng tiền (Mặc định thu tiền mặt COD hoặc tự thỏa thuận).
- Bản đồ GPS Live Tracking, tính khoảng cách qua Google Maps API (Chỉ dùng địa chỉ dạng text).
- Chat real-time hoặc gọi điện in-app (Dùng SĐT hiển thị trên đơn để gọi trực tiếp qua app điện thoại mặc định).
- Xác thực OTP qua SMS, Upload ảnh chứng minh giao hàng (POD), Push Notification (FCM).
- Trang Web Admin quản trị (Quản lý trực tiếp qua SQL Server hoặc Swagger API trong giai đoạn MVP).

---

## 2. LUỒNG NGHIỆP VỤ CỐT LÕI (CORE FLOW & STATE MACHINE)

### Luồng đi chính
`Tạo đơn (Customer)` ➔ `Chợ đơn hàng (Pending)` ➔ `Shipper nhận đơn (Accepted)` ➔ `Đã lấy hàng (PickedUp)` ➔ `Đang giao (Delivering)` ➔ `Hoàn thành (Completed)`

### Bảng chuyển đổi trạng thái đơn hàng (Order Status)
| Trạng thái (Enum/Int) | Tên hiển thị | Người thao tác | Hành động cho phép tiếp theo |
| :--- | :--- | :--- | :--- |
| `0 - Pending` | Đang tìm Shipper | Customer / Shipper | Shipper bấm **Nhận đơn** (`Accepted`) / Customer bấm **Hủy** (`Cancelled`) |
| `1 - Accepted` | Đã có Shipper nhận | Shipper | Shipper bấm **Đã lấy hàng** (`PickedUp`) |
| `2 - PickedUp` | Đã lấy hàng | Shipper | Shipper bấm **Bắt đầu giao** (`Delivering`) |
| `3 - Delivering` | Đang giao hàng | Shipper | Shipper bấm **Hoàn thành** (`Completed`) |
| `4 - Completed` | Giao thành công | Hệ thống | Trạng thái cuối (Kết thúc) |
| `5 - Cancelled` | Đã hủy | Customer | Trạng thái cuối (Chỉ hủy được khi ở trạng thái `0 - Pending`) |

---

## 3. DANH SÁCH TÍNH NĂNG CHI TIẾT (MVP)

### Module 1: Xác thực (Auth - Dùng chung)
- **Đăng ký:** Nhập Họ tên, Số điện thoại (dùng làm Username), Mật khẩu, Chọn vai trò (`Customer` hoặc `Shipper`).
- **Đăng nhập:** Nhập SĐT + Mật khẩu ➔ Trả về JWT Token + `Role` + `UserId`.
- **Điều hướng:** App Flutter lưu Token vào `SharedPreferences`/`SecureStorage`. Nếu `Role == Customer` vào màn hình Khách hàng; nếu `Role == Shipper` vào màn hình Shipper.

### Module 2: Dành cho Khách hàng (Customer)
1. **Tạo đơn hàng mới:**
   - Nhập địa chỉ lấy hàng (Text) + SĐT người gửi (mặc định điền SĐT user).
   - Nhập địa chỉ giao hàng (Text) + Tên & SĐT người nhận.
   - Nhập tên hàng hóa / Ghi chú (VD: "Hộp tài liệu, dễ vỡ").
   - Phí giao hàng / Tiền thu hộ COD: Nhập số tiền cố định hoặc chọn gói cước đơn giản (VD: Nội thành 25.000đ).
2. **Danh sách đơn của tôi:**
   - Hiển thị danh sách đơn đã tạo (sắp xếp mới nhất lên đầu).
   - Kéo xuống để làm mới (Pull-to-refresh) cập nhật trạng thái đơn.
3. **Chi tiết đơn hàng & Hủy đơn:**
   - Xem thông tin đơn hàng và trạng thái hiện tại.
   - Hiển thị Tên + SĐT của Shipper sau khi đơn chuyển sang trạng thái `Accepted`.
   - Nút **Hủy đơn** (chỉ hiển thị khi trạng thái là `Pending`).

### Module 3: Dành cho Tài xế (Shipper)
1. **Danh sách đơn đang chờ (Chợ đơn hàng):**
   - Hiển thị các đơn có trạng thái `Pending`.
   - Kéo để làm mới (Pull-to-refresh).
   - Nút **Nhận đơn** ngay trên item hoặc trong trang chi tiết.
   - *Lưu ý kỹ thuật:* Backend cần xử lý Transaction hoặc check điều kiện `WHERE Id = @Id AND Status = 0` để tránh 2 Shipper nhận cùng 1 đơn.
2. **Đơn hàng của tôi (Đang chạy & Lịch sử):**
   - Tab 1 (Đang thực hiện): Các đơn có trạng thái `Accepted`, `PickedUp`, `Delivering`.
   - Tab 2 (Lịch sử): Các đơn có trạng thái `Completed`.
3. **Cập nhật trạng thái đơn hàng:**
   - Trong màn hình chi tiết đơn đang thực hiện, hiển thị 1 nút hành động chính duy nhất theo đúng thứ tự:
     - Đang `Accepted` ➔ Nút **"Xác nhận đã lấy hàng"** (`PickedUp`)
     - Đang `PickedUp` ➔ Nút **"Bắt đầu giao hàng"** (`Delivering`)
     - Đang `Delivering` ➔ Nút **"Hoàn thành giao hàng"** (`Completed`)
   - Có nút bấm gọi điện nhanh (`url_launcher`: `tel:...`) tới SĐT người gửi / người nhận.

---

## 4. THIẾT KẾ CƠ SỞ DỮ LIỆU (SQL SERVER)

Chỉ sử dụng **2 bảng chính** để tối ưu tốc độ phát triển:

### Bảng `Users`
| Cột | Kiểu dữ liệu | Ghi chú |
| :--- | :--- | :--- |
| `Id` | `INT` (PK, Identity) | Khóa chính |
| `FullName` | `NVARCHAR(100)` | Họ và tên |
| `PhoneNumber` | `VARCHAR(15)` (Unique) | Dùng để đăng nhập & liên hệ |
| `PasswordHash` | `NVARCHAR(255)` | Mật khẩu đã mã hóa (BCrypt) |
| `Role` | `VARCHAR(20)` | `'Customer'` hoặc `'Shipper'` |
| `CreatedAt` | `DATETIME2` | Ngày tạo |

### Bảng `Orders`
| Cột | Kiểu dữ liệu | Ghi chú |
| :--- | :--- | :--- |
| `Id` | `INT` (PK, Identity) | Mã đơn hàng |
| `CustomerId` | `INT` (FK -> Users.Id) | Người tạo đơn |
| `ShipperId` | `INT` (FK -> Users.Id, Nullable) | Người nhận giao (Null khi mới tạo) |
| `PickupAddress` | `NVARCHAR(255)` | Địa chỉ lấy hàng |
| `DeliveryAddress` | `NVARCHAR(255)` | Địa chỉ giao hàng |
| `ReceiverName` | `NVARCHAR(100)` | Tên người nhận |
| `ReceiverPhone` | `VARCHAR(15)` | SĐT người nhận |
| `ItemDescription` | `NVARCHAR(255)` | Mô tả hàng hóa / Ghi chú |
| `ShippingFee` | `DECIMAL(18,0)` | Phí ship (VNĐ) |
| `CodAmount` | `DECIMAL(18,0)` | Tiền thu hộ (Mặc định 0) |
| `Status` | `INT` | `0: Pending`, `1: Accepted`, `2: PickedUp`, `3: Delivering`, `4: Completed`, `5: Cancelled` |
| `CreatedAt` | `DATETIME2` | Thời gian tạo |
| `UpdatedAt` | `DATETIME2` | Thời gian cập nhật trạng thái cuối |

---

## 5. DANH SÁCH API CẦN XÂY DỰNG (.NET CORE WEB API)

Sử dụng JWT Bearer Authentication cho tất cả API trừ `Auth`.

### AuthController (`/api/auth`)
- `POST /api/auth/register` - Đăng ký tài khoản mới.
- `POST /api/auth/login` - Đăng nhập, trả về Token + User Info (Id, FullName, Role).

### OrdersController (`/api/orders`)
- `POST /api/orders` `[Role: Customer]` - Tạo đơn hàng mới (Status mặc định = `0`).
- `GET /api/orders/my-orders` `[Role: Customer]` - Lấy danh sách đơn hàng của Customer hiện tại.
- `PUT /api/orders/{id}/cancel` `[Role: Customer]` - Hủy đơn (Chỉ thành công nếu `Status == 0` và đúng `CustomerId`).
- `GET /api/orders/available` `[Role: Shipper]` - Lấy danh sách đơn đang ở trạng thái `0 (Pending)`.
- `GET /api/orders/shipper-orders` `[Role: Shipper]` - Lấy danh sách đơn của Shipper hiện tại (lọc theo đang chạy / lịch sử).
- `POST /api/orders/{id}/accept` `[Role: Shipper]` - Nhận đơn (Cập nhật `ShipperId` và chuyển `Status = 1`).
- `PUT /api/orders/{id}/status` `[Role: Shipper]` - Chuyển trạng thái tiếp theo (`1 -> 2 -> 3 -> 4`). Body: `{ "newStatus": 2 }`.
- `GET /api/orders/{id}` `[Role: Customer, Shipper]` - Xem chi tiết 1 đơn hàng (kèm SĐT Customer/Shipper để liên lạc).

---

## 6. KẾ HOẠCH THỰC HIỆN TRONG 2 TUẦN (14 NGÀY)

### Tuần 1: Backend (.NET Core + SQL) & Nền tảng Flutter
- **Ngày 1-2:** Tạo Database SQL Server, thiết lập Project .NET Core Web API, cấu hình EF Core, JWT Auth và làm xong `AuthController`.
- **Ngày 3-4:** Viết toàn bộ `OrdersController` (bao gồm validate chuyển đổi trạng thái hợp lệ và xử lý concurrency khi nhận đơn). Test toàn bộ luồng bằng Swagger/Postman.
- **Ngày 5:** Khởi tạo project Flutter, cấu trúc thư mục, cài package (`http`/`dio`, `shared_preferences`, `url_launcher`), làm màn hình Login & Register + lưu Token.
- **Ngày 6-7:** Xây dựng luồng giao diện **Customer** trên Flutter (Danh sách đơn, Form tạo đơn, Chi tiết đơn & Hủy đơn).

### Tuần 2: Luồng Shipper, Tích hợp & Kiểm thử
- **Ngày 8-9:** Xây dựng luồng giao diện **Shipper** trên Flutter (Chợ đơn hàng `Pending`, Nút nhận đơn, Tab Đơn đang chạy & Lịch sử).
- **Ngày 10:** Làm màn hình Chi tiết đơn phía Shipper (Nút đổi trạng thái theo luồng, Nút gọi điện thoại nhanh cho khách).
- **Ngày 11-12:** Kết nối toàn bộ App Flutter với API thật (chạy local qua IP mạng LAN hoặc deploy API lên VPS/Azure/Render). Test End-to-End 2 máy/2 giả lập cùng lúc (1 máy Customer tạo đơn, 1 máy Shipper nhận và giao).
- **Ngày 13:** Sửa lỗi (Bug fixing), xử lý các trường hợp biên (mất mạng, 2 shipper bấm nhận cùng 1 đơn, validate form trống).
- **Ngày 14:** Build file `.apk` (Android) để cài đặt thực tế và đóng gói phiên bản MVP.