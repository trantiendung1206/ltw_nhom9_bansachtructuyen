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
   schema cũ (bao gồm ISBN và số lượt đánh giá), chuẩn hóa một số trạng thái
   đơn hàng cũ và tạo lịch sử ban đầu còn thiếu.
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
- Có thể đăng nhập bằng email hoặc số điện thoại. Mật khẩu mới phải dài ít nhất
  8 ký tự, có ít nhất một chữ in hoa và một chữ số. Không bắt buộc chữ thường
  hoặc ký tự đặc biệt. Identity khóa đăng nhập 15 phút sau 5 lần nhập sai liên
  tiếp.
- Route Admin yêu cầu role `Admin`. Không có màn hình tự đăng ký Admin.
- Quyền được phân biệt ở tầng ứng dụng: khách chỉ thao tác dữ liệu thuộc tài
  khoản của mình; Admin có quyền quản trị toàn hệ thống. Ứng dụng dùng một
  connection string SQL Server dùng chung, không cấu hình login SQL riêng cho
  từng người dùng cuối.
- Khi đăng nhập, Admin mặc định được chuyển đến `/Admin`, còn Customer mặc định
  về trang chủ; URL chuyển hướng nội bộ ban đầu, nếu có, cũng được hỗ trợ.
- Đăng xuất là request POST và kết thúc phiên Identity.

