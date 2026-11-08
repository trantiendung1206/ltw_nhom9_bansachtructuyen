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
        var book = await db.Books.FindAsync(bookId);
        if (book is not null) book.Rating = average;
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã lưu đánh giá của bạn.";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }
}
