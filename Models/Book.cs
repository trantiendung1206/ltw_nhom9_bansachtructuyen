using System.ComponentModel.DataAnnotations;

namespace sc.Models;

public sealed class Book
{
    public int Id { get; set; }
    [Required, StringLength(200)]
    public string Title { get; set; } = "";
    [Required, StringLength(200)]
    public string Author { get; set; } = "";
    [Required, StringLength(100)]
    public string Category { get; set; } = "";
    [StringLength(20)]
    public string ISBN { get; set; } = "";
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }
    [Range(0, 100)]
    public int DiscountPercent { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    [StringLength(1000)]
    public string CoverImage { get; set; } = "";
    [StringLength(20000)]
    public string Description { get; set; } = "";
    public decimal SalePrice => Math.Max(0, Price - (DiscountAmount > 0 ? DiscountAmount : Price * DiscountPercent / 100));
}

public sealed class BookEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(200)]
    public string Author { get; set; } = "";

    [Required, StringLength(100)]
    public string Category { get; set; } = "";

    [StringLength(20)]
    public string ISBN { get; set; } = "";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }

    [Range(0, 100)]
    public int DiscountPercent { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [StringLength(1000)]
    public string CoverImage { get; set; } = "";

    [StringLength(20000)]
    public string Description { get; set; } = "";

    public IFormFile? CoverUpload { get; set; }
}

public sealed class Category
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class Order
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string ReceiverName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Status { get; set; } = "Cần xác nhận";
    public string CancellationReason { get; set; } = "";
    public decimal Total { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class BookReview
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int OrderId { get; set; }
    public string UserId { get; set; } = "";
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Action { get; set; } = "";
    public Order? Order { get; set; }
}

public sealed class ReturnRequest
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }
    public string UserId { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string RequestType { get; set; } = "Hoàn tiền";
    [Required, StringLength(1000)]
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Chờ xác nhận";
    public string RejectionReason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ResolvedAt { get; set; }
    public Order? Order { get; set; }
}

public sealed class CustomerFeedback
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    [Required, StringLength(200)]
    public string Subject { get; set; } = "";
    [Required, StringLength(2000)]
    public string Message { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Status { get; set; } = "Chưa xử lý";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class AdminStatisticsViewModel
{
    public int TotalBooks { get; set; }
    public int TotalStock { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }
    public decimal Revenue { get; set; }
    public string BestSellingBook { get; set; } = "Chưa có dữ liệu";
    public string BestSellingCategory { get; set; } = "Chưa có dữ liệu";
    public List<Book> FeaturedBooks { get; set; } = [];
}

public sealed class PromotionViewModel
{
    [StringLength(200)]
    public string? Query { get; set; }
    [StringLength(100)]
    public string? Category { get; set; }
    [Range(0, 100)]
    public int DiscountPercent { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }
    public List<Book> Books { get; set; } = [];
}

public sealed class CartItem
{
    public Book Book { get; init; } = new();
    public int Quantity { get; set; }
    public decimal Subtotal => Book.SalePrice * Quantity;
}

public sealed class CheckoutViewModel
{
    [Required, StringLength(200)]
    public string ReceiverName { get; set; } = "";
    [Required, Phone, StringLength(50)]
    public string Phone { get; set; } = "";
    [Required, StringLength(500)]
    public string Address { get; set; } = "";
    [StringLength(50)]
    public string CouponCode { get; set; } = "";
    public List<CartItem> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(item => item.Subtotal);
    public decimal Total => Subtotal;
}

public sealed class ReturnRequestInput
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Required, StringLength(1000)]
    public string Reason { get; set; } = "";
}

public sealed class CustomerFeedbackInput
{
    [Required, StringLength(200)]
    public string Subject { get; set; } = "";

    [Required, StringLength(2000)]
    public string Message { get; set; } = "";
}

public sealed class BookApiWriteModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(200)]
    public string Author { get; set; } = "";

    [Required, StringLength(100)]
    public string Category { get; set; } = "";

    [StringLength(20)]
    public string ISBN { get; set; } = "";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }

    [Range(0, 100)]
    public int DiscountPercent { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [StringLength(1000)]
    public string CoverImage { get; set; } = "";

    [StringLength(20000)]
    public string Description { get; set; } = "";
}