Tài khoản được tạo trên database mới:

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@groupnine.vn` | `Admin123456` |
| Admin | `admin1@gmail.com` | `Admin123456` |
| Admin | `admin2@gmail.com` | `Admin123456` |
| Admin | `admin3@gmail.com` | `Admin123456` |
| Customer | `customer@groupnine.vn` | `Customer123!` |
| Customer | `khachhangmau1@gmail.com` | `CustomerOne123` |
| Customer | `khachhangmau2@gmail.com` | `CustomerTwo123` |
| Customer | `khachhangmau3@gmail.com` | `CustomerThree123` |

Các thông tin trên chỉ dành cho môi trường demo. Khi chạy, initializer đổi email
và tên đăng nhập của hai tài khoản demo cũ `admin@latrang.vn` và
`customer@latrang.vn` sang địa chỉ `@groupnine.vn`, giữ nguyên ID và vai trò;
email liên hệ của góp ý demo cũng được cập nhật. Với các tài khoản demo phụ
còn dùng mật khẩu seed trước đây, initializer đổi các tài khoản Admin sang
`Admin123456`; các mật khẩu khách hàng cũ `12345678` được nâng cấp theo chính
sách hiện tại. Mật khẩu đã được người dùng thay đổi sẽ không bị ghi đè. Nếu địa
chỉ mới đã thuộc tài khoản khác, initializer báo lỗi thay vì gộp tài khoản. Hãy
đổi thông tin demo và không dùng chúng khi triển khai thực tế.

## 4. Điều hướng và phân quyền theo chức năng

| URL / chức năng | Quyền | Hành vi |
|---|---|---|
| `/`, `/Home/Index` | Công khai | Trang chủ |
| `/Books`, `/Books/Details/{id}` | Công khai | Tìm, lọc, sắp xếp, phân trang và xem sách |
| `/api/books`, `/api/books/{id}` | GET công khai; POST/PUT/DELETE Admin | API sách REST; thao tác ghi yêu cầu role Admin và anti-forgery token |
| `/Account/Login`, `/Account/Register` | Công khai | Đăng nhập và đăng ký |
| `/Account/Logout` | POST | Đăng xuất (liên kết chỉ hiển thị khi đã đăng nhập) |
| `/Cart` và các thao tác giỏ hàng | Đã đăng nhập | Xem, thêm, cập nhật, xóa sách trong giỏ |
| `/Checkout` | Đã đăng nhập | Nhập thông tin nhận hàng và đặt đơn |
| `/Orders`, `/Orders/Details/{id}` | Đã đăng nhập | Xem đơn của chính tài khoản; hủy đơn trước khi vận chuyển |
| `/Orders/Cancel/{id}` | Đã đăng nhập, POST | Hủy đơn của chính tài khoản trước khi chuyển sang vận chuyển |
| `/Orders/Rate` | Đã đăng nhập, POST | Đánh giá sách trong đơn đã hoàn tất |
| `/Support/Return`, `/Support/Feedback` | Đã đăng nhập | Gửi yêu cầu hoàn tiền hoặc góp ý |
| `/Admin` và các URL `/Admin/...` | Role `Admin` | Quản trị sách, đơn hàng, danh mục, ưu đãi, trả hàng và góp ý |

Các thao tác thay đổi dữ liệu từ form sử dụng POST và kiểm tra anti-forgery
token. API ghi dùng POST/PUT/DELETE, lấy token qua
`GET /api/books/antiforgery` (Admin) và gửi token trong header
`X-CSRF-TOKEN`. Giao diện Admin có layout riêng.

## 5. Danh mục và dữ liệu sách

Bảy danh mục được seed và dùng cố định trong storefront/Admin:

1. Văn học & Tiểu thuyết
2. Sách giáo khoa
3. Kĩ năng & Kinh doanh
4. Sách khoa học
5. Sách thiếu nhi
6. Truyện
7. Sách Ngoại Ngữ

Sách có tên, tác giả, một chuỗi danh mục, ISBN (có thể để trống nếu chưa xác
minh), giá gốc, phần trăm giảm, số tiền giảm, tồn kho, điểm đánh giá, số lượt
đánh giá, ảnh bìa và mô tả. Điểm trung bình và số lượt đánh giá được đồng bộ từ
bảng `BookReviews`; sách chưa có đánh giá có điểm 0 và 0 lượt. Giá bán được tính từ giá gốc; nếu
`DiscountAmount > 0` thì dùng số tiền giảm, nếu không thì dùng phần trăm giảm.
Giá bán không thấp hơn 0. Không có entity khuyến mãi riêng hoặc lịch hiệu lực
khuyến mãi.

### Storefront

- **Trang chủ:** hiển thị nhóm sách đề xuất, nhóm sách được đánh giá cao, sách
  có giảm giá và một câu trích dẫn được chọn ngẫu nhiên. Nhóm sách được đánh
  giá cao chỉ gồm sách đã có đánh giá và sắp theo điểm trung bình.
- **Danh sách `/Books`:** tìm theo tên sách hoặc tác giả; có thể lọc nhiều danh
  mục, lọc sách có giảm giá và sắp xếp mới nhất, giá tăng/giảm hoặc đánh giá.
  “Mới nhất” sắp theo ID giảm dần. Truy vấn được lọc, sắp xếp và phân trang ở
  SQL Server, mỗi trang có 12 sách. Khi JavaScript hoạt động, giao diện gọi
  `GET /api/books` bằng AJAX để cập nhật kết quả và phân trang mà không tải lại
  trang; khi không có JavaScript, form và liên kết phân trang vẫn hoạt động
  bằng MVC.
- **Chi tiết sách:** hiển thị thông tin sách, ISBN, giá, số lượt/điểm đánh giá,
  tồn kho và phần mô tả bên dưới. Nếu chưa nhập ISBN, giao diện báo “Chưa cập
  nhật”; không tự tạo mã ISBN không được xác minh. Khách
  đăng nhập mới có thể thêm vào giỏ; khách khác được yêu cầu đăng nhập trước khi
  đặt mua. Mô tả HTML được làm sạch trước khi hiển thị. Tìm kiếm hiện không tra
  cứu ISBN.

### REST API sách

- `GET /api/books`: nhận các tham số tùy chọn `q`, `category` (có thể lặp),
  `promotion`, `sort`, `page` và `pageSize`. API giới hạn `pageSize` tối đa 48,
  trả danh sách `items` cùng `page`, `pageSize`, `totalCount` và `totalPages`.
- `GET /api/books/{id}`: trả thông tin sách hoặc HTTP 404 nếu không tìm thấy.
- `POST /api/books`: Admin tạo sách; body JSON được kiểm tra bằng Data
  Annotations, danh mục và đường dẫn ảnh được xác thực, mô tả được làm sạch.
- `PUT /api/books/{id}`: Admin cập nhật sách với cùng kiểm tra.
- `DELETE /api/books/{id}`: Admin xóa sách; trả HTTP 409 nếu sách đã có trong
  đơn hàng hoặc đánh giá để giữ lịch sử.
- Hai phương thức GET công khai; các phương thức ghi yêu cầu Admin, anti-forgery
  token và trả HTTP 401/403 đúng kiểu API khi chưa đăng nhập/không đủ quyền.
  Mô tả trong JSON cũng được làm sạch HTML phía máy chủ.

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
hủy và các yêu cầu hoàn tiền. Tại danh sách đơn, khách có thể hủy đơn ở trạng
thái `Cần xác nhận`, `Đã xác nhận đơn hàng` hoặc `Đã đóng gói - chờ vận chuyển`.
Không thể hủy từ khi đơn chuyển sang `Đang vận chuyển` trở đi. Thao tác hủy là
POST có anti-forgery token, chỉ áp dụng lên đơn của tài khoản đang đăng nhập,
đổi trạng thái sang `Đã hủy`, lưu lý do `Khách hàng yêu cầu hủy đơn hàng.`,
ghi lịch sử và cộng lại tồn kho trong cùng transaction. Server kiểm tra lại
trạng thái trong câu lệnh cập nhật để tránh hủy đồng thời hai lần hoặc hoàn kho
nhiều lần.

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
`Book.Rating` và `Book.ReviewCount` được cập nhật theo các đánh giá đã lưu; nếu
không có đánh giá thì hiển thị 0 sao và 0 lượt.

## 8. Chức năng Admin

Toàn bộ controller Admin được bảo vệ bởi role `Admin`.

### Thống kê `/Admin`

- Tổng số đầu sách, tổng tồn kho, số danh mục đang hoạt động và tổng đơn.
- Doanh thu tính từ đơn `Hoàn tất` theo `Total - RefundedAmount`.
- Sách và danh mục bán chạy được tổng hợp từ chi tiết của các đơn `Hoàn tất`,
  cùng phạm vi trạng thái với cách tính doanh thu.
- Danh sách sách được quan tâm hiển thị các sách có điểm `Rating` cao.

### Quản lý sách và ưu đãi

- `/Admin/Books`: tìm theo tên hoặc ID, phân trang 15 sách/trang; xem chi tiết,
  thêm, sửa và xóa sách. Form kiểm tra dữ liệu bằng Data Annotations và xác nhận
  danh mục còn hoạt động ở máy chủ. Sách đã được dùng trong đơn hàng hoặc đánh
  giá không thể xóa để giữ lịch sử.
- Form sách hỗ trợ tải ảnh PNG/JPEG/WebP tối đa 5 MB; phần mở rộng và chữ ký
  tệp được kiểm tra, tên tệp được tạo ngẫu nhiên và ảnh lưu dưới
  `wwwroot/uploads/books/`. Vẫn có thể giữ hoặc nhập URL HTTP(S)/đường dẫn ảnh
  nội bộ cũ. Mô tả có thanh công cụ định dạng rich-text; HTML được làm sạch ở
  máy chủ theo danh sách phần tử và liên kết cho phép trước khi lưu và hiển thị.
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
- Admin có thể hủy các đơn chưa ở trạng thái `Hoàn tất`, `Đã hủy` hoặc `Đã trả
  hàng`, nhập lý do và cộng lại tồn kho trong transaction. Trạng thái đơn được
  cập nhật có điều kiện để tránh hai yêu cầu đồng thời hoàn kho trùng lặp.
- Khách hàng có thể hủy đơn đến trước khi đơn chuyển sang `Đang vận chuyển`;
  cách xử lý và giới hạn chi tiết được mô tả trong mục theo dõi đơn hàng.

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
- Đánh giá: `BookReviews`; điểm trung bình và số lượt đánh giá trên sách được
  đồng bộ từ bảng này, sách chưa được đánh giá hiển thị `0.0` và `0 lượt`.

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

## 11. Cấu trúc dữ liệu chi tiết

Các tên cột dưới đây tương ứng với thuộc tính entity và schema SQL Server hiện
tại. Thuộc tính `SalePrice` là giá trị tính trong ứng dụng, không phải cột được
lưu riêng. Tên bảng và khóa ngoại có thể xem trong `Data/AppDbContext.cs` và
`Data/DbInitializer.cs`.

### Sách và danh mục

| Bảng | Trường nghiệp vụ | Ghi chú |
|---|---|---|
| `Books` | `Id`, `Title`, `Author`, `Category`, `ISBN` | `Category` lưu tên một danh mục. `ISBN` là chuỗi tùy chọn tối đa 20 ký tự; dữ liệu cũ được thêm giá trị rỗng, không tự suy đoán ISBN. |
| `Books` | `Price`, `DiscountPercent`, `DiscountAmount` | Giá tiền dùng decimal(18,2); phần trăm từ 0 đến 100. Giá bán tính theo `DiscountAmount` nếu lớn hơn 0, nếu không lấy `DiscountPercent`; không để giá bán âm. |
| `Books` | `Stock` | Số lượng tồn kho không âm. Đặt hàng trừ kho; hủy/hoàn trả hợp lệ cộng kho. |
| `Books` | `Rating`, `ReviewCount` | Điểm trung bình và số dòng đánh giá được tính từ `BookReviews` khi khởi tạo và sau khi khách đánh giá. Chưa có đánh giá: 0.0 điểm, 0 lượt. |
| `Books` | `CoverImage`, `Description` | Tham chiếu ảnh và mô tả HTML đã làm sạch; mô tả tối đa 20.000 ký tự sau khi sanitize. |
| `Categories` | `Id`, `Name`, `IsActive` | Danh mục hoạt động được giới hạn về danh sách cố định gồm 7 tên trong `StoreData.Categories`. |

`Rating` không đại diện cho doanh số. Số lượt mua cũng không suy ra từ số lượng
tồn kho. Số liệu bán được lấy từ `OrderItems` thuộc đơn `Hoàn tất`, còn đánh giá
được lấy từ `BookReviews`.

### Đơn hàng, đánh giá và hỗ trợ

| Bảng | Dữ liệu chính | Quan hệ / ý nghĩa |
|---|---|---|
| `Orders` | `Id`, `UserId`, `ReceiverName`, `Phone`, `Address`, `Status`, `Total`, `RefundedAmount`, `CancellationReason`, `CreatedAt`, `CompletedAt` | `UserId` liên kết chủ đơn; tổng tiền là tiền sách, không gồm phí giao hàng. `CompletedAt` được đặt khi Admin chuyển trạng thái sang hoàn tất. |
| `OrderItems` | `OrderId`, `BookId`, `BookTitle`, `Quantity`, `UnitPrice` | `BookTitle` và `UnitPrice` là ảnh chụp tại thời điểm đặt hàng; dùng dữ liệu này để giữ nội dung lịch sử đơn. |
| `OrderStatusHistories` | `OrderId`, `OccurredAt`, `Action` | Lịch sử các mốc xử lý; initializer tạo bản ghi đặt hàng cho các đơn chưa có lịch sử. |
| `BookReviews` | `BookId`, `OrderId`, `UserId`, `Rating`, `CreatedAt` | Mức sao 1–5. Mỗi tổ hợp sách/đơn/người dùng chỉ có một bản ghi; đánh giá trong một đơn chỉ hợp lệ sau khi đơn hoàn tất. |
| `ReturnRequests` | `OrderId`, `UserId`, thông tin email/điện thoại khách, `RequestType`, `Reason`, `Status`, `RejectionReason`, mốc thời gian | Yêu cầu hoàn tiền gắn với một đơn; khách không gửi lặp khi đã có yêu cầu đang xử lý/được chấp nhận. |
| `CustomerFeedbacks` | `UserId`, `Subject`, `Message`, `CustomerEmail`, `Status`, `CreatedAt` | Lưu góp ý và trạng thái phản hồi; các trường UserId/email/trạng thái do máy chủ gán, không nhận trực tiếp từ form khách. |
| `AspNetUsers` và các bảng Identity | thông tin tài khoản, hash mật khẩu, role, lockout | ASP.NET Core Identity quản lý xác thực và role. Mật khẩu dạng rõ không được lưu trong database. |

Quan hệ `OrderItems -> Orders`, `BookReviews -> Books/Orders`,
`OrderStatusHistories -> Orders` và `ReturnRequests -> Orders` được khai báo
qua khóa ngoại trong schema. Xóa sách qua Admin/API bị chặn khi có dòng đơn hoặc
đánh giá để tránh mất lịch sử.

### Hành vi seed và cập nhật dữ liệu

- `EnsureCreated` tạo schema trên database chưa có. Các câu SQL có điều kiện
  trong initializer bổ sung những bảng/cột đã biết cho schema cũ, gồm
  `ISBN`, `ReviewCount`, các cột đơn hàng/yêu cầu hỗ trợ, và chuẩn hóa một số
  trạng thái đơn cũ.
- Bộ sách mẫu được thêm toàn bộ khi chưa có sách; khi sách đã có, initializer
  chỉ bổ sung những tên sách seed chưa tồn tại. Không nên coi seed là thao tác
  đồng bộ/ghi đè toàn bộ tên, giá, tồn kho, ảnh hay mô tả của sách đã được quản
  trị viên chỉnh sửa.
- Danh mục bị đưa về bộ 7 danh mục cố định; một số tên danh mục cũ được ánh xạ
  sang tên hiện tại.
- Mỗi lần khởi tạo, `Rating` và `ReviewCount` được tính lại từ `BookReviews`.
  ISBN cũ không được biết sẽ để trống; quản trị viên có thể cập nhật trong form
  sách. Ứng dụng không tự tạo mã ISBN giả.
- Email tài khoản demo cũ được chuyển sang miền `groupnine.vn` nếu tài khoản
  mới chưa bị tài khoản khác chiếm. Các mật khẩu Admin cũ được đổi sang
  `Admin123456` khi mật khẩu hiện tại còn khớp một trong các mật khẩu seed cũ
  đã biết; mật khẩu khác do người dùng tự đổi không bị ghi đè.
- Mọi cập nhật trực tiếp từ initializer nên được sao lưu database trước khi
  triển khai trên dữ liệu quan trọng. Đây là cập nhật tương thích schema có
  phạm vi cụ thể, không thay thế migration versioned cho triển khai production.

## 12. Hợp đồng API và xử lý lỗi

API hiện có ở `Controllers/BooksApiController.cs`, prefix `/api/books`; dữ liệu
JSON mặc định dùng tên thuộc tính camelCase.

### Truy vấn danh sách

`GET /api/books` nhận các query parameter:

| Tham số | Kiểu / giá trị | Mặc định và quy tắc |
|---|---|---|
| `q` | chuỗi | Tìm chuỗi con trong tên sách hoặc tác giả; ISBN chưa được đưa vào tìm kiếm. |
| `category` | chuỗi lặp | Có thể truyền nhiều lần, ví dụ `?category=Truyện&category=Sách%20khoa%20học`. Sách phải thuộc một trong các giá trị đã chọn. |
| `promotion` | boolean | `true` chỉ lấy sách có phần trăm hoặc số tiền giảm lớn hơn 0. |
| `sort` | `new`, `rating`, `price-asc`, `price-desc` | Giá trị không hỗ trợ được chuẩn hóa về `new`. Giá áp dụng là giá sau giảm. |
| `page` | số nguyên | Trang nhỏ nhất là 1; nếu vượt tổng trang, trả trang cuối hiện có. |
| `pageSize` | số nguyên | Mặc định 12, giới hạn từ 1 đến tối đa 48. |

Response có dạng gồm `items`, `page`, `pageSize`, `totalCount` và `totalPages`.
Mỗi item gồm `id`, `title`, `author`, `category`, `isbn`, `price`,
`salePrice`, `discountPercent`, `discountAmount`, `stock`, `rating`,
`reviewCount`, `coverImage`, `description`. `description` được sanitize trước
khi trả ra.

### Đọc và ghi một sách

- `GET /api/books/{id}` trả một item theo cùng cấu trúc; trả 404 nếu không có
  sách hợp lệ.
- `POST /api/books` nhận JSON có `title`, `author`, `category`, `isbn`, `price`,
  `discountPercent`, `discountAmount`, `stock`, `coverImage`, `description`.
- `PUT /api/books/{id}` nhận cùng cấu trúc; trả 404 khi ID không tồn tại.
- `DELETE /api/books/{id}` trả 204 khi xóa thành công, 404 khi không tìm thấy,
  409 khi sách đã có đơn hàng hoặc đánh giá.
- POST/PUT/DELETE chỉ dành cho Admin. Cần lấy request token tại
  `GET /api/books/antiforgery` trong phiên Admin và gửi token trong header
  `X-CSRF-TOKEN`. API trả 401 khi chưa đăng nhập và 403 khi đã đăng nhập nhưng
  không đủ quyền.
- Ghi sách kiểm tra Annotation, danh mục còn hoạt động, URL ảnh HTTP(S) hoặc
  đường dẫn ảnh nội bộ hợp lệ, giới hạn độ dài mô tả và làm sạch HTML.

Ví dụ request tạo sách:

```json
{
  "title": "Tên sách",
  "author": "Tác giả",
  "category": "Truyện",
  "isbn": "",
  "price": 99000,
  "discountPercent": 0,
  "discountAmount": 0,
  "stock": 10,
  "coverImage": "/images/no-image.png",
  "description": "<p>Mô tả sách.</p>"
}
```

Các endpoint API không cung cấp phiên bản hóa, OpenAPI/Swagger, tìm kiếm nâng
cao, cập nhật/xóa đánh giá hay thao tác đơn hàng. Đây là API catalog/quản trị
sách, không phải một API công khai cho toàn bộ nghiệp vụ.

## 13. Validation và quy tắc đầu vào

Validation phía trình duyệt hỗ trợ thao tác thuận tiện; mọi quy tắc quan trọng
được kiểm tra lại trên máy chủ. Các giới hạn chính:

| Khu vực | Quy tắc |
|---|---|
| Đăng ký | Họ tên bắt buộc, tối đa 200 ký tự; email đúng định dạng, tối đa 256 ký tự và duy nhất; điện thoại hợp lệ, tối đa 50 ký tự và duy nhất; mật khẩu tối thiểu 8 ký tự, có ít nhất một chữ in hoa và một chữ số; xác nhận phải trùng mật khẩu. |
| Đăng nhập | Email hoặc số điện thoại cùng mật khẩu; hỗ trợ tùy chọn ghi nhớ đăng nhập; lỗi không tiết lộ liệu email/tài khoản có tồn tại hay không. |
| Sách | Tên/tác giả tối đa 200 ký tự; danh mục tối đa 100; ISBN tối đa 20; giá và giảm tiền không âm; phần trăm giảm 0–100; kho không âm; ảnh tối đa 1.000 ký tự; mô tả tối đa 20.000 ký tự sau sanitize. |
| Ảnh tải lên | Chỉ PNG/JPEG/WebP, tối đa 5 MiB; kiểm tra phần mở rộng và chữ ký file. URL ảnh chỉ chấp nhận HTTP(S), hoặc đường dẫn bắt đầu bằng `/` nhưng không phải `//`, không có `..` hay dấu `\`. |
| Đặt hàng | Tên nhận hàng bắt buộc, tối đa 200; điện thoại hợp lệ, tối đa 50; địa chỉ bắt buộc, tối đa 500. Tổng và mặt hàng được tính lại từ session/server, không tin các giá trị tổng do trình duyệt gửi. |
| Đánh giá | Chỉ sách có trong đơn của chính khách và đơn đã `Hoàn tất`; điểm 1–5. |
| Yêu cầu trả hàng | Đơn thuộc người đang đăng nhập, đã hoàn tất trong cửa sổ 7 ngày; lý do bắt buộc, tối đa 1.000 ký tự. |
| Góp ý | Chủ đề bắt buộc, tối đa 200; nội dung bắt buộc, tối đa 2.000; UserId/email/trạng thái được máy chủ thiết lập. |
| Ưu đãi | Phần trăm từ 0–100; số tiền giảm không âm; truy vấn giới hạn sách theo tên/ID và danh mục. |
| Hủy đơn bởi Admin | Lý do bắt buộc, tối đa 1.000 ký tự; đơn không được là đã hoàn tất, đã hủy hoặc đã trả hàng. |

