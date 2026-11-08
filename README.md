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
`Data/DbInitializer.cs` dùng `EnsureCreated` để tạo schema nếu cần, sau đó xóa
dữ liệu nghiệp vụ, toàn bộ tài khoản/vai trò cũ **một lần** và tạo lại dữ liệu
demo mới. Hãy sao lưu nếu cần giữ dữ liệu cũ trước lần chạy đầu tiên sau thay đổi
này. Đây không phải quy trình EF Core migrations; không cần chạy migration thủ công.

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

- Trang chủ theo nhận diện GroupNine, banner, sách đề xuất ngẫu nhiên khi chưa
  có lịch sử mua hàng/giỏ hàng và đề xuất theo danh mục đã tương tác, sách xếp
  theo đánh giá và sách đang khuyến mãi.
- Danh sách sách với tìm kiếm, lọc danh mục/khuyến mãi, phân trang server-side,
  sắp xếp theo giá/đánh giá và cập nhật kết quả bằng AJAX qua REST API.
  Điều hướng “Được đánh giá cao” sắp theo điểm đánh giá.
- REST API công khai `GET /api/books` (lọc, sắp xếp, phân trang) và
  `GET /api/books/{id}` (chi tiết sách); Admin có thể POST/PUT/DELETE sách qua
  API sau khi đăng nhập và gửi anti-forgery token từ `/api/books/antiforgery`.
- Trang chi tiết sách, giá khuyến mãi, tồn kho và đánh giá sao.
- Giỏ hàng lưu bằng Session, cập nhật số lượng và xóa sản phẩm.
- Checkout lưu tên, số điện thoại và địa chỉ; tổng tiền hệ thống chỉ tính tiền sách.
  Nhân viên dùng thông tin này để đặt COD qua đơn vị giao hàng bên ngoài; phí
  giao hàng do khách thanh toán trực tiếp cho shipper và không thuộc hệ thống.
- Trừ tồn kho sau khi đặt hàng và thông báo mã đơn hàng; trạng thái ban đầu là `Cần xác nhận`.
- Khách hàng có thể hủy đơn trước khi chuyển sang trạng thái `Đang vận chuyển`;
  tồn kho được cộng lại cùng transaction và lịch sử hủy được ghi nhận.
- Khách có thể đánh giá từng sách từ 1 đến 5 sao sau khi đơn hoàn tất.
- Dashboard Admin có thống kê tổng quan; tồn kho được quản lý trong trang sách.
- Quản lý sách Admin có CRUD, chi tiết, phân trang, validation server-side,
  tải ảnh bìa có kiểm tra định dạng và rich-text editor với làm sạch HTML.
- Responsive mobile, Bootstrap 5 và nhóm font sans-serif phổ biến (Arial/Roboto/Helvetica/Tahoma).

## Database và tài khoản demo

Ứng dụng hiện dùng SQL Server + Entity Framework Core. Script
`scripts/init-database.sql` chỉ tạo database rỗng `GroupNine` trên
instance `.\\MSSQLSERVER01`; `DbInitializer` khởi tạo schema bằng
`EnsureCreated` khi cần, reset dữ liệu cũ đúng một lần, rồi seed 47 sách,
7 danh mục và role `Admin`/`Customer`. Bảng `AppDataReset` ghi nhận lần reset
để các lần khởi động sau không xóa dữ liệu mới. Đây không phải quy trình EF Core
migrations.

- Admin 1: `admin1@gmail.com` / `Admin123456`
- Admin 2: `admin2@gmail.com` / `Admin123456`
- Khách hàng mẫu: `khach1@gmail.com` / `Khach123456`
- Ảnh bìa: `wwwroot/images/books/{1..47}.jpg`, ánh xạ theo STT trong
  `DanhSachSach_GroupNine.md`.
- Đăng nhập: `/Account/Login`
- Đăng ký: `/Account/Register`
- Quản trị sách: `/Admin` (chỉ role Admin truy cập được)
- Admin có giao diện riêng, không dùng header/footer cửa hàng; gồm thống kê hệ thống (tồn kho, doanh thu, đơn hàng, sách/danh mục bán chạy từ các đơn hoàn tất), CRUD sách, quản lý kho, danh mục, đơn hàng và ưu đãi.
- Quản lý ưu đãi tại `/Admin/Promotions`: lọc theo tên/mã sách hoặc danh mục, áp dụng giảm theo phần trăm hoặc số tiền trực tiếp, và xóa ưu đãi từng sách.
- Giỏ hàng và đặt hàng yêu cầu đăng nhập; khách chưa đăng nhập sẽ được chuyển tới `/Account/Login`.

Schema Identity và nghiệp vụ được khởi tạo từ `AppDbContext`/`EnsureCreated`,
còn dữ liệu mẫu và reset ban đầu do `DbInitializer` thực hiện. Connection string
mặc định nằm trong `appsettings.json`. Reset xóa đơn hàng, góp ý, đánh giá,
sách, danh mục, tài khoản, role và các liên kết Identity hiện có trong database
được cấu hình trước khi seed lại ba tài khoản demo.

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

Mật khẩu mới phải dài ít nhất 8 ký tự, có chữ in hoa và chữ số; sau 5 lần đăng
nhập sai liên tiếp, tài khoản bị khóa 15 phút. Tài khoản demo chỉ phục vụ chấm
bài/thử nghiệm. Khi triển khai thật, hãy đổi mật khẩu, không commit connection
string có mật khẩu vào GitHub và dùng User Secrets hoặc biến môi trường.
