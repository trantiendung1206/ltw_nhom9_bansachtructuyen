using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sc.Data;
using sc.Models;
using System.Security.Claims;

namespace sc.Controllers;

[Authorize]
public class CheckoutController(AppDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var items = GetCart();
        if (items.Count == 0) return RedirectToAction("Index", "Cart");
        return View(new CheckoutViewModel { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        model.Items = GetCart();
        if (model.Items.Count == 0) return RedirectToAction("Index", "Cart");
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.ReceiverName) || string.IsNullOrWhiteSpace(model.Phone) || string.IsNullOrWhiteSpace(model.Address))
            return View("Index", model);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = new Order
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            ReceiverName = model.ReceiverName,
            Phone = model.Phone,
            Address = model.Address,
            DeliveryMethod = model.DeliveryMethod,
            Total = model.Subtotal,
            Status = "Cần xác nhận"
        };
        foreach (var item in model.Items)
        {
            var book = await db.Books.FindAsync(item.Book.Id);
            if (book is null || book.Stock < item.Quantity)
            {
                ModelState.AddModelError("", $"Sách “{item.Book.Title}” không đủ tồn kho.");
                return View("Index", model);
            }
            book.Stock -= item.Quantity;
            order.Items.Add(new OrderItem { BookId = book.Id, BookTitle = book.Title, Quantity = item.Quantity, UnitPrice = item.Book.SalePrice });
        }
        order.Total = model.Subtotal;
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            OccurredAt = order.CreatedAt,
            Action = "Đã đặt hàng"
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        HttpContext.Session.Remove("bookstore-cart");
        ViewBag.OrderCode = $"LP{DateTime.Now:MMddHHmmss}";
        return View("Success");
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString("bookstore-cart");
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }
}