Các Data Annotations là giới hạn đầu vào, không phải ràng buộc bảo đảm tính
đúng đắn của dữ liệu ISBN, giá thị trường, bản quyền ảnh hoặc thông tin sách.

## 14. Bảo mật, xác thực và session

- ASP.NET Core Identity quản lý hash mật khẩu, xác thực và role. Cấu hình mật
  khẩu mới yêu cầu 8 ký tự, chữ số và chữ in hoa; chữ thường/ký tự đặc biệt
  không bắt buộc. Tài khoản bị khóa 15 phút sau 5 lần đăng nhập sai.
- Tài khoản tự đăng ký chỉ nhận role `Customer`. Mọi route Admin được trang trí
  `[Authorize(Roles = "Admin")]`; UI ẩn thao tác không thay thế kiểm tra quyền
  phía server.
- Cookie Identity và cookie Session bật `HttpOnly`, `SameSite=Lax`; Session
  cookie được đánh dấu essential. Session hết hạn sau 2 giờ không hoạt động,
  được lưu bằng distributed-memory cache cục bộ chứ chưa dùng kho chia sẻ giữa
  nhiều instance.
- Các form POST quan trọng dùng anti-forgery token. API nhận token qua header
  `X-CSRF-TOKEN`. Không tắt kiểm tra token để xử lý lỗi form.
