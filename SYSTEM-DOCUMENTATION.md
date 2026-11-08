# Tài liệu tổng hợp hệ thống GroupNine

## 1. Bối cảnh và mục tiêu

**GroupNine** là website bán sách trực tuyến xây dựng bằng ASP.NET Core MVC
(.NET 10), SQL Server và ASP.NET Core Identity. Hệ thống mô phỏng quy trình
bán sách online tại Việt Nam:

- Khách hàng xem, tìm kiếm, lọc và mua sách.
- Khách hàng quản lý giỏ hàng, đơn hàng, trả hàng và góp ý.
- Nhân viên/quản trị viên quản lý sách, kho, danh mục, khuyến mãi, đơn hàng,
  trả hàng và góp ý.
- Không hỗ trợ nghiệp vụ nhận hàng tại cửa hàng; mọi đơn hàng đều giao tận nơi.

## 2. Công nghệ và cấu trúc chính

| Thành phần | Công nghệ/chức năng |
|---|---|
| Giao diện | Razor View, HTML, CSS, Bootstrap |
| Backend | ASP.NET Core MVC .NET 10 |
| Đăng nhập | ASP.NET Core Identity |
| Phân quyền | Role `Admin`, `Customer` |
| Database | SQL Server `LatTrangBookStore` |
| Khởi tạo schema | `Data/DbInitializer.cs`, `AppDbContext` và `EnsureCreated` |
| Script tạo database | `scripts/init-database.sql` (chỉ tạo database rỗng) |
| Giỏ hàng | Session, khóa `bookstore-cart` |

Các khu vực chính trong mã nguồn:

- `Controllers/`: xử lý request và nghiệp vụ.
- `Models/`: entity, view model và dữ liệu sách mẫu.
- `Data/`: DbContext và khởi tạo database.
- `Views/`: giao diện storefront và Admin.
- `wwwroot/`: CSS, JavaScript và thư viện giao diện.

## 3. Khởi tạo database và chạy hệ thống

### 3.1. Chuẩn bị

Cần cài:

1. Windows 10/11.
2. .NET 10 SDK.
3. SQL Server Express/Developer.
4. SQL Server Management Studio hoặc `sqlcmd`.
5. Git.

### 3.2. Clone và restore

```powershell
git clone https://github.com/trantiendung1206/ltw_nhom9_bansachtructuyen.git
cd ltw_nhom9_bansachtructuyen
dotnet restore
```

### 3.3. Tạo database SQL Server

Mở `scripts/init-database.sql` trong SQL Server Management Studio và chạy.
Hoặc dùng:

```powershell
sqlcmd -S ".\MSSQLSERVER01" -E -i ".\scripts\init-database.sql"
```

Script chỉ tạo database rỗng `LatTrangBookStore`; script không tự tạo bảng.
Khi ứng dụng khởi động, `DbInitializer` tiếp tục:

1. Dùng `EnsureCreated` để tạo schema Identity và schema nghiệp vụ từ
   `AppDbContext`.
2. Tạo/bổ sung các bảng và cột nghiệp vụ còn thiếu nếu database đã tồn tại từ
   phiên bản cũ.
3. Chuẩn hóa trạng thái đơn hàng.
4. Xóa bảng `Wishlists` cũ nếu bảng này còn tồn tại; giỏ hàng hiện chỉ dùng
   Session, không lưu trong database.
5. Seed 7 danh mục cố định và 33 sách mẫu.
6. Tạo role `Admin`, `Customer`.
7. Tạo 4 tài khoản Admin và 4 tài khoản Customer demo.

Sau khi chạy ứng dụng lần đầu, có thể kiểm tra schema bằng:

```sql
USE [LatTrangBookStore];
SELECT [name]
FROM sys.tables
WHERE [name] IN (
    N'Books', N'Categories', N'Orders', N'OrderItems',
    N'OrderStatusHistories', N'ReturnRequests', N'CustomerFeedbacks',
    N'AspNetUsers', N'AspNetRoles'
)
ORDER BY [name];
```

Nếu dùng SQL Server instance khác, sửa `ConnectionStrings:DefaultConnection`
trong `appsettings.json`, ví dụ:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=LatTrangBookStore;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 3.4. Chạy ứng dụng

```powershell
dotnet run
```

Truy cập URL hiện trong terminal, thường là `http://localhost:5080`.

```powershell
dotnet run --urls http://localhost:5080
```