public static class StoreData
{
    public static readonly List<Book> Books =
    [
        new() { Id = 1, Title = "Nhà giả kim (The Alchemist)", Author = "Paulo Coelho", Category = "Văn học & Tiểu thuyết", ISBN = "9780061233845", Price = 220000, Stock = 20, CoverImage = "/images/books/1.jpg", Description = "Chàng chăn cừu Santiago đi tìm kho báu và học cách theo đuổi giấc mơ." },
        new() { Id = 2, Title = "Giết con chim nhại (To Kill a Mockingbird)", Author = "Harper Lee", Category = "Văn học & Tiểu thuyết", ISBN = "9780061120084", Price = 230000, Stock = 20, CoverImage = "/images/books/2.jpg", Description = "Câu chuyện về công lý và định kiến chủng tộc ở miền Nam nước Mỹ." },
        new() { Id = 3, Title = "1984", Author = "George Orwell", Category = "Văn học & Tiểu thuyết", ISBN = "9780451524935", Price = 160000, Stock = 20, CoverImage = "/images/books/3.jpg", Description = "Tiểu thuyết phản địa đàng về xã hội bị giám sát toàn diện." },
        new() { Id = 4, Title = "Kiêu hãnh và định kiến (Pride and Prejudice)", Author = "Jane Austen", Category = "Văn học & Tiểu thuyết", ISBN = "9780141439518", Price = 180000, Stock = 20, CoverImage = "/images/books/4.jpg", Description = "Chuyện tình Elizabeth Bennet và ngài Darcy trong xã hội Anh thế kỷ 19." },
        new() { Id = 5, Title = "Gatsby vĩ đại (The Great Gatsby)", Author = "F. Scott Fitzgerald", Category = "Văn học & Tiểu thuyết", ISBN = "9780743273565", Price = 150000, Stock = 20, CoverImage = "/images/books/5.jpg", Description = "Giấc mơ Mỹ và tình yêu bi kịch của triệu phú Jay Gatsby." },
        new() { Id = 6, Title = "Rừng Na Uy (Norwegian Wood)", Author = "Haruki Murakami", Category = "Văn học & Tiểu thuyết", ISBN = "9780375704024", Price = 300000, Stock = 20, CoverImage = "/images/books/6.jpg", Description = "Hồi ức tuổi trẻ, tình yêu và mất mát của một sinh viên Tokyo." },
        new() { Id = 7, Title = "Trăm năm cô đơn (One Hundred Years of Solitude)", Author = "Gabriel García Márquez", Category = "Văn học & Tiểu thuyết", ISBN = "9780060883287", Price = 300000, Stock = 20, CoverImage = "/images/books/7.jpg", Description = "Biên niên sử bảy thế hệ nhà Buendía ở làng Macondo." },
        new() { Id = 8, Title = "Introduction to Algorithms (4th ed.)", Author = "Cormen, Leiserson, Rivest, Stein", Category = "Sách giáo khoa", ISBN = "9780262046305", Price = 1450000, Stock = 20, CoverImage = "/images/books/8.jpg", Description = "Giáo trình kinh điển về thuật toán và cấu trúc dữ liệu." },
        new() { Id = 9, Title = "Campbell Biology (12th ed.)", Author = "Urry, Cain, Wasserman và cộng sự", Category = "Sách giáo khoa", ISBN = "9780135188743", Price = 2300000, Stock = 20, CoverImage = "/images/books/9.jpg", Description = "Giáo trình sinh học đại cương toàn diện, nhiều hình minh họa." },
        new() { Id = 10, Title = "Calculus: Early Transcendentals (8th ed.)", Author = "James Stewart", Category = "Sách giáo khoa", ISBN = "9781285740621", Price = 1300000, Stock = 20, CoverImage = "/images/books/10.jpg", Description = "Giáo trình giải tích: giới hạn, đạo hàm, tích phân." },
        new() { Id = 11, Title = "Database System Concepts (7th ed.)", Author = "Silberschatz, Korth, Sudarshan", Category = "Sách giáo khoa", ISBN = "9780078022159", Price = 1100000, Stock = 20, CoverImage = "/images/books/11.jpg", Description = "Giáo trình cơ sở dữ liệu: mô hình quan hệ, SQL, giao dịch." },
        new() { Id = 12, Title = "Computer Networking: A Top-Down Approach (8th ed.)", Author = "Kurose, Ross", Category = "Sách giáo khoa", ISBN = "9780135928615", Price = 1200000, Stock = 20, CoverImage = "/images/books/12.jpg", Description = "Giới thiệu mạng máy tính theo hướng từ tầng ứng dụng xuống." },
        new() { Id = 13, Title = "Introduction to Linear Algebra (5th ed.)", Author = "Gilbert Strang", Category = "Sách giáo khoa", ISBN = "9780980232776", Price = 1400000, Stock = 20, CoverImage = "/images/books/13.jpg", Description = "Giáo trình đại số tuyến tính: ma trận, không gian vectơ." },
        new() { Id = 14, Title = "Atomic Habits (Thói quen nguyên tử)", Author = "James Clear", Category = "Kĩ năng & Kinh doanh", ISBN = "9780735211292", Price = 420000, Stock = 20, CoverImage = "/images/books/14.jpg", Description = "Xây thói quen tốt bằng những thay đổi nhỏ và đều đặn." },
        new() { Id = 15, Title = "Đắc nhân tâm (How to Win Friends and Influence People)", Author = "Dale Carnegie", Category = "Kĩ năng & Kinh doanh", ISBN = "9780671027032", Price = 190000, Stock = 20, CoverImage = "/images/books/15.jpg", Description = "Nguyên tắc giao tiếp, thuyết phục và tạo thiện cảm." },
        new() { Id = 16, Title = "7 thói quen của người thành đạt", Author = "Stephen R. Covey", Category = "Kĩ năng & Kinh doanh", ISBN = "9781982137274", Price = 380000, Stock = 20, CoverImage = "/images/books/16.jpg", Description = "Bảy thói quen xây dựng hiệu quả cá nhân và lãnh đạo." },
        new() { Id = 17, Title = "Nghĩ giàu làm giàu (Think and Grow Rich)", Author = "Napoleon Hill", Category = "Kĩ năng & Kinh doanh", ISBN = "9781585424337", Price = 250000, Stock = 20, CoverImage = "/images/books/17.jpg", Description = "Các nguyên tắc tư duy và hành động để đạt thành công tài chính." },
        new() { Id = 18, Title = "Deep Work (Làm việc sâu)", Author = "Cal Newport", Category = "Kĩ năng & Kinh doanh", ISBN = "9781455586691", Price = 330000, Stock = 20, CoverImage = "/images/books/18.jpg", Description = "Rèn khả năng tập trung sâu trong thời đại phân tâm." },
        new() { Id = 19, Title = "Zero to One (Từ không đến một)", Author = "Peter Thiel, Blake Masters", Category = "Kĩ năng & Kinh doanh", ISBN = "9780804139298", Price = 300000, Stock = 20, CoverImage = "/images/books/19.jpg", Description = "Cách xây dựng startup tạo ra giá trị mới." },
        new() { Id = 20, Title = "The Lean Startup", Author = "Eric Ries", Category = "Kĩ năng & Kinh doanh", ISBN = "9780307887894", Price = 330000, Stock = 20, CoverImage = "/images/books/20.jpg", Description = "Phương pháp khởi nghiệp tinh gọn: thử nghiệm nhanh, học từ dữ liệu." },
        new() { Id = 21, Title = "Cha giàu cha nghèo (Rich Dad Poor Dad)", Author = "Robert T. Kiyosaki", Category = "Kĩ năng & Kinh doanh", ISBN = "9781612681139", Price = 280000, Stock = 20, CoverImage = "/images/books/21.jpg", Description = "Bài học tài chính cá nhân qua hai người cha với hai tư duy khác nhau." },
        new() { Id = 22, Title = "Good to Great (Từ tốt đến vĩ đại)", Author = "Jim Collins", Category = "Kĩ năng & Kinh doanh", ISBN = "9780066620992", Price = 400000, Stock = 20, CoverImage = "/images/books/22.jpg", Description = "Nghiên cứu vì sao một số công ty vượt trội bền vững." },
        new() { Id = 23, Title = "Sapiens: Lược sử loài người", Author = "Yuval Noah Harari", Category = "Sách khoa học", ISBN = "9780062316097", Price = 400000, Stock = 20, CoverImage = "/images/books/23.jpg", Description = "Lịch sử loài người từ thời nguyên thủy đến hiện đại." },
        new() { Id = 24, Title = "Lược sử thời gian (A Brief History of Time)", Author = "Stephen Hawking", Category = "Sách khoa học", ISBN = "9780553380163", Price = 280000, Stock = 20, CoverImage = "/images/books/24.jpg", Description = "Giải thích vũ trụ, hố đen, Big Bang cho độc giả phổ thông." },
        new() { Id = 25, Title = "Gen vị kỷ (The Selfish Gene)", Author = "Richard Dawkins", Category = "Sách khoa học", ISBN = "9780198788607", Price = 330000, Stock = 20, CoverImage = "/images/books/25.jpg", Description = "Góc nhìn tiến hóa lấy gen làm trung tâm." },
        new() { Id = 26, Title = "Astrophysics for People in a Hurry", Author = "Neil deGrasse Tyson", Category = "Sách khoa học", ISBN = "9780393609394", Price = 250000, Stock = 20, CoverImage = "/images/books/26.jpg", Description = "Thiên văn học cô đọng cho người bận rộn." },
        new() { Id = 27, Title = "The Gene: An Intimate History", Author = "Siddhartha Mukherjee", Category = "Sách khoa học", ISBN = "9781476733524", Price = 380000, Stock = 20, CoverImage = "/images/books/27.jpg", Description = "Lịch sử khám phá gen và di truyền học." },
        new() { Id = 28, Title = "A Short History of Nearly Everything", Author = "Bill Bryson", Category = "Sách khoa học", ISBN = "9780767908184", Price = 280000, Stock = 20, CoverImage = "/images/books/28.jpg", Description = "Hành trình khoa học từ Big Bang đến con người, văn phong dí dỏm." },
        new() { Id = 29, Title = "Thinking, Fast and Slow (Tư duy nhanh và chậm)", Author = "Daniel Kahneman", Category = "Sách khoa học", ISBN = "9780374533557", Price = 330000, Stock = 20, CoverImage = "/images/books/29.jpg", Description = "Hai hệ thống tư duy và những thiên kiến khi ra quyết định." },
        new() { Id = 30, Title = "Harry Potter và Hòn đá Phù thủy", Author = "J.K. Rowling", Category = "Sách thiếu nhi", ISBN = "9781408855652", Price = 330000, Stock = 20, CoverImage = "/images/books/30.jpg", Description = "Cậu bé Harry phát hiện mình là phù thủy và vào trường Hogwarts." },
        new() { Id = 31, Title = "Hoàng tử bé (The Little Prince)", Author = "Antoine de Saint-Exupéry", Category = "Sách thiếu nhi", ISBN = "9780156012195", Price = 200000, Stock = 20, CoverImage = "/images/books/31.jpg", Description = "Câu chuyện triết lý nhẹ nhàng về tình bạn và tình yêu." },
        new() { Id = 32, Title = "Charlotte's Web (Mạng nhện của Charlotte)", Author = "E.B. White", Category = "Sách thiếu nhi", ISBN = "9780064400558", Price = 160000, Stock = 20, CoverImage = "/images/books/32.jpg", Description = "Tình bạn giữa chú lợn Wilbur và nhện Charlotte." },
        new() { Id = 33, Title = "The Very Hungry Caterpillar", Author = "Eric Carle", Category = "Sách thiếu nhi", ISBN = "9780399226908", Price = 330000, Stock = 20, CoverImage = "/images/books/33.jpg", Description = "Sách tranh về chú sâu đói ăn và hóa bướm." },
        new() { Id = 34, Title = "Where the Wild Things Are", Author = "Maurice Sendak", Category = "Sách thiếu nhi", ISBN = "9780064431781", Price = 230000, Stock = 20, CoverImage = "/images/books/34.jpg", Description = "Cậu bé Max đến xứ sở quái vật bằng trí tưởng tượng." },
        new() { Id = 35, Title = "Matilda", Author = "Roald Dahl", Category = "Sách thiếu nhi", ISBN = "9780142410370", Price = 170000, Stock = 20, CoverImage = "/images/books/35.jpg", Description = "Cô bé thông minh có năng lực đặc biệt chống lại người lớn xấu tính." },
        new() { Id = 36, Title = "Charlie and the Chocolate Factory", Author = "Roald Dahl", Category = "Sách thiếu nhi", ISBN = "9780142410318", Price = 170000, Stock = 20, CoverImage = "/images/books/36.jpg", Description = "Chuyến thăm nhà máy kẹo kỳ diệu của ông Wonka." },
        new() { Id = 37, Title = "Diary of a Wimpy Kid", Author = "Jeff Kinney", Category = "Sách thiếu nhi", ISBN = "9780810993136", Price = 200000, Stock = 20, CoverImage = "/images/books/37.jpg", Description = "Nhật ký hài hước của cậu học sinh cấp hai, kèm tranh minh họa." },
        new() { Id = 38, Title = "One Piece, Vol. 1", Author = "Eiichiro Oda", Category = "Truyện", ISBN = "9781569319017", Price = 250000, Stock = 20, CoverImage = "/images/books/38.jpg", Description = "Luffy bắt đầu hành trình trở thành Vua Hải Tặc." },
        new() { Id = 39, Title = "Naruto, Vol. 1", Author = "Masashi Kishimoto", Category = "Truyện", ISBN = "9781569319000", Price = 250000, Stock = 20, CoverImage = "/images/books/39.jpg", Description = "Cậu bé ninja Naruto mơ ước trở thành Hokage." },
        new() { Id = 40, Title = "Death Note, Vol. 1", Author = "Tsugumi Ohba, Takeshi Obata", Category = "Truyện", ISBN = "9781421501680", Price = 270000, Stock = 20, CoverImage = "/images/books/40.jpg", Description = "Học sinh nhặt được cuốn sổ có thể giết người chỉ bằng cách viết tên." },
        new() { Id = 41, Title = "Attack on Titan, Vol. 1", Author = "Hajime Isayama", Category = "Truyện", ISBN = "9781612620244", Price = 290000, Stock = 20, CoverImage = "/images/books/41.jpg", Description = "Nhân loại sống sau bức tường để tránh những Titan khổng lồ." },
        new() { Id = 42, Title = "Fullmetal Alchemist, Vol. 1", Author = "Hiromu Arakawa", Category = "Truyện", ISBN = "9781591169208", Price = 290000, Stock = 20, CoverImage = "/images/books/42.jpg", Description = "Hai anh em giả kim thuật sư đi tìm cách lấy lại cơ thể." },
        new() { Id = 43, Title = "English Grammar in Use (5th ed.)", Author = "Raymond Murphy", Category = "Sách Ngoại Ngữ", ISBN = "9781108457651", Price = 400000, Stock = 20, CoverImage = "/images/books/43.jpg", Description = "Sách ngữ pháp tiếng Anh trình độ trung cấp, có bài tập." },
        new() { Id = 44, Title = "Practical English Usage (4th ed.)", Author = "Michael Swan", Category = "Sách Ngoại Ngữ", ISBN = "9780194202466", Price = 650000, Stock = 20, CoverImage = "/images/books/44.jpg", Description = "Tra cứu cách dùng tiếng Anh và các lỗi thường gặp." },
        new() { Id = 45, Title = "Genki I (3rd ed.)", Author = "Eri Banno và cộng sự", Category = "Sách Ngoại Ngữ", ISBN = "9784789017305", Price = 700000, Stock = 20, CoverImage = "/images/books/45.jpg", Description = "Giáo trình tiếng Nhật sơ cấp." },
        new() { Id = 46, Title = "Le Petit Prince (bản tiếng Pháp)", Author = "Antoine de Saint-Exupéry", Category = "Sách Ngoại Ngữ", ISBN = "9782070612758", Price = 190000, Stock = 20, CoverImage = "/images/books/46.jpg", Description = "Bản gốc tiếng Pháp, phù hợp đọc luyện tiếng Pháp." },
        new() { Id = 47, Title = "El Principito (bản tiếng Tây Ban Nha)", Author = "Antoine de Saint-Exupéry", Category = "Sách Ngoại Ngữ", ISBN = "9788498381498", Price = 250000, Stock = 20, CoverImage = "/images/books/47.jpg", Description = "Bản dịch tiếng Tây Ban Nha của Hoàng tử bé." }
    ];

    public static readonly string[] Categories =
    [
        "Văn học & Tiểu thuyết", "Sách giáo khoa", "Kĩ năng & Kinh doanh",
        "Sách khoa học", "Sách thiếu nhi", "Truyện", "Sách Ngoại Ngữ"
    ];
}