- Đăng nhập dùng `LocalRedirect` cho URL quay về nhằm chặn chuyển hướng ra
  ngoài website. API trả 401/403 thay vì redirect đến form HTML.
- Nội dung mô tả sách được làm sạch bằng HtmlSanitizer. Allowlist hiện gồm các
  thẻ định dạng cơ bản như `p`, `strong`, `em`, danh sách, heading, trích dẫn,
  liên kết; thuộc tính cho phép `href`, `title`; scheme liên kết `http`,
  `https`, `mailto`.
- Upload ảnh được giới hạn định dạng/kích thước, tên file do server tạo bằng
  GUID và chỉ xóa file nằm trong thư mục quản lý `wwwroot/uploads/books/`.
- Form hỗ trợ dùng input model riêng, không nhận trực tiếp UserId, trạng thái
  hoặc dữ liệu quản trị do người dùng gửi. Chủ sở hữu đơn được lọc theo UserId
  ở server.
- Môi trường production bật Exception Handler và HSTS; mọi môi trường đều bật
  HTTPS redirection. Mặc định development có thể hiện thông tin lỗi chi tiết,
  nên không chạy môi trường Development trên máy chủ public.
- Connection string mặc định dùng Windows Integrated Security và không chứa
  password. Không commit thông tin xác thực, khóa ứng dụng hay dữ liệu cá nhân
  thật vào repository.