Nếu database đã tồn tại, có thể bỏ qua script SQL và chạy ứng dụng; chương
trình vẫn kiểm tra schema và seed dữ liệu cần thiết.

## 4. Phân quyền và điều hướng

### Khách chưa đăng nhập

- Được xem trang chủ, danh sách sách, chi tiết sách và tìm kiếm.
- Khi truy cập giỏ hàng, thanh toán, đơn hàng hoặc hỗ trợ sẽ được
  chuyển tới trang đăng nhập.

### Khách hàng (`Customer`)

- Đăng nhập bằng email hoặc số điện thoại.
- Đăng ký tài khoản mới bằng họ tên, email, số điện thoại, mật khẩu và xác
  nhận mật khẩu.
- Mua sách, quản lý giỏ hàng và theo dõi đơn.
- Gửi yêu cầu đổi hàng/hoàn tiền và góp ý.

### Quản trị viên (`Admin`)

- Sau khi đăng nhập được chuyển thẳng vào `/Admin`.
- Dùng layout Admin riêng, không hiển thị giao diện cửa hàng.
- Có quyền cập nhật dữ liệu quản trị và trạng thái nghiệp vụ.
- Không có chức năng tự đăng ký tài khoản Admin.

## 5. Chức năng storefront cho khách hàng

### 5.1. Trang chủ

Trang chủ hiển thị:

- Logo và tên thương hiệu GroupNine.
- Ô tìm kiếm.
- Liên kết giỏ hàng, đơn hàng và tài khoản.
- Thanh danh mục sách.
- Sách nổi bật, sách bán chạy, sách mới và sách đang khuyến mãi.
- Trích dẫn cuối trang được chọn ngẫu nhiên từ khoảng 10 câu mỗi lần tải.
- Khu vực “Hỗ trợ và Góp ý”.

Các khu vực đã bỏ theo yêu cầu: combo tiết kiệm, văn phòng phẩm, góc đọc
sách, danh mục nổi bật, ưu đãi trong tuần, đăng ký nhận tin và chính sách
giao hàng.

### 5.2. Danh sách sách và bộ lọc

Đường dẫn: `/Books`

- Ô tìm kiếm lọc theo tên sách hoặc tác giả.
- Có thể tích chọn nhiều danh mục.
- Có thể chọn một cách sắp xếp:
  - Mới nhất.
  - Giá tăng dần.
  - Giá giảm dần.
  - Đánh giá cao.
- Bộ lọc khuyến mãi chỉ hiển thị sách có giá bán thấp hơn giá gốc.
- Nút **Áp dụng bộ lọc** gửi lựa chọn lên server.
- Nút **Xóa bộ lọc** đưa danh sách về trạng thái mặc định.
- Bấm vào thẻ sách để xem chi tiết.

### 5.3. Chi tiết sách

Hiển thị tên, tác giả, danh mục, ảnh, giá gốc, giá bán, mức giảm, đánh giá,
mô tả và tồn kho.

- **Thêm vào giỏ**: thêm sách với số lượng chọn trước.
- **Mua/đến giỏ hàng**: chuyển người dùng tới giỏ để kiểm tra trước khi đặt.

### 5.4. Đăng nhập, đăng ký và đăng xuất

#### Đăng nhập

- Nhập email hoặc số điện thoại.
- Nhập mật khẩu.
- **Ghi nhớ đăng nhập**: giữ phiên đăng nhập theo lựa chọn của người dùng.
- **Đăng nhập**: xác thực thông tin và điều hướng:
  - Admin → `/Admin`.
  - Customer → trang chủ hoặc URL ban đầu.

#### Đăng ký

- Nhập họ tên, email, số điện thoại, mật khẩu và xác nhận mật khẩu.
- Email và số điện thoại không được trùng tài khoản hiện có.
- Tài khoản đăng ký mới luôn nhận role `Customer`.
- Đăng ký thành công sẽ tự đăng nhập và về trang chủ.

#### Đăng xuất

- Hủy phiên Identity hiện tại và quay về trang chủ.
- Header hiển thị tên khách hàng bên dưới khu vực đăng xuất.

### 5.5. Giỏ hàng

Đường dẫn: `/Cart`

- **Thêm vào giỏ**: lưu sách vào Session; số lượng không vượt quá tồn kho.
- **Tăng/giảm số lượng**: cập nhật số lượng và thành tiền.
- **Cập nhật**: lưu số lượng mới.
- **Xóa**: loại một sách khỏi giỏ.
- **Thanh toán**: chuyển tới checkout.
- Badge trên header hiển thị tổng số lượng sách trong giỏ.

