using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

[Authorize]
public class SupportController(AppDbContext db, UserManager<AppUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Return(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);
        return order is null ? NotFound() : View(new ReturnRequest { OrderId = orderId, Order = order });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnRequest model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.FirstOrDefaultAsync(item => item.Id == model.OrderId && item.UserId == userId);
        if (order is null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            ModelState.AddModelError(nameof(model.Reason), "Vui lòng nhập lý do.");
            model.Order = order;
            return View(model);
        }
        var user = await userManager.FindByIdAsync(userId);
        db.ReturnRequests.Add(new ReturnRequest { OrderId = order.Id, UserId = userId, CustomerEmail = user?.Email ?? "", CustomerPhone = user?.PhoneNumber ?? order.Phone, RequestType = model.RequestType, Reason = model.Reason });
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã gửi yêu cầu đổi trả, vui lòng chờ quản lý xác nhận.";
        return RedirectToAction("Index", "Orders");
    }

    [HttpGet]
    public IActionResult Feedback() => View(new CustomerFeedback());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(CustomerFeedback model)
    {
        if (!ModelState.IsValid) return View(model);
        model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        model.CustomerEmail = (await userManager.FindByIdAsync(model.UserId))?.Email ?? "";
        db.CustomerFeedbacks.Add(model);
        await db.SaveChangesAsync();
        TempData["Message"] = "Cảm ơn góp ý của bạn. Lật Trang sẽ phản hồi sớm.";
        return RedirectToAction("Index", "Home");
    }
}