Các cài đặt cookie hiện chưa cấu hình `SecurePolicy` tường minh, rate limiting
toàn site, MFA, xác minh email/điện thoại, kho session phân tán, logging/audit
đầy đủ hoặc quy trình quản lý secret production. Cần bổ sung và đánh giá riêng
trước khi triển khai Internet production.

## 15. Front-end, AJAX và khả năng responsive

- Razor Views kết hợp Bootstrap và CSS dự án; JavaScript phía trình duyệt xử lý
  catalog, tương tác giao diện và editor Admin. CSS riêng đặt trong
  `wwwroot/css/`, script trong `wwwroot/js/`.
- Bộ lọc catalog dùng các tham số GET `q`, `category` lặp, `promotion`, `sort`,
  `page`. Danh mục là checkbox cho phép chọn nhiều mục; nút xóa bộ lọc trở về
  catalog mặc định.
- Khi JavaScript hoạt động, `book-catalog.js` gửi AJAX đến `/api/books`, thay
  danh sách kết quả và phân trang mà không tải lại tài liệu. URL được cập nhật
  bằng History API; thao tác Back/Forward khôi phục từ khóa, các danh mục, sắp
  xếp và khuyến mãi. Khi API lỗi, trang điều hướng về MVC form để vẫn hiển thị
  kết quả phía server.
