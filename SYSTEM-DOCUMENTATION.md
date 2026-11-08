# Tài liệu hệ thống GroupNine

Tài liệu này mô tả hành vi đang được triển khai trong mã nguồn hiện tại, không
phải đặc tả cho các chức năng dự kiến. Khi tài liệu và ứng dụng khác nhau, các
controller, model, view, `Program.cs` và `Data/DbInitializer.cs` là nguồn đối
chiếu.

## 1. Tổng quan

GroupNine là website bán sách trực tuyến xây dựng bằng ASP.NET Core MVC trên
.NET 10. Ứng dụng dùng Razor Views, Entity Framework Core, SQL Server và ASP.NET
Core Identity. Giao diện sử dụng Bootstrap và các tài nguyên tĩnh trong
`wwwroot/`.

| Thành phần | Triển khai hiện tại |
|---|---|
| Backend và định tuyến | ASP.NET Core MVC, route mặc định `{controller}/{action}/{id?}` |
| Giao diện | Razor Views, HTML, CSS, Bootstrap |
| Dữ liệu | Entity Framework Core với SQL Server |
| Tài khoản và đăng nhập | ASP.NET Core Identity, cookie authentication |
| Vai trò | `Admin`, `Customer` |
| Giỏ hàng | ASP.NET Core Session, khóa `bookstore-cart`, lưu JSON trong bộ nhớ ứng dụng |
| Cấu hình database mặc định | `GroupNine` trên instance `.\MSSQLSERVER01` |
| Tên logic file SQL Server hiện tại | `GroupNine`, `GroupNine_log` |
| Khởi tạo database | `Data/DbInitializer.cs`, `AppDbContext`, `EnsureCreated` và một số lệnh SQL bổ sung |

Các thư mục chính:

- `Controllers/`: xử lý request, phân quyền và nghiệp vụ.
- `Models/`: entity, view model và dữ liệu seed trong `StoreData`.
- `Data/`: `AppDbContext` và khởi tạo/cập nhật dữ liệu khi ứng dụng chạy.
- `Views/`: giao diện storefront, tài khoản và Admin.
- `wwwroot/`: CSS, JavaScript và thư viện giao diện.
- `scripts/init-database.sql`: tạo database rỗng trên SQL Server.

## 2. Cài đặt và khởi chạy

### Yêu cầu

- Windows 10/11.
- .NET 10 SDK.
- SQL Server Express/Developer đang chạy; SSMS hoặc `sqlcmd` để tạo database.
- Git.

### Tạo database và chạy

```powershell
git clone https://github.com/trantiendung1206/ltw_nhom9_bansachtructuyen.git
cd ltw_nhom9_bansachtructuyen
dotnet restore
sqlcmd -S ".\MSSQLSERVER01" -E -i ".\scripts\init-database.sql"
dotnet run
```

Có thể chạy script `scripts/init-database.sql` trong SSMS thay cho `sqlcmd`.
Script chỉ tạo database rỗng `GroupNine`, không khai báo các bảng.
Trên SQL Server instance mặc định hiện tại, database đã được đổi tên từ
`LatTrangBookStore` thành `GroupNine`; tên logic file là `GroupNine` và
`GroupNine_log`. Hai tài khoản demo cùng email góp ý demo cũng đã được chuyển
sang miền `groupnine.vn`. Tên vật lý file MDF/LDF trên instance này vẫn là
`LatTrangBookStore.mdf` và `LatTrangBookStore_log.ldf`; đây là tên file lưu
trữ trên máy chủ, không phải tên database hay tên logic file ứng dụng kết nối
tới. Đổi tên file vật lý không cần thiết để ứng dụng sử dụng database `GroupNine`.

Ứng dụng khởi tạo dữ liệu khi khởi động. Profile HTTP mặc định dùng
`http://localhost:5080`; profile HTTPS dùng `https://localhost:7188` và
`http://localhost:5080`.

