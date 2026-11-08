using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var books = await db.Books.AsNoTracking().Where(book => book.Title != "").ToListAsync();
        ViewBag.Books = books;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cartJson = HttpContext.Session.GetString("bookstore-cart");
        var cartItems = string.IsNullOrEmpty(cartJson)
            ? []
            : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? [];
        var interactedBookIds = cartItems.Select(item => item.Book.Id).ToHashSet();
        var preferredCategories = cartItems
            .Select(item => item.Book.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .ToHashSet();

        if (userId is not null)
        {
            var orderedBooks = await db.OrderItems
                .Where(item => db.Orders.Any(order =>
                    order.Id == item.OrderId &&
                    order.UserId == userId &&
                    order.Status != "Đã hủy"))
                .Join(db.Books, item => item.BookId, book => book.Id, (_, book) => new
                {
                    book.Id,
                    book.Category
                })
                .ToListAsync();

            interactedBookIds.UnionWith(orderedBooks.Select(book => book.Id));
            preferredCategories.UnionWith(orderedBooks.Select(book => book.Category));
        }

        var recommendedBooks = preferredCategories.Count == 0
            ? books
            : books.Where(book =>
                preferredCategories.Contains(book.Category) &&
                !interactedBookIds.Contains(book.Id)).ToList();

        if (recommendedBooks.Count == 0 && preferredCategories.Count > 0)
            recommendedBooks = books.Where(book => preferredCategories.Contains(book.Category)).ToList();

        ViewBag.FeaturedBooks = recommendedBooks
            .OrderBy(_ => Random.Shared.Next())
            .Take(4)
            .ToList();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