- Khi JavaScript tắt, form GET và phân trang MVC vẫn gửi cùng tham số; đây là
  fallback của trang catalog.
- Giá trị số và nội dung động được tạo bằng DOM text node trong kết quả AJAX;
  mô tả HTML do API trả đã được sanitize ở máy chủ.
- Panel catalog, nhãn checkbox, lựa chọn sắp xếp và thao tác tìm kiếm được bố
  trí lại theo breakpoint mobile; các trang storefront/Admin dùng Bootstrap
  grid và CSS responsive. Bảng quản trị có thể cuộn ngang trong vùng bảng thay
  vì làm rộng toàn trang.
- `wwwroot/css/responsive-overrides.css` bổ sung giao diện bộ lọc và một số
  điều chỉnh mobile. Khi thay đổi markup của form, phải kiểm tra lại cả
  serialization AJAX và khôi phục trạng thái browser history.

Responsive là hành vi cần kiểm tra trên kích thước thực tế, không chỉ suy ra từ
việc đã dùng Bootstrap. Nên kiểm tra ít nhất 375–390 px, tablet và desktop;
quan sát tràn ngang, thao tác bằng cảm ứng, chiều rộng form/bảng và khả năng
đọc nội dung.

## 16. Kiểm tra sau thay đổi và smoke test