Nếu SQL Server dùng instance khác, sửa
`ConnectionStrings:DefaultConnection` trong `appsettings.json` hoặc cấu hình
giá trị tương ứng bằng cơ chế cấu hình của ASP.NET Core. Mặc định connection
string dùng Windows Integrated Security (`Trusted_Connection=True`).

Nếu cài đặt khác vẫn có database cũ `LatTrangBookStore` chứa dữ liệu, không
chạy script tạo database trước khi quyết định cách chuyển đổi: script tạo
`GroupNine` rỗng, không sao chép dữ liệu. Sao lưu trước, dừng ứng dụng, xác
nhận `GroupNine` chưa tồn tại, sau đó kết nối SQL Server vào `master` và đổi
tên database cũ để giữ nguyên dữ liệu:

```sql
ALTER DATABASE [LatTrangBookStore] MODIFY NAME = [GroupNine];
```

Script khởi tạo không tự đổi tên hoặc sao chép database cũ. Nếu cả
`LatTrangBookStore` và `GroupNine` cùng tồn tại, cần xác định database chứa dữ
liệu cần dùng; không ghi đè hoặc xóa database nào khi chưa sao lưu.

### Cách khởi tạo schema và seed

Khi khởi động, `DbInitializer`:

1. Gọi `EnsureCreated` để tạo schema theo `AppDbContext` nếu database chưa có.
2. Chạy các câu lệnh SQL có điều kiện để bổ sung một số bảng/cột còn thiếu ở
   schema cũ, chuẩn hóa một số trạng thái đơn hàng cũ và tạo lịch sử ban đầu
   còn thiếu.
3. Nếu chưa có sách, thêm bộ sách mẫu; nếu đã có sách, bổ sung những sách mẫu
   còn thiếu theo tên.
4. Đồng bộ danh mục với danh sách cố định bên dưới, tạo hai role và tạo/cập
   nhật hồ sơ các tài khoản demo.

Đây **không phải** quy trình EF Core migrations và không có bước
`dotnet ef database update`. Không nên xem các lệnh tương thích schema thủ công
là công cụ migration tổng quát cho mọi schema tùy biến.

## 3. Tài khoản và quyền truy cập

- Khách chưa đăng nhập được xem trang chủ, danh sách và chi tiết sách. Các trang
  giỏ hàng, thanh toán, đơn hàng và hỗ trợ yêu cầu đăng nhập.
- Người đăng ký qua website luôn nhận role `Customer`. Đăng ký yêu cầu họ tên,
  email, số điện thoại và mật khẩu; email/số điện thoại được kiểm tra trùng.
- Có thể đăng nhập bằng email hoặc số điện thoại. Mật khẩu tối thiểu 6 ký tự;
  cấu hình hiện tại không bắt buộc chữ hoa, chữ thường, chữ số hay ký tự đặc
  biệt.
- Route Admin yêu cầu role `Admin`. Không có màn hình tự đăng ký Admin.
- Khi đăng nhập, Admin mặc định được chuyển đến `/Admin`, còn Customer mặc định
  về trang chủ; URL chuyển hướng nội bộ ban đầu, nếu có, cũng được hỗ trợ.
- Đăng xuất là request POST và kết thúc phiên Identity.

