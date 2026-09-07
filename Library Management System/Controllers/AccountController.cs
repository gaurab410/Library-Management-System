using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? role = null;
            string userName = model.Email;

            // Simple demo account verification as specified in requirements
            if (model.Email.Equals("admin@library.com", StringComparison.OrdinalIgnoreCase) && model.Password == "Admin123!")
            {
                role = "Admin";
                userName = "Administrator";
            }
            else if (model.Email.Equals("reception@library.com", StringComparison.OrdinalIgnoreCase) && model.Password == "Reception123!")
            {
                role = "Reception";
                userName = "Front Desk Receptionist";
            }
            else if (model.Email.Equals("manager@library.com", StringComparison.OrdinalIgnoreCase) && model.Password == "Manager123!")
            {
                role = "Manager";
                userName = "Library Manager";
            }

            if (role != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.Role, role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                // Redirect based on role
                return role switch
                {
                    "Admin" => RedirectToAction("Index", "Admin"),
                    "Reception" => RedirectToAction("Index", "Reception"),
                    "Manager" => RedirectToAction("Index", "Manager"),
                    _ => RedirectToAction("Index", "Public")
                };
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password. Please use one of the demo credentials provided below.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Public");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
