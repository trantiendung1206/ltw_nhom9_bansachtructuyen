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
        if (order is null || order.Status != "Hoàn tất" || order.CreatedAt < DateTime.Now.AddDays(-7)) return NotFound();
        return View(new ReturnRequest { OrderId = orderId, Order = order, RequestType = "Hoàn tiền" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnRequest model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.FirstOrDefaultAsync(item => item.Id == model.OrderId && item.UserId == userId);
        if (order is null || order.Status != "Hoàn tất" || order.CreatedAt < DateTime.Now.AddDays(-7)) return NotFound();
        if (await db.ReturnRequests.AnyAsync(item => item.OrderId == order.Id)) return Conflict("Đơn hàng đã có yêu cầu trả hàng.");
        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            ModelState.AddModelError(nameof(model.Reason), "Vui lòng nhập lý do.");
            model.Order = order;
            return View(model);
        }
        var user = await userManager.FindByIdAsync(userId);
        db.ReturnRequests.Add(new ReturnRequest { OrderId = order.Id, UserId = userId, CustomerEmail = user?.Email ?? "", CustomerPhone = user?.PhoneNumber ?? order.Phone, RequestType = "Hoàn tiền", Reason = model.Reason });
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã gửi yêu cầu trả hàng, vui lòng chờ nhân viên xác nhận.";
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
