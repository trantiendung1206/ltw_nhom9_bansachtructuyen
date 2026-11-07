# GroupNine - Website bán sách

Website bán sách trực tuyến xây dựng bằng ASP.NET Core MVC và SQL Server.

## Yêu cầu môi trường

- Windows 10/11.
- .NET 10 SDK: `https://dotnet.microsoft.com/download/dotnet/10.0`.
- SQL Server Express/Developer và SQL Server Management Studio (hoặc `sqlcmd`).
- Git.

## Cài đặt và chạy sau khi clone

Mở PowerShell tại thư mục project:

```powershell
git clone https://github.com/trantiendung1206/ltw_nhom9_bansachtructuyen.git
cd ltw_nhom9_bansachtructuyen
dotnet restore
```

### 1. Tạo database

Mặc định project dùng SQL Server instance:

```text
.\MSSQLSERVER01
```

Nếu máy bạn dùng instance khác, sửa `ConnectionStrings:DefaultConnection` trong
`appsettings.json`, ví dụ:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=LatTrangBookStore;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Tạo database bằng SQL Server Management Studio:

1. Mở file `scripts/init-database.sql`.
2. Kết nối đúng SQL Server instance.
3. Bấm **Execute**.

Hoặc dùng PowerShell với `sqlcmd`:

```powershell
sqlcmd -S ".\MSSQLSERVER01" -E -i ".\scripts\init-database.sql"
```

Script chỉ tạo database `LatTrangBookStore`. Khi ứng dụng chạy lần đầu,
`Data/DbInitializer.cs` sẽ tự tạo toàn bộ bảng Identity và bảng nghiệp vụ,
cập nhật schema, tạo 7 danh mục, thêm sách mẫu, role và tài khoản demo.
Không cần chạy thêm migration thủ công.

### 2. Chạy website

```powershell
dotnet run
```

Mở URL được hiển thị trong terminal, thường là `http://localhost:5080`.

Để chạy cố định trên cổng 5080:

```powershell
dotnet run --urls http://localhost:5080
```

Khu vực quản lý: `http://localhost:5080/Admin`.

### 3. Nếu database đã tồn tại

Có thể bỏ qua bước chạy SQL script và chạy thẳng:

```powershell
dotnet restore
dotnet run
```

Ứng dụng vẫn kiểm tra và bổ sung schema/dữ liệu cần thiết khi khởi động.

## Đã triển khai

- Trang chủ theo nhận diện GroupNine, banner, danh mục và sách mới.
- Danh sách sách với tìm kiếm, lọc danh mục và sắp xếp theo giá/đánh giá.
- Trang chi tiết sách, giá khuyến mãi, tồn kho và đánh giá sao.
- Giỏ hàng lưu bằng Session, cập nhật số lượng và xóa sản phẩm.
- Checkout COD giao tận nơi, tổng tiền chỉ tính tiền sách.
- Trừ tồn kho sau khi đặt hàng và thông báo mã đơn hàng; trạng thái ban đầu là `Cần xác nhận`.
- Dashboard quản lý mẫu với thống kê và bảng quản lý tồn kho.
- Responsive mobile, Bootstrap 5 và nhóm font sans-serif phổ biến (Arial/Roboto/Helvetica/Tahoma).

## Database và tài khoản demo

Ứng dụng hiện dùng SQL Server + Entity Framework Core. Script
`scripts/init-database.sql` chỉ tạo database rỗng `LatTrangBookStore` trên
instance `.\\MSSQLSERVER01`; khi chạy lần đầu, `DbInitializer` dùng
`EnsureCreated` để tạo bảng Identity/nghiệp vụ, cập nhật schema và seed sách,
role `Admin`/`Customer`.

- Admin: `admin@latrang.vn` / `Admin123!`
- Customer: `customer@latrang.vn` / `Customer123!`
- Admin mẫu: `admin1@gmail.com`, `admin2@gmail.com`, `admin3@gmail.com` / `12345678`
- Khách hàng mẫu: `khachhangmau1@gmail.com`, `khachhangmau2@gmail.com`, `khachhangmau3@gmail.com` / `12345678`
- Đăng nhập: `/Account/Login`
- Đăng ký: `/Account/Register`
- Quản trị sách: `/Admin` (chỉ role Admin truy cập được)
- Admin có giao diện riêng, không dùng header/footer cửa hàng; gồm thống kê hệ thống (tồn kho, doanh thu, đơn hàng, sách/danh mục bán chạy), CRUD sách, quản lý kho, danh mục, đơn hàng và ưu đãi.
- Quản lý ưu đãi tại `/Admin/Promotions`: lọc theo tên/mã sách hoặc danh mục, áp dụng giảm theo phần trăm hoặc số tiền trực tiếp, và xóa ưu đãi từng sách.
- Giỏ hàng và đặt hàng yêu cầu đăng nhập; khách chưa đăng nhập sẽ được chuyển tới `/Account/Login`.

Các bảng Identity và dữ liệu sách được tạo tự động bằng `EnsureCreated`. Connection string nằm trong `appsettings.json`.

## Xử lý lỗi thường gặp

- **Không kết nối được SQL Server**: kiểm tra dịch vụ SQL Server đang chạy, tên instance
  trong connection string và Windows Account có quyền truy cập database.
- **Database bị khóa khi build**: dừng tiến trình website cũ rồi chạy lại `dotnet run`.
- **Cổng 5080 đang được sử dụng**: dùng cổng khác, ví dụ
  `dotnet run --urls http://localhost:5081`.
- **`sqlcmd` không được nhận diện**: chạy file SQL bằng SQL Server Management Studio
  hoặc cài SQL Server Command Line Utilities.

## Lưu ý bảo mật

Tài khoản demo chỉ phục vụ chấm bài/thử nghiệm. Khi triển khai thật, hãy đổi mật khẩu,
không commit connection string có mật khẩu vào GitHub và dùng User Secrets hoặc biến
môi trường.
