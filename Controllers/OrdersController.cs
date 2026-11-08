using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

[Authorize]
public class OrdersController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await db.Orders.AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();
        ViewBag.ReturnRequests = await db.ReturnRequests.AsNoTracking().Where(item => item.UserId == userId).ToListAsync();
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        ViewBag.Ratings = await db.BookReviews.Where(review => review.OrderId == id && review.UserId == userId)
            .ToDictionaryAsync(review => review.BookId, review => review.Rating);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cancellableStatuses = new[]
        {
            "Cần xác nhận",
            "Đã xác nhận đơn hàng",
            "Đã đóng gói - chờ vận chuyển"
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.Orders.AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (order is null)
            return NotFound();

        if (!cancellableStatuses.Contains(order.Status))
        {
            await transaction.RollbackAsync();
            TempData["Message"] = "Đơn hàng không thể hủy vì đã được chuyển sang vận chuyển hoặc đã kết thúc.";
            return RedirectToAction(nameof(Index));
        }

        var updated = await db.Orders
            .Where(item => item.Id == id && item.UserId == userId && cancellableStatuses.Contains(item.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "Đã hủy")
                .SetProperty(item => item.CancellationReason, "Khách hàng yêu cầu hủy đơn hàng."));
        if (updated != 1)
        {
            await transaction.RollbackAsync();
            TempData["Message"] = "Trạng thái đơn hàng vừa thay đổi; vui lòng tải lại trang.";
            return RedirectToAction(nameof(Index));
        }

        var bookIds = order.Items.Select(item => item.BookId).Distinct().ToList();
        var books = await db.Books
            .Where(book => bookIds.Contains(book.Id))
            .ToDictionaryAsync(book => book.Id);
        if (books.Count != bookIds.Count)
        {
            await transaction.RollbackAsync();
            TempData["Message"] = "Không thể hủy đơn vì một hoặc nhiều sách trong đơn không còn tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var item in order.Items)
            books[item.BookId].Stock += item.Quantity;

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Action = "Khách hàng đã hủy đơn hàng"
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Message"] = $"Đơn hàng #{order.Id:D8} đã được hủy.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int orderId, int bookId, int rating)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);
        if (order is null) return NotFound();
        if (order.Status != "Hoàn tất" || rating is < 1 or > 5 || !order.Items.Any(item => item.BookId == bookId))
            return BadRequest("Chỉ có thể đánh giá sách trong đơn đã hoàn tất với mức từ 1 đến 5 sao.");

        var review = await db.BookReviews.FirstOrDefaultAsync(item => item.OrderId == orderId && item.BookId == bookId && item.UserId == userId);
        if (review is null)
            db.BookReviews.Add(new BookReview { BookId = bookId, OrderId = orderId, UserId = userId, Rating = rating });
        else
            review.Rating = rating;

        await db.SaveChangesAsync();
        var average = await db.BookReviews.Where(item => item.BookId == bookId).AverageAsync(item => (double?)item.Rating) ?? rating;
        var reviewCount = await db.BookReviews.CountAsync(item => item.BookId == bookId);
        var book = await db.Books.FindAsync(bookId);
        if (book is not null)
        {
            book.Rating = average;
            book.ReviewCount = reviewCount;
        }
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã lưu đánh giá của bạn.";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }
}