Repository hiện không có project test tự động riêng. Build xác nhận mã nguồn
biên dịch; sau khi thay đổi luồng nghiệp vụ nên chạy ứng dụng và kiểm tra thủ
công các luồng ảnh hưởng. Các lệnh cơ bản:

```powershell
dotnet restore
dotnet build
dotnet run
```

Checklist smoke test gợi ý:

1. Mở trang chủ, catalog, trang chi tiết; kiểm tra lọc từ khóa, chọn nhiều danh
   mục, khuyến mãi, đủ lựa chọn sắp xếp, phân trang, Back/Forward và fallback
   GET khi tắt JavaScript.
2. Kiểm tra catalog/chi tiết sách ở mobile 375–390 px, tablet và desktop; bảo
   đảm không tràn ngang ngoài vùng bảng cuộn có chủ đích.
3. Đăng ký Customer với dữ liệu hợp lệ, email/điện thoại trùng, mật khẩu thiếu
   chữ hoa/chữ số hoặc ngắn hơn 8 ký tự; xác nhận chỉ role Customer được cấp.
4. Đăng nhập Admin/Customer; xác nhận Customer không vào `/Admin`; thử Admin
   thêm/sửa/xem/xóa sách, phân trang, danh mục, upload ảnh hợp lệ/sai định
   dạng/sai chữ ký/vượt 5 MiB, mô tả rich-text và ưu đãi.
5. Thử API GET danh sách/chi tiết, đăng nhập Admin để POST/PUT/DELETE với token;
   xác nhận anonymous 401, Customer 403, dữ liệu sai 400, không tìm thấy 404,
   sách có lịch sử 409.
