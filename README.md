# GroupNine - Website bán sách

Website bán sách trực tuyến xây dựng bằng ASP.NET Core MVC và SQL Server.
Tài liệu chi tiết về kiến trúc, quyền truy cập và các luồng chức năng được duy
trì tại [SYSTEM-DOCUMENTATION.md](./SYSTEM-DOCUMENTATION.md).

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
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=GroupNine;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Tạo database bằng SQL Server Management Studio:

1. Mở file `scripts/init-database.sql`.
2. Kết nối đúng SQL Server instance.
3. Bấm **Execute**.

Hoặc dùng PowerShell với `sqlcmd`:

```powershell
sqlcmd -S ".\MSSQLSERVER01" -E -i ".\scripts\init-database.sql"
```

Script chỉ tạo database `GroupNine`. Khi ứng dụng khởi động,
`Data/DbInitializer.cs` dùng `EnsureCreated` để tạo schema nếu cần, chạy một số
câu lệnh SQL bổ sung/cập nhật schema cũ, đồng bộ 7 danh mục, thêm sách mẫu,
role và tài khoản demo. Đây không phải quy trình EF Core migrations; không cần
chạy migration thủ công.

Database trên instance mặc định của dự án hiện đã mang tên `GroupNine`. Nếu cài
đặt khác vẫn có database cũ `LatTrangBookStore` chứa dữ liệu, hãy sao lưu và
xác nhận chưa có database `GroupNine` trước khi đổi tên. Không chạy script tạo
database trước khi quyết định cách chuyển dữ liệu, vì script không sao chép
database cũ. Để giữ nguyên dữ liệu, dừng ứng dụng, kết nối SQL Server vào
database `master` và chạy:

```sql
ALTER DATABASE [LatTrangBookStore] MODIFY NAME = [GroupNine];
```

Sau đó chạy ứng dụng với connection string mặc định. Nếu cả hai database cùng
tồn tại, chọn đúng database có dữ liệu cần dùng và sao lưu trước mọi thao tác.

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

Ứng dụng vẫn chạy `DbInitializer` khi khởi động; các cập nhật schema thủ công
chỉ bao phủ những thay đổi đã được xử lý trong mã nguồn, không thay thế
migrations cho schema tùy biến.

## Đã triển khai

- Trang chủ theo nhận diện GroupNine, banner, sách đề xuất, sách xếp theo đánh
  giá và sách đang khuyến mãi.
- Danh sách sách với tìm kiếm, lọc danh mục và sắp xếp theo giá/đánh giá.
  Điều hướng “Được đánh giá cao” sắp theo điểm đánh giá.
- Trang chi tiết sách, giá khuyến mãi, tồn kho và đánh giá sao.
- Giỏ hàng lưu bằng Session, cập nhật số lượng và xóa sản phẩm.
- Checkout lưu tên, số điện thoại và địa chỉ; tổng tiền hệ thống chỉ tính tiền sách.
  Nhân viên dùng thông tin này để đặt COD qua đơn vị giao hàng bên ngoài; phí
  giao hàng do khách thanh toán trực tiếp cho shipper và không thuộc hệ thống.
- Trừ tồn kho sau khi đặt hàng và thông báo mã đơn hàng; trạng thái ban đầu là `Cần xác nhận`.
- Khách có thể đánh giá từng sách từ 1 đến 5 sao sau khi đơn hoàn tất.
- Dashboard Admin có thống kê tổng quan; tồn kho được quản lý trong trang sách.
- Responsive mobile, Bootstrap 5 và nhóm font sans-serif phổ biến (Arial/Roboto/Helvetica/Tahoma).

## Database và tài khoản demo

Ứng dụng hiện dùng SQL Server + Entity Framework Core. Script
`scripts/init-database.sql` chỉ tạo database rỗng `GroupNine` trên
instance `.\\MSSQLSERVER01`; `DbInitializer` khởi tạo schema bằng
`EnsureCreated` khi cần, thực hiện một số cập nhật schema có điều kiện và seed
dữ liệu, role `Admin`/`Customer`. Đây không phải quy trình EF Core migrations.

- Admin: `admin@groupnine.vn` / `Admin123!`
- Customer: `customer@groupnine.vn` / `Customer123!`
- Admin mẫu: `admin1@gmail.com`, `admin2@gmail.com`, `admin3@gmail.com` / `12345678`
- Khách hàng mẫu: `khachhangmau1@gmail.com`, `khachhangmau2@gmail.com`, `khachhangmau3@gmail.com` / `12345678`
- Đăng nhập: `/Account/Login`
- Đăng ký: `/Account/Register`
- Quản trị sách: `/Admin` (chỉ role Admin truy cập được)
- Admin có giao diện riêng, không dùng header/footer cửa hàng; gồm thống kê hệ thống (tồn kho, doanh thu, đơn hàng, sách/danh mục bán chạy từ các đơn hoàn tất), CRUD sách, quản lý kho, danh mục, đơn hàng và ưu đãi.
- Quản lý ưu đãi tại `/Admin/Promotions`: lọc theo tên/mã sách hoặc danh mục, áp dụng giảm theo phần trăm hoặc số tiền trực tiếp, và xóa ưu đãi từng sách.
- Giỏ hàng và đặt hàng yêu cầu đăng nhập; khách chưa đăng nhập sẽ được chuyển tới `/Account/Login`.

Schema Identity và nghiệp vụ được khởi tạo từ `AppDbContext`/`EnsureCreated`,
còn dữ liệu mẫu và các cập nhật schema bổ sung do `DbInitializer` thực hiện.
Connection string mặc định nằm trong `appsettings.json`. Khi ứng dụng khởi động,
initializer đổi email và tên đăng nhập của hai tài khoản demo cũ từ miền
`latrang.vn` sang `groupnine.vn`, giữ nguyên ID, vai trò và mật khẩu; nếu email
mới đã thuộc tài khoản khác, initializer báo lỗi thay vì gộp tài khoản.
Email liên hệ trong góp ý của khách hàng demo cũng được cập nhật.

## Xử lý lỗi thường gặp

- **Không kết nối được SQL Server**: kiểm tra dịch vụ SQL Server đang chạy, tên instance
  trong connection string và Windows Account có quyền truy cập database.
- **Ứng dụng không khởi động được sau khi tạo database**: kiểm tra kết nối,
  quyền trên SQL Server và thông báo lỗi của `DbInitializer` trong terminal.
- **Cổng 5080 đang được sử dụng**: dùng cổng khác, ví dụ
  `dotnet run --urls http://localhost:5081`.
- **`sqlcmd` không được nhận diện**: chạy file SQL bằng SQL Server Management Studio
  hoặc cài SQL Server Command Line Utilities.

## Lưu ý bảo mật

Tài khoản demo chỉ phục vụ chấm bài/thử nghiệm. Khi triển khai thật, hãy đổi mật khẩu,
không commit connection string có mật khẩu vào GitHub và dùng User Secrets hoặc biến
môi trường.
