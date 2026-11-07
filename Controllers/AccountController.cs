using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;

namespace sc.Controllers;

public sealed class AccountController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Identifier)
            ?? await userManager.Users.FirstOrDefaultAsync(item => item.PhoneNumber == model.Identifier);
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
        if (result.Succeeded)
        {
            var destination = user is not null && await userManager.IsInRoleAsync(user, "Admin")
                ? Url.Action("Index", "Admin")! : Url.Action("Index", "Home")!;
            return LocalRedirect(model.ReturnUrl ?? destination);
        }
        ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (await userManager.FindByEmailAsync(model.Email) is not null)
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
        if (await userManager.Users.AnyAsync(user => user.PhoneNumber == model.Phone))
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại này đã được sử dụng.");
        if (!ModelState.IsValid) return View(model);
        var user = new AppUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, PhoneNumber = model.Phone };
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Customer");
            await signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Home");
        }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}

public sealed class LoginViewModel
{
    [Required, Display(Name = "Email hoặc số điện thoại")] public string Identifier { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, Display(Name = "Họ và tên")] public string FullName { get; set; } = "";
    [Required, EmailAddress, Display(Name = "Email")] public string Email { get; set; } = "";
    [Required, Phone, Display(Name = "Số điện thoại")] public string Phone { get; set; } = "";
    [Required, MinLength(6), DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu")] public string ConfirmPassword { get; set; } = "";
}
