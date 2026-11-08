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
        if (order is null || order.Status != "Hoàn tất" || (order.CompletedAt ?? order.CreatedAt) < DateTime.Now.AddDays(-7)) return NotFound();
        return View(new ReturnRequestInput { OrderId = orderId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnRequestInput model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.FirstOrDefaultAsync(item => item.Id == model.OrderId && item.UserId == userId);
        if (order is null || order.Status != "Hoàn tất" || (order.CompletedAt ?? order.CreatedAt) < DateTime.Now.AddDays(-7)) return NotFound();
        if (await db.ReturnRequests.AnyAsync(item => item.OrderId == order.Id && item.Status != "Từ chối yêu cầu trả hàng"))
            return Conflict("Đơn hàng đã có yêu cầu trả hàng đang xử lý hoặc đã được chấp nhận.");
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await userManager.FindByIdAsync(userId);
        db.ReturnRequests.Add(new ReturnRequest { OrderId = order.Id, UserId = userId, CustomerEmail = user?.Email ?? "", CustomerPhone = user?.PhoneNumber ?? order.Phone, RequestType = "Hoàn tiền", Reason = model.Reason.Trim() });
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã gửi yêu cầu trả hàng, vui lòng chờ nhân viên xác nhận.";
        return RedirectToAction("Index", "Orders");
    }

    [HttpGet]
    public IActionResult Feedback() => View(new CustomerFeedbackInput());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(CustomerFeedbackInput model)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = (await userManager.FindByIdAsync(userId))?.Email ?? "";
        db.CustomerFeedbacks.Add(new CustomerFeedback
        {
            UserId = userId,
            CustomerEmail = email,
            Subject = model.Subject.Trim(),
            Message = model.Message.Trim()
        });
        await db.SaveChangesAsync();
        TempData["Message"] = "Cảm ơn góp ý của bạn. GroupNine sẽ phản hồi sớm.";
        return RedirectToAction("Index", "Home");
    }
}