Giỏ hàng được lưu theo phiên trình duyệt, không tạo bảng database riêng.

### 5.6. Thanh toán và đặt hàng

Đường dẫn: `/Checkout`

Người dùng nhập:

- Tên người nhận.
- Số điện thoại.
- Địa chỉ giao hàng.

Hệ thống tính:

- Tạm tính theo giá sau khuyến mãi.
- Tổng tiền hàng chỉ tính tiền sách sau khuyến mãi; hệ thống không tính, thu
  hoặc lưu phí vận chuyển.

Sau khi đơn được tạo, nhân viên dùng tên, số điện thoại và địa chỉ trong đơn
để đặt một đơn COD với đơn vị giao hàng bên ngoài như be hoặc Grab. Shipper
ứng trước tiền hàng cho nhân viên; khi giao thành công, khách thanh toán tiền
hàng và phí giao hàng trực tiếp cho shipper. Nhân viên chủ động báo trước
phí giao hàng cho khách qua số điện thoại hoặc email. Phí giao hàng bên ngoài
không thuộc doanh thu hay tổng tiền của đơn trong hệ thống.

Nút **Đặt hàng**:

1. Kiểm tra giỏ hàng và tồn kho.
2. Tạo đơn hàng trạng thái `Cần xác nhận`.
3. Tạo các dòng chi tiết đơn hàng.
4. Trừ tồn kho trong transaction.
5. Xóa giỏ hàng sau khi lưu thành công.
6. Hiển thị mã đơn hàng.

### 5.7. Đơn hàng khách hàng

Đường dẫn: `/Orders`

Khách hàng xem:

- Mã đơn dạng dễ đọc.
- Ngày đặt.
- Danh sách sách và số lượng.
- Tổng tiền.
- Tên, số điện thoại, địa chỉ nhận.
- Trạng thái đơn.
- Lịch sử các yêu cầu trả hàng.

Trạng thái đơn:

1. `Cần xác nhận`
2. `Đã xác nhận đơn hàng`
3. `Đã đóng gói - chờ vận chuyển`
4. `Đang vận chuyển`
5. `Hoàn tất`
6. `Đã hủy`

### 5.8. Trả hàng

Khách mở **Chi tiết đơn hàng** và chỉ thấy nút **Yêu cầu trả hàng** với đơn
`Hoàn tất` trong vòng 7 ngày. Chỉ hỗ trợ hoàn tiền. Khách nhập lý do và gửi
yêu cầu `Chờ xác nhận`; nhân viên xác nhận để chuyển thành
`Chấp nhận yêu cầu trả hàng`.

### 5.9. Góp ý

Đường dẫn: `/Support/Feedback`

- Nhập tiêu đề.
- Nhập nội dung.
- **Gửi góp ý** lưu vào database và chuyển về trang chủ.
- Góp ý xuất hiện trong mục **Góp ý** của Admin.

## 6. Chức năng Admin

Tất cả route `/Admin` yêu cầu role `Admin`.

### 6.1. Thống kê hệ thống

Trang `/Admin` hiển thị:

- Tổng số sách.
- Tổng số lượng tồn kho.
- Tổng danh mục.
- Tổng số đơn hàng.
- Doanh thu từ đơn `Hoàn tất`.
- Sách bán chạy nhất.
- Danh mục bán chạy nhất.
- Các sách có đánh giá cao.

Đây là trang thống kê, không còn mục “sách sắp hết”.

### 6.2. Quản lý sách và kho

Đường dẫn: `/Admin/Books`

- Ô tìm kiếm theo tên sách hoặc mã sách.
- **Thêm sách**: mở form nhập sách mới.
- **Sửa**: cập nhật tên, tác giả, danh mục, giá, giảm giá, tồn kho, ảnh và mô
  tả.
- **Xóa**: xóa sách khỏi database sau xác nhận trên giao diện.
- **Cập nhật tồn kho**: sửa số lượng tồn kho thực tế; sách không có nghiệp vụ
  ẩn/hiện và vẫn xuất hiện trong danh sách khi hết hàng.
- **Lưu/Xác nhận**: gửi thay đổi lên server.

Khuyến mãi hỗ trợ hai dạng:

- Giảm theo phần trăm.
- Giảm trực tiếp một số tiền.

Giá bán ưu tiên giảm tiền trực tiếp nếu giá trị này lớn hơn 0; nếu không sẽ
dùng phần trăm.

