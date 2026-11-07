# Lật Trang - Website bán sách

MVP ASP.NET Core MVC (.NET 8) theo kế hoạch đồ án website bán sách.

## Chạy dự án

```powershell
dotnet restore
dotnet run
```

Sau đó mở URL được hiển thị trong terminal. Khu vực quản lý mẫu: `/Admin`.

## Khởi tạo SQL Server

1. Mở `scripts/init-database.sql` bằng SQL Server Management Studio và chạy script.
2. Kiểm tra connection string trong `appsettings.json`:
   `Server=.\\MSSQLSERVER01;Database=LatTrangBookStore;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`
3. Chạy `dotnet run`. Lần chạy đầu tiên, `Data/DbInitializer.cs` dùng `EnsureCreated` để tạo toàn bộ bảng, cập nhật schema và seed dữ liệu mẫu.

Có thể tạo database trực tiếp bằng PowerShell:

```powershell
sqlcmd -S ".\MSSQLSERVER01" -E -i ".\scripts\init-database.sql"
```

## Đã triển khai

- Trang chủ theo nhận diện Lật Trang, banner, danh mục và sách mới.
- Danh sách sách với tìm kiếm, lọc danh mục và sắp xếp theo giá/đánh giá.
- Trang chi tiết sách, giá khuyến mãi, tồn kho và đánh giá sao.
- Giỏ hàng lưu bằng Session, cập nhật số lượng và xóa sản phẩm.
- Checkout COD giao tận nơi.
- Trừ tồn kho sau khi đặt hàng và thông báo mã đơn hàng.
- Dashboard quản lý mẫu với thống kê và bảng quản lý tồn kho.
- Responsive mobile, Bootstrap 5 và nhóm font sans-serif phổ biến (Arial/Roboto/Helvetica/Tahoma).

## Ghi chú mở rộng

## Database và tài khoản demo

Ứng dụng hiện dùng SQL Server + Entity Framework Core. Database `LatTrangBookStore` được tạo trên instance `.\\MSSQLSERVER01`; khi chạy lần đầu, ứng dụng tạo bảng Identity, seed sách và tạo role `Admin`/`Customer`.

- Admin: `admin@latrang.vn` / `Admin123!`
- Customer: `customer@latrang.vn` / `Customer123!`
- Admin mẫu: `admin1@gmail.com`, `admin2@gmail.com`, `admin3@gmail.com` / `12345678`
- Khách hàng mẫu: `khachhangmau1@gmail.com`, `khachhangmau2@gmail.com`, `khachhangmau3@gmail.com` / `12345678`
- Đăng nhập: `/Account/Login`
- Đăng ký: `/Account/Register`
- Quản trị sách: `/Admin` (chỉ role Admin truy cập được)
- Admin có giao diện riêng, không dùng header/footer cửa hàng; gồm thống kê hệ thống (tồn kho, doanh thu, đơn hàng, sách/danh mục bán chạy), CRUD sách, quản lý kho, danh mục, đơn hàng và ưu đãi.
- Quản lý ưu đãi tại `/Admin/Promotions`: lọc theo tên/mã sách hoặc danh mục, áp dụng giảm theo phần trăm hoặc số tiền trực tiếp, và xóa ưu đãi từng sách.
- Giỏ hàng, đặt hàng và yêu thích yêu cầu đăng nhập; khách chưa đăng nhập sẽ được chuyển tới `/Account/Login`.

Các bảng Identity và dữ liệu sách được tạo tự động bằng `EnsureCreated`. Connection string nằm trong `appsettings.json`.
