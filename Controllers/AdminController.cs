using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;
using System.Security.Claims;

namespace sc.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin")]
public class AdminController(AppDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var stats = new AdminStatisticsViewModel
        {
            TotalBooks = await db.Books.CountAsync(),
            TotalStock = await db.Books.SumAsync(book => book.Stock),
            TotalCategories = await db.Categories.CountAsync(category => category.IsActive),
            TotalOrders = await db.Orders.CountAsync(),
            Revenue = await db.Orders.Where(order => order.Status == "Hoàn tất")
                .SumAsync(order => (decimal?)(order.Total - order.RefundedAmount)) ?? 0,
            FeaturedBooks = await db.Books.OrderByDescending(book => book.Rating).Take(5).AsNoTracking().ToListAsync()
        };
        stats.BestSellingBook = await db.OrderItems
            .GroupBy(item => item.BookTitle)
            .OrderByDescending(group => group.Sum(item => item.Quantity))
            .Select(group => group.Key)
            .FirstOrDefaultAsync() ?? "Chưa có dữ liệu";
        stats.BestSellingCategory = await db.OrderItems
            .Join(db.Books, item => item.BookId, book => book.Id, (item, book) => new { book.Category, item.Quantity })
            .GroupBy(item => item.Category)
            .OrderByDescending(group => group.Sum(item => item.Quantity))
            .Select(group => group.Key)
            .FirstOrDefaultAsync() ?? "Chưa có dữ liệu";
        return View(stats);
    }

    [HttpGet("Books")]
    public async Task<IActionResult> Books(string? q) =>
        View(await db.Books.Where(book => string.IsNullOrEmpty(q) || book.Title.Contains(q) || book.Id.ToString() == q).OrderByDescending(book => book.Id).ToListAsync());

    [HttpGet("Books/Create")]
    public async Task<IActionResult> CreateBook() { ViewBag.Categories = await db.Categories.Where(c => c.IsActive).ToListAsync(); return View("BookForm", new Book()); }

    [HttpPost("Books/Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBook(Book book)
    {
        AllowOptionalBookFields();
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Dữ liệu sách chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu.";
            await LoadBookCategoriesAsync();
            return View("BookForm", book);
        }

        NormalizeBook(book);
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Books));
    }

    [HttpGet("Books/Edit/{id}")]
    public async Task<IActionResult> EditBook(int id) { var book = await db.Books.FindAsync(id); if (book is null) return NotFound(); ViewBag.Categories = await db.Categories.Where(c => c.IsActive).ToListAsync(); return View("BookForm", book); }

    [HttpPost("Books/Edit/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBook(int id, Book input)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return NotFound();
        AllowOptionalBookFields();
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Dữ liệu sách chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu.";
            input.Id = id;
            await LoadBookCategoriesAsync();
            return View("BookForm", input);
        }

        NormalizeBook(input);
        book.Title = input.Title;
        book.Author = input.Author;
        book.Category = input.Category;
        book.Price = input.Price;
        book.DiscountPercent = input.DiscountPercent;
        book.DiscountAmount = input.DiscountAmount;
        book.Stock = input.Stock;
        book.CoverImage = input.CoverImage;
        book.Description = input.Description;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Books));
    }

    private async Task LoadBookCategoriesAsync() =>
        ViewBag.Categories = await db.Categories
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync();

    private void AllowOptionalBookFields()
    {
        ModelState.Remove(nameof(Book.CoverImage));
        ModelState.Remove(nameof(Book.Description));
    }

    private static void NormalizeBook(Book book)
    {
        book.Title ??= "";
        book.Author ??= "";
        book.Category ??= "";
        book.CoverImage ??= "";
        book.Description ??= "";
    }

    [HttpGet("Promotions")]
    public async Task<IActionResult> Promotions(string? q, string? category)
    {
        var books = db.Books.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            books = books.Where(book => book.Title.Contains(q) || book.Id.ToString() == q);
        if (!string.IsNullOrWhiteSpace(category))
            books = books.Where(book => book.Category == category);
        ViewBag.Categories = await db.Categories.Where(item => item.IsActive).OrderBy(item => item.Name).ToListAsync();
        return View(new PromotionViewModel { Query = q, Category = category, Books = await books.OrderBy(book => book.Title).ToListAsync() });
    }

    [HttpPost("Promotions/Apply"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPromotion(PromotionViewModel model)
    {
        if (model.DiscountPercent < 0 || model.DiscountPercent > 100 || model.DiscountAmount < 0)
        {
            TempData["Message"] = "Giá trị giảm không hợp lệ.";
            return RedirectToAction(nameof(Promotions), new { q = model.Query, category = model.Category });
        }
        var books = db.Books.AsQueryable();
        if (!string.IsNullOrWhiteSpace(model.Query))
            books = books.Where(book => book.Title.Contains(model.Query) || book.Id.ToString() == model.Query);
        if (!string.IsNullOrWhiteSpace(model.Category))
            books = books.Where(book => book.Category == model.Category);
        await books.ExecuteUpdateAsync(setters => setters
            .SetProperty(book => book.DiscountPercent, model.DiscountAmount > 0 ? 0 : model.DiscountPercent)
            .SetProperty(book => book.DiscountAmount, model.DiscountAmount));
        TempData["Message"] = $"Đã áp dụng ưu đãi cho {await books.CountAsync()} sách.";
        return RedirectToAction(nameof(Promotions), new { q = model.Query, category = model.Category });
    }

    [HttpPost("Promotions/Clear/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearPromotion(int id)
    {
        await db.Books.Where(book => book.Id == id).ExecuteUpdateAsync(setters => setters.SetProperty(book => book.DiscountPercent, 0).SetProperty(book => book.DiscountAmount, 0));
        return RedirectToAction(nameof(Promotions));
    }

    [HttpPost("Books/Delete/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBook(int id) { var book = await db.Books.FindAsync(id); if (book is not null) { db.Books.Remove(book); await db.SaveChangesAsync(); } return RedirectToAction(nameof(Books)); }

    [HttpPost("Books/Toggle/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id) { var book = await db.Books.FindAsync(id); if (book is not null) { book.Stock = book.Stock == 0 ? 10 : 0; await db.SaveChangesAsync(); } return RedirectToAction(nameof(Books)); }

    [HttpGet("Categories")]
    public async Task<IActionResult> Categories()
    {
        var categories = await db.Categories.Where(c => StoreData.Categories.Contains(c.Name)).OrderBy(c => c.Id).ToListAsync();
        ViewBag.BookCounts = await db.Books.GroupBy(book => book.Category)
            .Select(group => new { Category = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Category, item => item.Count);
        return View(categories);
    }

    [HttpGet("Categories/{name}")]
    public async Task<IActionResult> CategoryBooks(string name)
    {
        if (!StoreData.Categories.Contains(name)) return NotFound();
        ViewBag.CategoryName = name;
        ViewBag.SelectedBookIds = await db.Books.Where(book => book.Category == name).Select(book => book.Id).ToListAsync();
        return View(await db.Books.Where(book => book.Title != "").OrderBy(book => book.Title).ToListAsync());
    }

    [HttpPost("Categories/{name}/Books"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategoryBooks(string name, int[]? selectedBookIds)
    {
        if (!StoreData.Categories.Contains(name)) return NotFound();
        var selected = selectedBookIds?.ToHashSet() ?? [];
        await db.Books.Where(book => book.Category == name)
            .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.Category, ""));
        if (selected.Count > 0)
            await db.Books.Where(book => selected.Contains(book.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.Category, name));
        TempData["Message"] = $"Đã cập nhật sách trong danh mục “{name}”.";
        return RedirectToAction(nameof(CategoryBooks), new { name });
    }

    [HttpGet("Orders")]
    public async Task<IActionResult> Orders(string? q, string? status)
    {
        await db.Orders
            .Where(order => order.Status != "Đã trả hàng" &&
                db.ReturnRequests.Any(request => request.OrderId == order.Id && request.Status == "Chấp nhận yêu cầu trả hàng"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.Status, "Đã trả hàng"));

        var orders = db.Orders.Include(order => order.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            orders = orders.Where(order => order.Status == status);

        var search = q?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var orderCode = search.TrimStart('#');
            if (int.TryParse(orderCode, out var orderId))
                orders = orders.Where(order => order.Id == orderId || order.ReceiverName.Contains(search) || order.Phone.Contains(search) || order.Items.Any(item => item.BookTitle.Contains(search)));
            else
                orders = orders.Where(order => order.ReceiverName.Contains(search) || order.Phone.Contains(search) || order.Items.Any(item => item.BookTitle.Contains(search)));
        }
        ViewBag.Query = q;
        ViewBag.Status = status;
        return View(await orders.OrderByDescending(order => order.CreatedAt).ToListAsync());
    }

    [HttpGet("Orders/{id}")]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id);
        if (order is null) return NotFound();
        ViewBag.History = await db.OrderStatusHistories.Where(item => item.OrderId == id).OrderBy(item => item.OccurredAt).ToListAsync();
        return View(order);
    }

    [HttpPost("Orders/{id}/Advance"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceOrder(int id)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        var next = new Dictionary<string, (string Status, string Action)>
        {
            ["Cần xác nhận"] = ("Đã xác nhận đơn hàng", "Đã xác nhận đơn hàng"),
            ["Đã xác nhận đơn hàng"] = ("Đã đóng gói - chờ vận chuyển", "Đã đóng gói thành công - chờ vận chuyển"),
            ["Đã đóng gói - chờ vận chuyển"] = ("Đang vận chuyển", "Đang vận chuyển"),
            ["Đang vận chuyển"] = ("Hoàn tất", "Hoàn tất đơn hàng")
        };
        if (next.TryGetValue(order.Status, out var step))
        {
            order.Status = step.Status;
            db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = id, Action = step.Action });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [HttpPost("Orders/{id}/Cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id, string reason)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        if (order.Status != "Hoàn tất" && order.Status != "Đã trả hàng" && !string.IsNullOrWhiteSpace(reason))
        {
            order.Status = "Đã hủy";
            order.CancellationReason = reason;
            db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = id, Action = $"Đã hủy đơn hàng: {reason}" });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Orders));
    }

    [HttpGet("Returns")]
    public async Task<IActionResult> Returns(string? q)
    {
        var requests = db.ReturnRequests
            .Include(item => item.Order)
            .ThenInclude(order => order!.Items)
            .AsQueryable();
        var search = q?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var code = search.TrimStart('#');
            requests = int.TryParse(code, out var orderId)
                ? requests.Where(item => item.OrderId == orderId)
                : requests.Where(item => false);
        }
        ViewBag.Query = q;
        return View(await requests.OrderByDescending(item => item.CreatedAt).ToListAsync());
    }

    [HttpPost("Returns/Accept/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptReturn(int id, string? q)
    {
        var request = await db.ReturnRequests
            .Include(item => item.Order)
            .ThenInclude(order => order!.Items)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (request is not null && request.Status == "Chờ xác nhận" && request.Order is not null)
        {
            var bookIds = request.Order.Items.Select(item => item.BookId).Distinct().ToList();
            var books = await db.Books.Where(book => bookIds.Contains(book.Id)).ToDictionaryAsync(book => book.Id);
            if (books.Count != bookIds.Count)
            {
                TempData["Message"] = "Không thể hoàn hàng vì một hoặc nhiều sách trong đơn không còn tồn tại.";
                return RedirectToAction(nameof(Returns), new { q });
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            foreach (var item in request.Order.Items)
                books[item.BookId].Stock += item.Quantity;
            request.Order.RefundedAmount = request.Order.Total;
            request.Order.Status = "Đã trả hàng";
            request.Status = "Chấp nhận yêu cầu trả hàng";
            request.RejectionReason = "";
            request.ResolvedAt = DateTime.Now;
            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = request.Order.Id,
                Action = "Hàng đã trả về"
            });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Message"] = "Đã chấp nhận yêu cầu trả hàng, hoàn tiền và cộng lại tồn kho.";
        }
        return RedirectToAction(nameof(Returns), new { q });
    }

    [HttpPost("Returns/Reject/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectReturn(int id, string? reason, string? q)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Message"] = "Vui lòng nêu lý do không chấp nhận yêu cầu trả hàng.";
            return RedirectToAction(nameof(Returns), new { q });
        }

        var request = await db.ReturnRequests.FindAsync(id);
        if (request is not null && request.Status == "Chờ xác nhận")
        {
            request.Status = "Từ chối yêu cầu trả hàng";
            request.RejectionReason = reason.Trim();
            request.ResolvedAt = DateTime.Now;
            await db.SaveChangesAsync();
            TempData["Message"] = "Đã từ chối yêu cầu trả hàng.";
        }
        return RedirectToAction(nameof(Returns), new { q });
    }

    [HttpGet("Feedback")]
    public async Task<IActionResult> Feedback() => View(await db.CustomerFeedbacks.OrderByDescending(item => item.CreatedAt).ToListAsync());

    [HttpPost("Feedback/Status/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFeedbackStatus(int id, string status)
    {
        var feedback = await db.CustomerFeedbacks.FindAsync(id);
        if (feedback is not null) { feedback.Status = status; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Feedback));
    }
}