### 6.3. Quản lý 7 danh mục cố định

Đường dẫn: `/Admin/Categories`

Danh mục cố định:

1. Văn học & Tiểu thuyết
2. Sách giáo khoa
3. Kĩ năng & Kinh doanh
4. Sách khoa học
5. Sách thiếu nhi
6. Truyện
7. Sách Ngoại Ngữ

Danh mục không có nút thêm, xóa hoặc ẩn/hiện. Người quản trị bấm vào một danh
mục để mở danh sách sách.

### 6.4. Gán sách vào danh mục

Đường dẫn: `/Admin/Categories/{name}`

- Mặc định hiển thị toàn bộ sách theo hàng dọc.
- Ô tìm kiếm lọc nhanh theo tên, mã hoặc tác giả.
- Checkbox đầu mỗi dòng:
  - Tích: thêm sách vào danh mục đang chọn.
  - Bỏ tích: bỏ sách khỏi danh mục đang chọn.
- **Xác nhận thay đổi**: lưu các checkbox vào database.
- Hộp thoại xác nhận giúp tránh thao tác nhầm.

### 6.5. Quản lý đơn hàng

Đường dẫn: `/Admin/Orders`

- Tìm kiếm theo mã đơn, tên người nhận, số điện thoại hoặc tên sách.
- Mã đơn hiển thị dạng `#00000001`.
- Mỗi dòng hiển thị mã đơn, ngày đặt, khách hàng/số điện thoại, tổng tiền và
  trạng thái chỉ đọc.
- **Chi tiết đơn hàng** mở thông tin khách, sản phẩm, địa chỉ và lịch trình.
- Các nút xử lý theo đúng thứ tự: xác nhận đơn, đóng gói, đang vận chuyển,
  hoàn tất; không thể chuyển ngược trạng thái.
- Nút hủy yêu cầu nhập lý do và chỉ hoạt động trước khi đơn hoàn tất.

### 6.6. Quản lý trả hàng

Đường dẫn: `/Admin/Returns`

- Tìm kiếm theo mã đơn, tên khách, số điện thoại hoặc email.
- Mã đơn hiển thị dạng dễ đọc.
- Xem lý do, thời gian và trạng thái yêu cầu hoàn tiền.
- Xem trực tiếp email và số điện thoại khách để liên hệ.
- **Chấp nhận**: chuyển `Chờ xác nhận` thành `Chấp nhận yêu cầu trả hàng`,
  hoàn tiền bằng tổng đơn, trừ khoản hoàn khỏi doanh thu và cộng lại số lượng
  từng sách vào tồn kho trong cùng một transaction.
- **Không chấp nhận**: bắt buộc nhập lý do, chuyển thành `Từ chối yêu cầu trả
  hàng` và hiển thị lý do cho khách hàng.
- Khi trạng thái không còn là `Chờ xác nhận`, hệ thống lưu thời gian xử lý;
  thao tác chấp nhận chỉ thực hiện một lần để không cộng tồn kho trùng.

### 6.7. Quản lý góp ý

Đường dẫn: `/Admin/Feedback`

- Xem tiêu đề, nội dung, email khách và thời gian gửi.
- Cập nhật trạng thái góp ý.
- **Xác nhận lưu**: lưu trạng thái xử lý vào database.

### 6.8. Quản lý ưu đãi

Đường dẫn: `/Admin/Promotions`

- Tìm sách theo tên hoặc mã.
- Lọc theo danh mục.
- Chọn mức giảm phần trăm hoặc số tiền.
- **Áp dụng ưu đãi**: áp dụng cho toàn bộ kết quả lọc.
- **Xóa ưu đãi**: đưa mức giảm của một sách về 0.

## 7. Quy trình nghiệp vụ tổng quát

### Quy trình mua hàng

```text
Xem sách
  → Tìm kiếm/lọc
  → Xem chi tiết
  → Đăng nhập
  → Thêm vào giỏ
  → Kiểm tra số lượng
  → Nhập tên, số điện thoại và địa chỉ nhận
  → Đặt hàng
  → Trừ kho và tạo đơn
  → Nhân viên đặt COD qua đơn vị giao hàng bên ngoài
  → Admin cập nhật trạng thái
  → Khách theo dõi đơn
```

### Quy trình trả hàng

