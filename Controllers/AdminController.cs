using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss;
using sc.Data;
using sc.Models;
using System.Security.Claims;

namespace sc.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin")]
public class AdminController(
    AppDbContext db,
    IWebHostEnvironment environment,
    HtmlSanitizer sanitizer,
    ILogger<AdminController> logger) : Controller
{
    private const int BookPageSize = 15;
    private const long MaximumCoverLength = 5 * 1024 * 1024;

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
            .Where(item => db.Orders.Any(order => order.Id == item.OrderId && order.Status == "Hoàn tất"))
            .GroupBy(item => item.BookTitle)
            .OrderByDescending(group => group.Sum(item => item.Quantity))
            .Select(group => group.Key)
            .FirstOrDefaultAsync() ?? "Chưa có dữ liệu";
        stats.BestSellingCategory = await db.OrderItems
            .Where(item => db.Orders.Any(order => order.Id == item.OrderId && order.Status == "Hoàn tất"))
            .Join(db.Books, item => item.BookId, book => book.Id, (item, book) => new { book.Category, item.Quantity })
            .GroupBy(item => item.Category)
            .OrderByDescending(group => group.Sum(item => item.Quantity))
            .Select(group => group.Key)
            .FirstOrDefaultAsync() ?? "Chưa có dữ liệu";
        return View(stats);
    }

    [HttpGet("Books")]
    public async Task<IActionResult> Books(string? q, int page = 1)
    {
        var books = db.Books.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            books = books.Where(book => book.Title.Contains(q) || book.Id.ToString() == q);
        }

        var totalCount = await books.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)BookPageSize);
        page = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        return View(new AdminBookListViewModel
        {
            Books = await books.OrderByDescending(book => book.Id)
                .Skip((page - 1) * BookPageSize)
                .Take(BookPageSize)
                .ToListAsync(),
            Query = q,
            Page = page,
            PageSize = BookPageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("Books/Create")]
    public async Task<IActionResult> CreateBook()
    {
        await LoadBookCategoriesAsync();
        return View("BookForm", new BookEditViewModel());
    }

    [HttpPost("Books/Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBook(BookEditViewModel input)
    {
        input.Description = sanitizer.Sanitize(input.Description ?? "");
        ModelState.Remove(nameof(input.Description));
        await ValidateBookInputAsync(input);
        if (!ModelState.IsValid)
        {
            await LoadBookCategoriesAsync();
            return View("BookForm", input);
        }

        var uploadedCover = await SaveCoverUploadAsync(input.CoverUpload);
        var book = new Book();
        ApplyBookInput(input, book, uploadedCover ?? input.CoverImage);
        db.Books.Add(book);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            await RemoveUploadedCoverAfterFailureAsync(uploadedCover, exception);
            throw;
        }
        return RedirectToAction(nameof(Books));
    }

    [HttpGet("Books/Edit/{id}")]
    public async Task<IActionResult> EditBook(int id)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (book is null) return NotFound();
        await LoadBookCategoriesAsync();
        var input = ToBookInput(book);
        input.Description = sanitizer.Sanitize(input.Description ?? "");
        return View("BookForm", input);
    }

    [HttpPost("Books/Edit/{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBook(int id, BookEditViewModel input)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return NotFound();
        input.Id = id;
        input.Description = sanitizer.Sanitize(input.Description ?? "");
        ModelState.Remove(nameof(input.Description));
        await ValidateBookInputAsync(input);
        if (!ModelState.IsValid)
        {
            await LoadBookCategoriesAsync();
            return View("BookForm", input);
        }

        var oldCover = book.CoverImage;
        var uploadedCover = await SaveCoverUploadAsync(input.CoverUpload);
        ApplyBookInput(input, book, uploadedCover ?? input.CoverImage);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            await RemoveUploadedCoverAfterFailureAsync(uploadedCover, exception);
            throw;
        }
        if (uploadedCover is not null)
        {
            try
            {
                DeleteManagedCover(oldCover);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Could not remove replaced cover {CoverPath}", oldCover);
            }
        }
        return RedirectToAction(nameof(Books));
    }

    private async Task LoadBookCategoriesAsync() =>
        ViewBag.Categories = await db.Categories
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync();

    [HttpGet("Books/Details/{id}")]
    public async Task<IActionResult> BookDetails(int id)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (book is null) return NotFound();
        book.Description = sanitizer.Sanitize(book.Description ?? "");
        return View("BookDetails", book);
    }

    private async Task ValidateBookInputAsync(BookEditViewModel input)
    {
        var categoryExists = await db.Categories.AnyAsync(category =>
            category.IsActive && category.Name == input.Category);
        if (!categoryExists)
            ModelState.AddModelError(nameof(input.Category), "Vui lòng chọn danh mục đang hoạt động.");

        if (input.Description.Length > 20000)
            ModelState.AddModelError(nameof(input.Description), "Mô tả không được vượt quá 20.000 ký tự sau khi làm sạch.");

        if (input.CoverUpload is not null && input.CoverUpload.Length > MaximumCoverLength)
            ModelState.AddModelError(nameof(input.CoverUpload), "Ảnh bìa không được vượt quá 5 MB.");
        else if (input.CoverUpload is not null && !await HasValidImageSignatureAsync(input.CoverUpload))
            ModelState.AddModelError(nameof(input.CoverUpload), "Tệp tải lên phải là ảnh PNG, JPEG hoặc WebP hợp lệ.");
        else if (input.CoverUpload is null && !IsSafeCoverReference(input.CoverImage))
            ModelState.AddModelError(nameof(input.CoverImage), "Ảnh bìa phải là URL HTTP(S) hoặc đường dẫn nội bộ hợp lệ.");
    }

    private static bool IsSafeCoverReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return true;
        if (Uri.TryCreate(reference, UriKind.Absolute, out var uri))
            return uri.Scheme is "http" or "https";
        return reference.StartsWith('/') && !reference.StartsWith("//") &&
            !reference.Contains('\\') && !reference.Contains("..", StringComparison.Ordinal);
    }

    private static async Task<bool> HasValidImageSignatureAsync(IFormFile upload)
    {
        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        var header = new byte[12];
        await using var stream = upload.OpenReadStream();
        var read = await stream.ReadAsync(header);
        return extension switch
        {
            ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255,
            ".webp" => read >= 12 &&
                header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private async Task<string?> SaveCoverUploadAsync(IFormFile? upload)
    {
        if (upload is null) return null;
        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        var filename = $"{Guid.NewGuid():N}{extension}";
        var directory = Path.Combine(environment.WebRootPath, "uploads", "books");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, filename);
        await using var destination = System.IO.File.Create(path);
        await upload.CopyToAsync(destination);
        return $"/uploads/books/{filename}";
    }

    private async Task RemoveUploadedCoverAfterFailureAsync(string? cover, Exception exception)
    {
        logger.LogError(exception, "Could not save book changes; removing uploaded cover {CoverPath}", cover);
        if (cover is null) return;
        try
        {
            DeleteManagedCover(cover);
        }
        catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
        {
            logger.LogError(cleanupException, "Could not remove uploaded cover {CoverPath} after save failure", cover);
        }
    }

    private void DeleteManagedCover(string? cover)
    {
        if (string.IsNullOrWhiteSpace(cover) || !cover.StartsWith("/uploads/books/", StringComparison.Ordinal))
            return;
        var filename = Path.GetFileName(cover);
        var path = Path.Combine(environment.WebRootPath, "uploads", "books", filename);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    private static void ApplyBookInput(BookEditViewModel input, Book book, string coverImage)
    {
        book.Title = input.Title.Trim();
        book.Author = input.Author.Trim();
        book.Category = input.Category;
        book.ISBN = input.ISBN.Trim();
        book.Price = input.Price;
        book.DiscountPercent = input.DiscountPercent;
        book.DiscountAmount = input.DiscountAmount;
        book.Stock = input.Stock;
        book.CoverImage = coverImage ?? "";
        book.Description = input.Description;
    }

    private static BookEditViewModel ToBookInput(Book book) => new()
    {
        Title = book.Title,
        Id = book.Id,
        Author = book.Author,
        Category = book.Category,
        ISBN = book.ISBN,
        Price = book.Price,
        DiscountPercent = book.DiscountPercent,
        DiscountAmount = book.DiscountAmount,
        Stock = book.Stock,
        CoverImage = book.CoverImage,
        Description = book.Description
    };

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
        if (!ModelState.IsValid ||
            model.DiscountPercent < 0 || model.DiscountPercent > 100 || model.DiscountAmount < 0)
        {
            TempData["Message"] = "Giá trị giảm không hợp lệ hoặc vượt quá giới hạn cho phép.";
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
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return RedirectToAction(nameof(Books));

        var hasHistory = await db.OrderItems.AnyAsync(item => item.BookId == id) ||
            await db.BookReviews.AnyAsync(review => review.BookId == id);
        if (hasHistory)
        {
            TempData["Message"] = "Không thể xóa sách đã có trong đơn hàng hoặc đánh giá.";
            return RedirectToAction(nameof(Books));
        }

        var cover = book.CoverImage;
        db.Books.Remove(book);
        await db.SaveChangesAsync();
        try
        {
            DeleteManagedCover(cover);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Book {BookId} was deleted but its cover could not be removed", id);
        }
        return RedirectToAction(nameof(Books));
    }

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
            if (step.Status == "Hoàn tất")
                order.CompletedAt = DateTime.Now;
            db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = id, Action = step.Action });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [HttpPost("Orders/{id}/Cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
        {
            TempData["Message"] = "Lý do hủy phải có từ 1 đến 1.000 ký tự.";
            return RedirectToAction(nameof(Orders));
        }

        var order = await db.Orders.AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (order is null) return NotFound();
        if (order.Status is "Hoàn tất" or "Đã trả hàng" or "Đã hủy")
        {
            TempData["Message"] = "Đơn hàng không thể hủy ở trạng thái hiện tại hoặc thiếu lý do hủy.";
            return RedirectToAction(nameof(Orders));
        }

        var bookIds = order.Items.Select(item => item.BookId).Distinct().ToList();
        var books = await db.Books.Where(book => bookIds.Contains(book.Id)).ToDictionaryAsync(book => book.Id);
        if (books.Count != bookIds.Count)
        {
            TempData["Message"] = "Không thể hủy đơn vì một hoặc nhiều sách trong đơn không còn tồn tại.";
            return RedirectToAction(nameof(Orders));
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var updated = await db.Orders
            .Where(item => item.Id == id && item.Status == order.Status &&
                item.Status != "Hoàn tất" && item.Status != "Đã trả hàng" && item.Status != "Đã hủy")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "Đã hủy")
                .SetProperty(item => item.CancellationReason, reason.Trim()));
        if (updated != 1)
        {
            await transaction.RollbackAsync();
            TempData["Message"] = "Trạng thái đơn hàng vừa thay đổi; không thể hủy đơn.";
            return RedirectToAction(nameof(Orders));
        }

        foreach (var item in order.Items)
            books[item.BookId].Stock += item.Quantity;
        db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = id, Action = $"Đã hủy đơn hàng: {reason.Trim()}" });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
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
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
        {
            TempData["Message"] = "Vui lòng nhập lý do từ 1 đến 1.000 ký tự.";
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
        if (status is not ("Chưa xử lý" or "Đang xử lý" or "Đã phản hồi"))
            return BadRequest("Trạng thái góp ý không hợp lệ.");
        var feedback = await db.CustomerFeedbacks.FindAsync(id);
        if (feedback is not null) { feedback.Status = status; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Feedback));
    }
}