Tài khoản được tạo trên database mới:

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@groupnine.vn` | `Admin123!` |
| Admin | `admin1@gmail.com`, `admin2@gmail.com`, `admin3@gmail.com` | `12345678` |
| Customer | `customer@groupnine.vn` | `Customer123!` |
| Customer | `khachhangmau1@gmail.com`, `khachhangmau2@gmail.com`, `khachhangmau3@gmail.com` | `12345678` |

Các thông tin trên chỉ dành cho môi trường demo. Khi chạy, initializer đổi email
và tên đăng nhập của hai tài khoản demo cũ `admin@latrang.vn` và
`customer@latrang.vn` sang địa chỉ `@groupnine.vn`, giữ nguyên ID, vai trò và
mật khẩu; email liên hệ của góp ý demo cũng được cập nhật. Nếu địa chỉ mới đã
thuộc tài khoản khác, initializer báo lỗi thay vì gộp tài khoản. Hãy đổi thông
tin demo và không dùng chúng khi triển khai thực tế.

## 4. Điều hướng và phân quyền theo chức năng

| URL / chức năng | Quyền | Hành vi |
|---|---|---|
| `/`, `/Home/Index` | Công khai | Trang chủ |
| `/Books`, `/Books/Details/{id}` | Công khai | Tìm, lọc, sắp xếp và xem sách |
| `/Account/Login`, `/Account/Register` | Công khai | Đăng nhập và đăng ký |
| `/Account/Logout` | POST | Đăng xuất (liên kết chỉ hiển thị khi đã đăng nhập) |
| `/Cart` và các thao tác giỏ hàng | Đã đăng nhập | Xem, thêm, cập nhật, xóa sách trong giỏ |
| `/Checkout` | Đã đăng nhập | Nhập thông tin nhận hàng và đặt đơn |
| `/Orders`, `/Orders/Details/{id}` | Đã đăng nhập | Xem đơn của chính tài khoản |
| `/Orders/Rate` | Đã đăng nhập, POST | Đánh giá sách trong đơn đã hoàn tất |
| `/Support/Return`, `/Support/Feedback` | Đã đăng nhập | Gửi yêu cầu hoàn tiền hoặc góp ý |
| `/Admin` và các URL `/Admin/...` | Role `Admin` | Quản trị sách, đơn hàng, danh mục, ưu đãi, trả hàng và góp ý |

Các thao tác thay đổi dữ liệu từ form sử dụng POST và kiểm tra anti-forgery
token. Giao diện Admin có layout riêng.

## 5. Danh mục và dữ liệu sách

Bảy danh mục được seed và dùng cố định trong storefront/Admin:

1. Văn học & Tiểu thuyết
2. Sách giáo khoa
3. Kĩ năng & Kinh doanh
4. Sách khoa học
5. Sách thiếu nhi
6. Truyện
7. Sách Ngoại Ngữ

Sách có tên, tác giả, một chuỗi danh mục, giá gốc, phần trăm giảm, số tiền giảm,
tồn kho, điểm đánh giá, ảnh bìa và mô tả. Giá bán được tính từ giá gốc; nếu
`DiscountAmount > 0` thì dùng số tiền giảm, nếu không thì dùng phần trăm giảm.
Giá bán không thấp hơn 0. Không có entity khuyến mãi riêng hoặc lịch hiệu lực
khuyến mãi.

### Storefront

- **Trang chủ:** hiển thị nhóm sách đề xuất, nhóm sách được đánh giá cao, sách
  có giảm giá và một câu trích dẫn được chọn ngẫu nhiên. Nhóm sách được đánh
  giá cao sắp theo điểm đánh giá.
- **Danh sách `/Books`:** tìm theo tên sách hoặc tác giả; có thể lọc nhiều danh
  mục, lọc sách có giá bán thấp hơn giá gốc và sắp xếp mới nhất, giá tăng/giảm
  hoặc đánh giá. “Mới nhất” sắp theo ID giảm dần. Không có phân trang.
- **Chi tiết sách:** hiển thị thông tin sách, giá, đánh giá và tồn kho. Khách
  đăng nhập mới có thể thêm vào giỏ; khách khác được yêu cầu đăng nhập trước khi
  đặt mua. Tìm kiếm hiện không tra cứu ISBN.

## 6. Giỏ hàng, thanh toán và đơn hàng

### Giỏ hàng

Giỏ hàng chỉ dành cho người đã đăng nhập. Dữ liệu được tuần tự hóa vào Session
với thời hạn nhàn rỗi 2 giờ; không có bảng giỏ hàng trong database. Số lượng
thêm/cập nhật được giới hạn theo tồn kho đọc tại thời điểm thao tác. Session
được lưu trong bộ nhớ ứng dụng nên không phải giỏ hàng bền vững qua lần khởi
động lại ứng dụng.

### Đặt hàng

Checkout thu thập tên người nhận, số điện thoại và địa chỉ. Giao diện ghi nhận
phương thức COD; hệ thống:

1. Kiểm tra giỏ và đọc lại tồn kho trước khi tạo đơn.
2. Tính tổng từ giá bán của các mặt hàng trong giỏ; tổng chỉ là tiền sách.
3. Tạo đơn ở trạng thái `Cần xác nhận`, lưu các dòng hàng và giảm tồn kho trong
   transaction.
4. Ghi lịch sử `Đã đặt hàng`, commit transaction, xóa Session giỏ hàng và hiển
   thị mã đơn dạng 8 chữ số.

Không có tích hợp cổng thanh toán, hãng vận chuyển, API đặt giao hàng, phí vận
chuyển hay phí vận chuyển trong tổng đơn. Việc nhân viên liên hệ hãng giao hàng
và sắp xếp COD là quy trình thủ công bên ngoài ứng dụng; khách thanh toán tiền
hàng và phí giao hàng cho đơn vị giao hàng theo thỏa thuận bên ngoài.

### Khách hàng theo dõi đơn

`/Orders` chỉ trả về đơn thuộc tài khoản đang đăng nhập. Khách có thể xem mã
đơn, mặt hàng, số lượng, tổng tiền sách, thông tin nhận hàng, trạng thái, lý do
hủy và các yêu cầu hoàn tiền. Trang chi tiết không cho khách sửa hoặc hủy đơn.

Các trạng thái đơn được dùng trong luồng chính:

1. `Cần xác nhận`
2. `Đã xác nhận đơn hàng`
3. `Đã đóng gói - chờ vận chuyển`
4. `Đang vận chuyển`
5. `Hoàn tất`
6. `Đã hủy`
7. `Đã trả hàng`

## 7. Hoàn tiền và đánh giá

### Yêu cầu hoàn tiền

- Chỉ đơn `Hoàn tất` trong vòng 7 ngày mới đủ điều kiện gửi yêu cầu. Ngày bắt
  đầu tính là `CompletedAt`; với đơn cũ chưa có trường này, hệ thống dùng
  `CreatedAt`.
- Hệ thống chỉ hỗ trợ hoàn tiền toàn bộ đơn, không có hoàn một phần. Khách nhập
  lý do; yêu cầu được lưu với trạng thái `Chờ xác nhận`.
- Mỗi đơn chỉ có một yêu cầu đang chờ hoặc đã chấp nhận. Sau khi bị từ chối,
  khách có thể gửi yêu cầu mới nếu đơn vẫn trong thời hạn.
- Admin có thể chấp nhận hoặc từ chối và phải nhập lý do khi từ chối. Khi chấp
  nhận, ứng dụng đặt `RefundedAmount` bằng `Total`, chuyển đơn sang `Đã trả
  hàng` và cộng lại số lượng sách trong transaction.
- Mục `/Admin/Returns` lọc theo mã đơn; giao diện hiển thị thông tin khách,
  sản phẩm, lý do và trạng thái yêu cầu.

### Đánh giá sách

Sau khi đơn `Hoàn tất`, khách có thể đánh giá từ 1 đến 5 sao cho từng sách thuộc
đơn của chính mình. Khách có thể cập nhật đánh giá đã gửi cho cùng sách/đơn.
Điểm `Book.Rating` được tính lại từ các đánh giá đã lưu cho sách.

## 8. Chức năng Admin

Toàn bộ controller Admin được bảo vệ bởi role `Admin`.

### Thống kê `/Admin`

- Tổng số đầu sách, tổng tồn kho, số danh mục đang hoạt động và tổng đơn.
- Doanh thu tính từ đơn `Hoàn tất` theo `Total - RefundedAmount`.
- Sách và danh mục bán chạy được tổng hợp từ chi tiết của các đơn `Hoàn tất`,
  cùng phạm vi trạng thái với cách tính doanh thu.
- Danh sách sách được quan tâm hiển thị các sách có điểm `Rating` cao.

### Quản lý sách và ưu đãi

- `/Admin/Books`: tìm theo tên hoặc ID; thêm, sửa và xóa sách. Trường chỉnh sửa
  gồm tên, tác giả, danh mục, giá, giảm giá, tồn kho, ảnh bìa và mô tả.
- `/Admin/Promotions`: lọc theo tên/ID sách và danh mục, áp dụng giảm theo phần
  trăm hoặc số tiền cho các sách phù hợp; có thể xóa ưu đãi từng sách.
- Nếu số tiền giảm lớn hơn 0, phần trăm giảm được đặt về 0 và giá bán dùng số
  tiền giảm. Nếu không, giá bán dùng phần trăm.
- Không có nghiệp vụ ẩn/hiện sách; sách hết hàng vẫn có thể xuất hiện trong
  danh sách.

### Quản lý danh mục

- `/Admin/Categories` chỉ hiển thị bảy danh mục cố định; giao diện không có thao
  tác thêm hoặc xóa danh mục.
- `/Admin/Categories/{name}` hiển thị các sách và checkbox để gán sách vào danh
  mục. Mỗi sách chỉ lưu một tên danh mục; khi lưu, bỏ gán các sách đã rời danh
  mục và chuyển các sách được chọn sang danh mục đang thao tác.

### Quản lý đơn hàng

- `/Admin/Orders` lọc theo trạng thái và tìm theo mã đơn, tên người nhận, số
  điện thoại hoặc tên sách.
- Mã đơn hiển thị theo dạng `#00000001`; trang chi tiết hiển thị thông tin nhận
  hàng, các dòng hàng và lịch sử xử lý.
