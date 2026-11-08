using System.ComponentModel.DataAnnotations;

namespace sc.Models;

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public string CoverImage { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal SalePrice => Math.Max(0, Price - (DiscountAmount > 0 ? DiscountAmount : Price * DiscountPercent / 100));
}

public sealed class Category
{
    public int Id { get; set; }
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
    public int OrderId { get; set; }
    public string UserId { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string RequestType { get; set; } = "Hoàn tiền";
    [Required]
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
    [Required]
    public string Subject { get; set; } = "";
    [Required]
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
    public string? Query { get; set; }
    public string? Category { get; set; }
    public int DiscountPercent { get; set; }
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
    public string ReceiverName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string CouponCode { get; set; } = "";
    public List<CartItem> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(item => item.Subtotal);
    public decimal Total => Subtotal;
}

public static class StoreData
{
    public static readonly List<Book> Books =
    [
        new() { Id = 1, Title = "Nhà giả kim", Author = "Paulo Coelho", Category = "Văn học & Tiểu thuyết", Price = 89000, DiscountPercent = 10, Stock = 24, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=80", Description = "Một câu chuyện truyền cảm hứng về hành trình theo đuổi ước mơ và lắng nghe trái tim." },
        new() { Id = 2, Title = "Atomic Habits", Author = "James Clear", Category = "Kĩ năng & Kinh doanh", Price = 175000, DiscountPercent = 15, Stock = 18, Rating = 4.9, CoverImage = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&q=80", Description = "Những thay đổi nhỏ tạo nên kết quả phi thường trong cuộc sống." },
        new() { Id = 3, Title = "Muôn kiếp nhân sinh", Author = "Nguyên Phong", Category = "Văn học & Tiểu thuyết", Price = 138000, DiscountPercent = 0, Stock = 12, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=600&q=80", Description = "Câu chuyện về nhân quả, lựa chọn và hành trình trưởng thành của con người." },
        new() { Id = 4, Title = "Lược sử loài người", Author = "Yuval Noah Harari", Category = "Sách khoa học", Price = 220000, DiscountPercent = 20, Stock = 7, Rating = 4.6, CoverImage = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&q=80", Description = "Một góc nhìn rộng lớn về lịch sử tiến hóa của Homo sapiens." },
        new() { Id = 5, Title = "Dế Mèn phiêu lưu ký", Author = "Tô Hoài", Category = "Sách thiếu nhi", Price = 65000, DiscountPercent = 0, Stock = 32, Rating = 4.9, CoverImage = "https://images.unsplash.com/photo-1629992101753-56d196c8aabb?w=600&q=80", Description = "Tác phẩm thiếu nhi kinh điển đồng hành cùng nhiều thế hệ độc giả Việt Nam." },
        new() { Id = 6, Title = "English Grammar in Use", Author = "Raymond Murphy", Category = "Sách Ngoại Ngữ", Price = 190000, DiscountPercent = 10, Stock = 9, Rating = 4.5, CoverImage = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600&q=80", Description = "Tài liệu ngữ pháp tiếng Anh thực hành dành cho người học trình độ trung cấp." },
        new() { Title = "Mắt biếc", Author = "Nguyễn Nhật Ánh", Category = "Văn học & Tiểu thuyết", Price = 110000, DiscountPercent = 10, Stock = 20, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=80" },
        new() { Title = "Tuổi trẻ đáng giá bao nhiêu", Author = "Rosie Nguyễn", Category = "Kĩ năng & Kinh doanh", Price = 99000, DiscountPercent = 15, Stock = 25, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&q=80" },
        new() { Title = "Cây cam ngọt của tôi", Author = "José Mauro de Vasconcelos", Category = "Văn học & Tiểu thuyết", Price = 125000, DiscountPercent = 12, Stock = 16, Rating = 4.9, CoverImage = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=600&q=80" },
        new() { Title = "Không gia đình", Author = "Hector Malot", Category = "Văn học & Tiểu thuyết", Price = 95000, DiscountPercent = 0, Stock = 19, Rating = 4.6, CoverImage = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&q=80" },
        new() { Title = "Bố già", Author = "Mario Puzo", Category = "Văn học & Tiểu thuyết", Price = 145000, DiscountPercent = 10, Stock = 13, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1629992101753-56d196c8aabb?w=600&q=80" },
        new() { Title = "Bộ đề ôn thi Toán lớp 12", Author = "Nhiều tác giả", Category = "Sách giáo khoa", Price = 85000, DiscountPercent = 20, Stock = 30, Rating = 4.4, CoverImage = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600&q=80" },
        new() { Title = "Tổng ôn Ngữ văn THPT", Author = "Nhiều tác giả", Category = "Sách giáo khoa", Price = 79000, DiscountPercent = 10, Stock = 27, Rating = 4.5, CoverImage = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&q=80" },
        new() { Title = "Vật lý 12 nâng cao", Author = "Nhiều tác giả", Category = "Sách giáo khoa", Price = 72000, DiscountPercent = 0, Stock = 22, Rating = 4.3, CoverImage = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&q=80" },
        new() { Title = "Đắc nhân tâm", Author = "Dale Carnegie", Category = "Kĩ năng & Kinh doanh", Price = 86000, DiscountPercent = 10, Stock = 40, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=80" },
        new() { Title = "7 thói quen hiệu quả", Author = "Stephen R. Covey", Category = "Kĩ năng & Kinh doanh", Price = 155000, DiscountPercent = 15, Stock = 18, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&q=80" },
        new() { Title = "Nghệ thuật bán hàng", Author = "Brian Tracy", Category = "Kĩ năng & Kinh doanh", Price = 135000, DiscountPercent = 0, Stock = 14, Rating = 4.4, CoverImage = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=600&q=80" },
        new() { Title = "Sapiens", Author = "Yuval Noah Harari", Category = "Sách khoa học", Price = 245000, DiscountPercent = 12, Stock = 10, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&q=80" },
        new() { Title = "Vũ trụ trong vỏ hạt dẻ", Author = "Stephen Hawking", Category = "Sách khoa học", Price = 180000, DiscountPercent = 10, Stock = 11, Rating = 4.6, CoverImage = "https://images.unsplash.com/photo-1629992101753-56d196c8aabb?w=600&q=80" },
        new() { Title = "Lược sử thời gian", Author = "Stephen Hawking", Category = "Sách khoa học", Price = 165000, DiscountPercent = 0, Stock = 9, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600&q=80" },
        new() { Title = "Hoàng tử bé", Author = "Antoine de Saint-Exupéry", Category = "Sách thiếu nhi", Price = 69000, DiscountPercent = 15, Stock = 35, Rating = 4.9, CoverImage = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=80" },
        new() { Title = "Charlie và nhà máy sô-cô-la", Author = "Roald Dahl", Category = "Sách thiếu nhi", Price = 88000, DiscountPercent = 10, Stock = 21, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&q=80" },
        new() { Title = "Kính vạn hoa", Author = "Nguyễn Nhật Ánh", Category = "Truyện", Price = 75000, DiscountPercent = 0, Stock = 26, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=600&q=80" },
        new() { Title = "Thám tử lừng danh Conan 1", Author = "Gosho Aoyama", Category = "Truyện", Price = 25000, DiscountPercent = 5, Stock = 50, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1629992101753-56d196c8aabb?w=600&q=80" },
        new() { Title = "Doraemon - Nobita và vũ trụ", Author = "Fujiko F. Fujio", Category = "Truyện", Price = 30000, DiscountPercent = 0, Stock = 42, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&q=80" },
        new() { Title = "The Little Prince", Author = "Antoine de Saint-Exupéry", Category = "Sách Ngoại Ngữ", Price = 115000, DiscountPercent = 15, Stock = 17, Rating = 4.6, CoverImage = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600&q=80" },
        new() { Title = "Oxford Word Skills Basic", Author = "Ruth Gairns", Category = "Sách Ngoại Ngữ", Price = 210000, DiscountPercent = 10, Stock = 12, Rating = 4.5, CoverImage = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=80" },
        new() { Title = "Tự học tiếng Nhật N5", Author = "Minna no Nihongo", Category = "Sách Ngoại Ngữ", Price = 135000, DiscountPercent = 0, Stock = 15, Rating = 4.4, CoverImage = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&q=80" },
        new() { Title = "Tư duy nhanh và chậm", Author = "Daniel Kahneman", Category = "Kĩ năng & Kinh doanh", Price = 189000, DiscountPercent = 20, Stock = 16, Rating = 4.8, CoverImage = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=600&q=80" },
        new() { Title = "Những người khốn khổ", Author = "Victor Hugo", Category = "Văn học & Tiểu thuyết", Price = 175000, DiscountPercent = 10, Stock = 8, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&q=80" },
        new() { Title = "Thế giới động vật", Author = "DK Publishing", Category = "Sách khoa học", Price = 220000, DiscountPercent = 15, Stock = 13, Rating = 4.6, CoverImage = "https://images.unsplash.com/photo-1629992101753-56d196c8aabb?w=600&q=80" },
        new() { Title = "Truyện cổ tích Việt Nam", Author = "Nhiều tác giả", Category = "Truyện", Price = 68000, DiscountPercent = 10, Stock = 24, Rating = 4.5, CoverImage = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600&q=80" },
        new() { Title = "Bách khoa tri thức cho trẻ", Author = "Nhiều tác giả", Category = "Sách thiếu nhi", Price = 145000, DiscountPercent = 20, Stock = 18, Rating = 4.7, CoverImage = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=80" }
    ];

    public static readonly string[] Categories =
    [
        "Văn học & Tiểu thuyết", "Sách giáo khoa", "Kĩ năng & Kinh doanh",
        "Sách khoa học", "Sách thiếu nhi", "Truyện", "Sách Ngoại Ngữ"
    ];
}
