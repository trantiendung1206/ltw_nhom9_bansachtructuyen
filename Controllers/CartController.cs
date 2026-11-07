using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

[Authorize]
public class CartController(AppDbContext db) : Controller
{
    private const string CartKey = "bookstore-cart";

    public IActionResult Index() => View(GetCart());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int quantity = 1, string? returnUrl = null)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (book is null || quantity < 1 || book.Stock < 1) return NotFound();
        var cart = GetCart();
        var item = cart.FirstOrDefault(entry => entry.Book.Id == id);
        if (item is null) cart.Add(new CartItem { Book = book, Quantity = Math.Min(quantity, book.Stock) });
        else item.Quantity = Math.Min(item.Quantity + quantity, book.Stock);
        SaveCart(cart);
        TempData["Message"] = $"Đã thêm “{book.Title}” vào giỏ hàng.";
        return Redirect(returnUrl is not null && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index))!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int id, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(entry => entry.Book.Id == id);
        if (item is not null)
        {
            item.Quantity = Math.Min(Math.Max(quantity, 1), item.Book.Stock);
            SaveCart(cart);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        var cart = GetCart();
        cart.RemoveAll(item => item.Book.Id == id);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    public List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartKey);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    private void SaveCart(List<CartItem> cart) =>
        HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
}