6. Với Customer, thêm sách vào giỏ, sửa số lượng, đặt hàng; kiểm tra tồn kho
   giảm, tổng từ giá server và đơn ở trạng thái ban đầu.
7. Hủy đơn ở từng trạng thái trước vận chuyển; xác nhận đơn chuyển `Đã hủy`,
   tồn kho phục hồi đúng một lần và không thể hủy sau khi đã `Đang vận chuyển`.
8. Với Admin, tiến đơn qua từng trạng thái, hủy đơn đủ điều kiện, kiểm tra
   lịch sử, tồn kho và `CompletedAt`; xác nhận doanh thu/bán chạy chỉ tính đơn
   `Hoàn tất`.
9. Gửi yêu cầu hoàn tiền trong/ngoài 7 ngày, xử lý chấp nhận/từ chối; kiểm tra
   trạng thái đơn, lý do từ chối, hoàn kho và ngăn yêu cầu trùng.
10. Đánh giá sách chỉ từ đơn hoàn tất; xác nhận một đánh giá cho cùng
    sách/đơn/người, điểm trung bình và số lượt trên catalog/chi tiết/Admin.
11. Gửi góp ý, đổi trạng thái tại Admin và xác nhận form không thể tự giả mạo
    UserId/email/trạng thái.

Kết quả smoke test phụ thuộc dữ liệu hiện có và database mục tiêu; không dùng
database production hoặc dữ liệu khách thật để thử thao tác tạo/hủy đơn. Với
thử nghiệm database, dùng bản sao có thể khôi phục và xóa dữ liệu test theo
quy trình đã xác nhận.

## 17. Phạm vi chưa triển khai

Những giới hạn sau là chủ ý của phiên bản hiện tại, không nên mô tả như tính
năng đã có:

- Không có cổng thanh toán điện tử, tích hợp vận chuyển, tính phí giao hàng,
  mã giảm giá có hạn sử dụng hoặc theo dõi vận đơn.
- Không có API công khai cho tài khoản, đơn hàng, hoàn tiền, đánh giá hoặc góp
  ý; API hiện tại phục vụ đọc catalog và CRUD sách có quyền Admin.
- Không có tìm kiếm theo ISBN, tra cứu/xác minh ISBN từ dịch vụ ngoài, hoặc dữ
  liệu ISBN được seed cho mọi đầu sách.
- Đánh giá hiện là số sao gắn với đơn; không có nội dung nhận xét bằng văn bản,
  hình ảnh đánh giá, kiểm duyệt hay phản hồi đánh giá.
- Session dùng bộ nhớ của một instance; giỏ hàng không đồng bộ giữa nhiều
  instance và mất khi ứng dụng khởi động lại.
- Database bootstrap dùng `EnsureCreated` và SQL có điều kiện, chưa dùng EF
  Core migrations có version, rollback hay pipeline migration.
- Không có test project tự động, pipeline CI/CD, dashboard quan sát, chính sách
  backup tự động hoặc quy trình khôi phục đã được kiểm chứng trong ứng dụng.
- Một số dữ liệu tĩnh như danh mục đến từ `StoreData`; không có giao diện thêm/
  xóa tùy ý danh mục.

## 18. Các file tham chiếu chính

| Khu vực | File tham chiếu |
|---|---|
| Cấu hình dịch vụ, Identity, cookie, session, middleware và route | `Program.cs` |
| Entity, validation annotations, seed sách, danh mục, API models và catalog query | `Models/Book.cs`, `Models/BookCatalogModels.cs` |
| EF Core và Identity schema | `Data/AppDbContext.cs` |
| Khởi tạo schema, cập nhật dữ liệu cũ, seed role/tài khoản/sách | `Data/DbInitializer.cs` |
| Tài khoản | `Controllers/AccountController.cs`, `Views/Account/` |
| Trang chủ và catalog | `Controllers/HomeController.cs`, `Controllers/BooksController.cs`, `Controllers/BooksApiController.cs`, `Views/Books/`, `wwwroot/js/book-catalog.js` |
| Giỏ hàng và checkout | `Controllers/CartController.cs`, `Controllers/CheckoutController.cs` |
| Đơn hàng, đánh giá, hoàn tiền, góp ý | `Controllers/OrdersController.cs`, `Controllers/SupportController.cs` |
| Quản trị | `Controllers/AdminController.cs`, `Views/Admin/` |
| Giao diện và responsive | `Views/Shared/`, `wwwroot/css/site.css`, `wwwroot/css/responsive-overrides.css` |
| Hướng dẫn cài đặt ngắn | `README.md` |