```text
Đơn hoàn tất trong 7 ngày
  → Chi tiết đơn hàng
  → Yêu cầu trả hàng
  → Nhập lý do hoàn tiền
  → Chờ xác nhận
  → Admin xem thông tin liên hệ
  → Chấp nhận yêu cầu trả hàng
  → Khách xem kết quả trong lịch sử đơn
```

### Quy trình góp ý

```text
Khách nhập góp ý
  → Lưu CustomerFeedbacks
  → Admin mở mục Góp ý
  → Cập nhật trạng thái xử lý
```

## 8. Database và dữ liệu

### 8.1. Bảng và quan hệ

Database gồm các bảng Identity mặc định của ASP.NET Core Identity và các bảng
nghiệp vụ sau:

| Bảng | Cột chính | Quan hệ/chức năng |
|---|---|---|
| `Books` | `Id`, `Title`, `Author`, `Category`, `Price`, `DiscountPercent`, `DiscountAmount`, `Stock`, `Rating`, `CoverImage`, `Description` | Danh mục sách và tồn kho; `Price`, `DiscountAmount` dùng `decimal(18,2)` |
| `Categories` | `Id`, `Name`, `IsActive` | 7 danh mục hệ thống; không có CRUD danh mục trên Admin |
| `Orders` | `Id`, `UserId`, `ReceiverName`, `Phone`, `Address`, `Status`, `CancellationReason`, `Total`, `RefundedAmount`, `CreatedAt` | Đơn hàng và thông tin để nhân viên đặt giao hàng bên ngoài; `UserId` có thể rỗng; `Total` chỉ là tiền hàng; `RefundedAmount` dùng để trừ doanh thu khi hoàn hàng |
| `OrderItems` | `Id`, `OrderId`, `BookId`, `BookTitle`, `Quantity`, `UnitPrice` | Các dòng trong đơn; `OrderId` xóa dây chuyền theo `Orders` |
| `OrderStatusHistories` | `Id`, `OrderId`, `OccurredAt`, `Action` | Lịch sử trạng thái; `OrderId` xóa dây chuyền theo `Orders` |
| `ReturnRequests` | `Id`, `OrderId`, `UserId`, `CustomerEmail`, `CustomerPhone`, `RequestType`, `Reason`, `Status`, `RejectionReason`, `CreatedAt`, `ResolvedAt` | Yêu cầu hoàn tiền hoặc từ chối; `OrderId` xóa dây chuyền theo `Orders` |
| `CustomerFeedbacks` | `Id`, `UserId`, `CustomerEmail`, `Subject`, `Message`, `Status`, `CreatedAt` | Góp ý của khách hàng |

Identity tạo các bảng `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`,
`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` và
`AspNetRoleClaims`. `AppUser` bổ sung `FullName` và `Address` cho
`AspNetUsers`.

Không có bảng `Wishlists` trong thiết kế hiện tại. Nếu database cũ còn bảng
này, `DbInitializer` sẽ xóa bảng khi ứng dụng khởi động.

### 8.2. Dữ liệu mẫu

Các dữ liệu mẫu được tạo tự động:

- 7 danh mục cố định.
- 33 sách mẫu từ `StoreData.Books`; nếu database đã có sách, chỉ bổ sung sách
  chưa tồn tại theo tên.
- Role `Admin`, `Customer`.
- 4 tài khoản Admin và 4 tài khoản khách hàng mẫu.

### Tài khoản demo

| Loại | Tài khoản | Mật khẩu |
|---|---|---|
| Admin | `admin1@gmail.com` | `12345678` |
| Admin | `admin2@gmail.com` | `12345678` |
| Admin | `admin3@gmail.com` | `12345678` |
| Admin | `admin@latrang.vn` | `Admin123!` |
| Customer | `khachhangmau1@gmail.com` | `12345678` |
| Customer | `khachhangmau2@gmail.com` | `12345678` |
| Customer | `khachhangmau3@gmail.com` | `12345678` |
| Customer | `customer@latrang.vn` | `Customer123!` |

## 9. Lưu ý vận hành

- Không commit mật khẩu thật hoặc connection string chứa mật khẩu lên GitHub.
- Tài khoản demo chỉ dùng cho chấm bài và kiểm thử.
- Nếu cổng 5080 bận, chạy `dotnet run --urls http://localhost:5081`.
- Nếu SQL Server không kết nối được, kiểm tra service, tên instance và quyền
  Windows Authentication.
- Nếu file `sc.exe` bị khóa khi build, dừng tiến trình website cũ rồi build lại.
- Database hiện dùng `EnsureCreated`, không dùng migration EF Core.
