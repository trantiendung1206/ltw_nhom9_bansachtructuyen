using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

[Authorize]
public class WishlistController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await db.Wishlists.AsNoTracking().Include(item => item.Book)
            .Where(item => item.UserId == userId && item.Book.Title != "").OrderByDescending(item => item.AddedAt).ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, string? returnUrl = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.Wishlists.FindAsync(userId, bookId);
        if (item is null)
        {
            if (!await db.Books.AnyAsync(book => book.Id == bookId)) return NotFound();
            db.Wishlists.Add(new WishlistItem { UserId = userId, BookId = bookId });
            TempData["Message"] = "Đã thêm sách vào danh sách yêu thích.";
        }
        else
            TempData["Message"] = "Sách đã có trong danh sách yêu thích.";
        await db.SaveChangesAsync();
        return Redirect(returnUrl is not null && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index))!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.Wishlists.FindAsync(userId, bookId);
        if (item is not null)
        {
            db.Wishlists.Remove(item);
            await db.SaveChangesAsync();
        }
        TempData["Message"] = "Đã bỏ sách khỏi danh sách yêu thích.";
        return RedirectToAction(nameof(Index));
    }
}
