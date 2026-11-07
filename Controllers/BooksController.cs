using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;
using System.Security.Claims;

namespace sc.Controllers;

public class BooksController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string[]? category, bool promotion = false, string? sort = "new")
    {
        IEnumerable<Book> books = await db.Books.AsNoTracking().Where(book => book.Title != "").ToListAsync();
        var selectedCategories = category?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct().ToArray() ?? [];
        if (!string.IsNullOrWhiteSpace(q))
            books = books.Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || b.Author.Contains(q, StringComparison.OrdinalIgnoreCase));
        if (selectedCategories.Length > 0)
            books = books.Where(b => selectedCategories.Contains(b.Category));
        if (promotion)
            books = books.Where(b => b.SalePrice < b.Price);
        books = sort switch
        {
            "price-asc" => books.OrderBy(b => b.SalePrice),
            "price-desc" => books.OrderByDescending(b => b.SalePrice),
            "rating" => books.OrderByDescending(b => b.Rating),
            _ => books.OrderByDescending(b => b.Id)
        };
        ViewBag.Query = q;
        ViewBag.Categories = selectedCategories;
        ViewBag.Sort = sort;
        ViewBag.Promotion = promotion;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.WishlistBookIds = await db.Wishlists.Where(item => item.UserId == userId).Select(item => item.BookId).ToListAsync();
        }
        return View(books.ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.Title != "");
        if (book is null) return NotFound();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsWishlisted = await db.Wishlists.AnyAsync(item => item.UserId == userId && item.BookId == id);
        }
        return View(book);
    }
}