- Admin chuyển trạng thái theo thứ tự: xác nhận, đóng gói, đang vận chuyển,
  hoàn tất. Thời điểm chuyển sang hoàn tất được lưu trong `CompletedAt`.
- Giao diện cho phép hủy đơn trước trạng thái hoàn tất/đã hủy/đã trả hàng, yêu
  cầu lý do và cộng lại tồn kho. Cập nhật trạng thái và tồn kho được thực hiện
  trong transaction.

### Góp ý

- `/Support/Feedback` yêu cầu đăng nhập và lưu chủ đề, nội dung, email khách
  gửi, trạng thái và thời điểm gửi.
- `/Admin/Feedback` hiển thị các góp ý. Admin có thể đặt trạng thái `Chưa xử
  lý`, `Đang xử lý` hoặc `Đã phản hồi`.

## 9. Dữ liệu và bảng chính

`AppDbContext` khai báo các nhóm dữ liệu:

- Identity: tài khoản, role và các bảng liên quan; `AppUser` bổ sung `FullName`
  và `Address`.
- Cửa hàng: `Books`, `Categories`.
- Đơn hàng: `Orders`, `OrderItems`, `OrderStatusHistories`.
- Hỗ trợ: `ReturnRequests`, `CustomerFeedbacks`.
- Đánh giá: `BookReviews`.

`OrderItem` lưu tên sách, số lượng và đơn giá tại thời điểm đặt hàng. Giỏ hàng
không nằm trong các bảng trên mà ở Session. Không có bảng/thành phần lưu phương
thức giao hàng, phí giao hàng hay giao dịch thanh toán trực tuyến.

## 10. Cấu hình và lưu ý vận hành

- Connection string mặc định nằm trong `appsettings.json`; có thể ghi đè bằng
  cấu hình ASP.NET Core khi chạy/deploy. Không đưa connection string chứa mật
  khẩu thật vào Git.
- Nếu không kết nối được database, kiểm tra SQL Server service, instance,
  database và quyền của tài khoản Windows đang chạy ứng dụng.
- Profile HTTP mặc định sử dụng cổng 5080; có thể đổi URL khi chạy, ví dụ:
  `dotnet run --urls http://localhost:5081`.
- Tài khoản/mật khẩu nêu trong tài liệu là dữ liệu demo. Đổi hoặc loại bỏ chúng
  trước khi triển khai thực tế; không dùng cấu hình demo như cấu hình production.
