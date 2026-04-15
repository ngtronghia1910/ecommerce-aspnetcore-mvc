# Website bán hàng — ASP.NET Core MVC

Ứng dụng thương mại điện tử mẫu: khách xem sản phẩm, tìm kiếm, giỏ hàng, đặt hàng; quản trị viên quản lý danh mục, sản phẩm, đơn hàng và xem danh sách người dùng. Kiến trúc **MVC**, dữ liệu **Entity Framework Core** + **SQL Server**, xác thực **ASP.NET Core Identity** với phân quyền theo vai trò.

---

## Mục lục

1. [Công nghệ sử dụng](#công-nghệ-sử-dụng)
2. [Cấu trúc solution](#cấu-trúc-solution)
3. [Yêu cầu môi trường](#yêu-cầu-môi-trường)
4. [Cài đặt và chạy](#cài-đặt-và-chạy)
5. [Cấu hình cơ sở dữ liệu](#cấu-hình-cơ-sở-dữ-liệu)
6. [Migration và seed dữ liệu](#migration-và-seed-dữ-liệu)
7. [Tài khoản mặc định](#tài-khoản-mặc-định)
8. [Chức năng theo vai trò](#chức-năng-theo-vai-trò)
9. [API REST](#api-rest)
10. [Upload ảnh sản phẩm](#upload-ảnh-sản-phẩm)
11. [Thanh toán VNPay / MoMo](#thanh-toán-vnpay--momo)
12. [Xử lý sự cố thường gặp](#xử-lý-sự-cố-thường-gặp)

---

## Công nghệ sử dụng

| Lớp | Công nghệ |
|-----|-----------|
| Framework | ASP.NET Core 8.0 (MVC) |
| ORM | Entity Framework Core 8 |
| Cơ sở dữ liệu | SQL Server (mặc định: **LocalDB**) |
| Xác thực / phân quyền | ASP.NET Core Identity + vai trò (`Admin`, `Customer`) |
| Giao diện | Razor Views, HTML/CSS, **Bootstrap 5** |

---

## Cấu trúc solution

```
NewProject/
├── ECommerce.slnx              # Solution (Visual Studio / VS Code)
├── README.md                   # Tài liệu này
└── ECommerceWeb/               # Project web chính
    ├── Areas/
    │   └── Admin/              # Khu vực quản trị (Controllers + Views)
    ├── Controllers/            # MVC + Api + Payment (callback VNPay/MoMo)
    ├── Services/               # VNPay (URL + chữ ký), MoMo (create + verify)
    ├── Options/                # Cấu hình VNPay / MoMo (appsettings)
    ├── Data/
    │   ├── ApplicationDbContext.cs
    │   ├── DbInitializer.cs    # Seed vai trò, admin, danh mục, sản phẩm mẫu
    │   └── Migrations/         # EF Core migrations
    ├── Models/                 # Entity + ViewModels
    ├── Views/                  # Razor (storefront + Account)
    ├── wwwroot/                # CSS, JS, ảnh tĩnh, uploads
    ├── Program.cs              # Đăng ký DI, pipeline, seed khi khởi động
    └── appsettings.json        # Connection string, logging
```

---

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** hoặc **SQL Server LocalDB** (Windows, thường đi kèm Visual Studio)
- (Tuỳ chọn) [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) để tạo/cập nhật migration từ CLI:

  ```bash
  dotnet tool install --global dotnet-ef --version 8.0.x
  ```

---

## Cài đặt và chạy

1. Clone hoặc giải nén project vào máy.
2. Mở terminal tại thư mục project web:

   ```powershell
   cd "đường-dẫn\NewProject\ECommerceWeb"
   ```

3. Khôi phục package và build:

   ```bash
   dotnet restore
   dotnet build
   ```

4. Chạy ứng dụng:

   ```bash
   dotnet run
   ```

5. Mở trình duyệt theo URL hiển thị (ví dụ `https://localhost:7xxx` hoặc `http://localhost:5xxx` — xem `Properties/launchSettings.json`).

Lần **chạy đầu tiên**, ứng dụng sẽ:

- Áp dụng migration còn thiếu (`MigrateAsync()` trong `DbInitializer`).
- Tạo vai trò, tài khoản admin (nếu chưa có), danh mục và sản phẩm mẫu (nếu bảng `Categories` trống).

---

## Cấu hình cơ sở dữ liệu

Chuỗi kết nối nằm trong `ECommerceWeb/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ECommerceWebDb;..."
}
```

### Dùng SQL Server đầy đủ (không dùng LocalDB)

Thay `DefaultConnection` bằng chuỗi của bạn, ví dụ:

```text
Server=YOUR_SERVER;Database=ECommerceWebDb;User Id=...;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Sau đó chạy lại migration (xem mục dưới) trên instance đó nếu database chưa tồn tại.

---

## Migration và seed dữ liệu

### Áp dụng migration từ CLI (khuyến nghị khi deploy / CI)

```powershell
cd ECommerceWeb
dotnet ef database update
```

### Tạo migration mới (sau khi đổi model)

```powershell
cd ECommerceWeb
dotnet ef migrations add TenMigration --output-dir Data/Migrations
dotnet ef database update
```

**Lưu ý:** `Program.cs` gọi `DbInitializer.SeedAsync()` khi startup; trong đó có `MigrateAsync()`. Trên môi trường production, nên cân nhắc tách migration ra pipeline deploy thay vì tự migrate mỗi lần chạy app.

---

## Tài khoản mặc định

Được tạo trong `Data/DbInitializer.cs` (chỉ khi chưa tồn tại user có email tương ứng):

| Trường | Giá trị |
|--------|---------|
| Email / đăng nhập | `admin@shop.local` |
| Mật khẩu | `Admin@123` |
| Vai trò | **Admin** |

**Khuyến nghị bảo mật:** đổi mật khẩu ngay sau khi triển khai thật; không commit mật khẩu production vào git — dùng User Secrets hoặc biến môi trường.

Người dùng thường đăng ký qua `/Account/Register` và được gán vai trò **Customer**.

---

## Chức năng theo vai trò

### Khách / Customer (đã đăng nhập)

| Chức năng | Route gợi ý |
|-----------|-------------|
| Đăng ký / Đăng nhập / Đăng xuất | `/Account/Register`, `/Account/Login`, POST `/Account/Logout` |
| Trang chủ, sản phẩm nổi bật | `/` |
| Danh sách, tìm kiếm, lọc danh mục | `/Products`, query `?q=...&categoryId=...` |
| Chi tiết sản phẩm, thêm giỏ | `/Products/Details/{id}` |
| Giỏ hàng (cập nhật / xóa) | `/Cart` |
| Checkout (COD / VNPay / MoMo) | `/Orders/Checkout` |
| Lịch sử đơn, chi tiết đơn của mình | `/Orders/MyOrders`, `/Orders/Details/{id}` |

Khách **chưa đăng nhập** vẫn xem được sản phẩm; thêm giỏ / đặt hàng cần đăng nhập.

### Admin

| Chức năng | Route |
|-----------|--------|
| Dashboard | `/Admin` hoặc `/Admin/Home` |
| CRUD sản phẩm + upload ảnh | `/Admin/Products` |
| CRUD danh mục | `/Admin/Categories` |
| Danh sách đơn, chi tiết, đổi trạng thái | `/Admin/Orders` |
| Danh sách người dùng + vai trò | `/Admin/Users` |

Truy cập khu Admin yêu cầu vai trò **Admin**; nếu không đủ quyền sẽ về `/Account/AccessDenied`.

---

## API REST

Controller: `Controllers/Api/ProductsApiController.cs`

| Phương thức | URL | Mô tả |
|------------|-----|--------|
| GET | `/api/ProductsApi` | Danh sách sản phẩm; query tuỳ chọn: `?q=ten`, `?categoryId=1` |
| GET | `/api/ProductsApi/{id}` | Chi tiết một sản phẩm |

Trả về JSON (mặc định camelCase). Không bắt buộc đăng nhập cho hai endpoint trên.

---

## Upload ảnh sản phẩm

- Admin tạo/sửa sản phẩm có thể chọn file ảnh.
- File được lưu tại: `wwwroot/uploads/products/` với tên ngẫu nhiên (GUID + phần mở rộng).
- Đường dẫn lưu trong DB dạng `/uploads/products/...`.
- Nếu không upload, có thể dùng ảnh mặc định `/images/placeholder-product.svg`.

---

## Thanh toán VNPay / MoMo

Trang checkout cho phép chọn **COD**, **VNPay** hoặc **MoMo**. Đơn online được tạo với trạng thái **Chờ thanh toán** (`AwaitingPayment`); tồn kho chỉ bị trừ sau khi cổng xác nhận thanh toán thành công.

### Cấu hình `appsettings.json`

Thêm / chỉnh các khóa sandbox (lấy từ cổng VNPay / MoMo khi đăng ký merchant test):

**VNPay** (`VNPay`):

| Khóa | Ý nghĩa |
|------|---------|
| `TmnCode` | Mã website (Terminal ID) |
| `HashSecret` | Chuỗi bí mật ký HMAC SHA512 |
| `ReturnUrl` | *(Tuỳ chọn)* URL tuyệt đối public; nếu để trống, app dùng `Url.Action` theo `Request` khi tạo link thanh toán. |
| `IpnUrl` | *(Tuỳ chọn)* URL server nhận IPN; **localhost không nhận được IPN** — cần domain public hoặc tunnel (ngrok). |
| `PaymentUrl` | Mặc định sandbox: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html` |

**MoMo** (`Momo`):

| Khóa | Ý nghĩa |
|------|---------|
| `PartnerCode`, `AccessKey`, `SecretKey` | Thông tin cổng sandbox |
| `ReturnUrl`, `NotifyUrl` | *(Tuỳ chọn)* Nếu để trống, app gắn URL động từ request (Notify vẫn cần public URL để MoMo gọi server). |
| `Endpoint` | Mặc định: `https://test-payment.momo.vn/v2/gateway/api/create` |

Khi chưa điền đủ khóa VNPay/MoMo, lựa chọn tương ứng trên form checkout sẽ bị vô hiệu hoá.

### Route callback (không cần đăng nhập; có kiểm tra chữ ký)

| Cổng | Route | Ghi chú |
|------|--------|--------|
| VNPay return | `GET /Payment/VnpayReturn` | Khách quay lại sau thanh toán |
| VNPay IPN | `GET /Payment/VnpayIpn` | Server VNPay gọi (cần URL public) |
| MoMo return | `GET /Payment/MomoReturn` | Redirect sau thanh toán |
| MoMo notify | `POST /Payment/MomoNotify` | IPN JSON (cần URL public) |

Mã đối chiếu đơn: `Orders.GatewayTxnRef` (trùng `vnp_TxnRef` / `orderId` gửi lên cổng).

### Code liên quan

- `Services/VnpayService.cs` — tạo URL thanh toán, kiểm tra `vnp_SecureHash`.
- `Services/MomoPaymentService.cs` — gọi API `create`, kiểm tra chữ ký return/notify.
- `Controllers/PaymentController.cs` — xử lý callback, cập nhật `PaymentStatus`, trừ tồn kho (idempotent).
- `Controllers/OrdersController.cs` — tạo đơn và redirect sang VNPay / MoMo.

---

## Xử lý sự cố thường gặp

### Không kết nối được SQL Server / LocalDB

- Kiểm tra SQL Server hoặc LocalDB đã cài và đang chạy.
- Đổi `ConnectionStrings:DefaultConnection` cho đúng instance của bạn.
- Thử chạy: `dotnet ef database update` để tạo database thủ công.

### Lỗi migration / database đã tồn tại nhưng schema cũ

- Đồng bộ bằng `dotnet ef database update`.
- Tránh xóa tay bảng Identity nếu không hiểu ràng buộc FK.

### Quên mật khẩu admin

- Có thể tạo user admin mới bằng code seed (đổi email trong `DbInitializer`) hoặc dùng SQL / tool quản lý Identity (nâng cao).

### Port đã bị chiếm

- Sửa `applicationUrl` trong `Properties/launchSettings.json` hoặc chạy: `dotnet run --urls "http://localhost:5050"`.

### VNPay / MoMo báo chữ ký không hợp lệ hoặc không tạo được link

- Kiểm tra đúng môi trường **sandbox** vs **production** (`PaymentUrl` / `Endpoint`).
- VNPay: `HashSecret` phải khớp merchant; tham số ký loại **HMAC-SHA512** (theo tài liệu v2).
- MoMo: thứ tự chuỗi raw ký phải đúng spec; `ReturnUrl` / `NotifyUrl` phải khớp chính xác URL đã gửi trong request `create`.
- IPN/Notify từ internet **không gọi được** `localhost` — dùng URL public hoặc tunnel khi test IPN.

---

## License / đóng góp

Project mẫu cho học tập và portfolio. Bạn có thể tự do chỉnh sửa theo nhu cầu (bổ sung email xác nhận đơn, đổi flow hoàn tiền, v.v.).

---

*Tài liệu được tạo cho solution trong thư mục `NewProject`, project chính `ECommerceWeb`.*
