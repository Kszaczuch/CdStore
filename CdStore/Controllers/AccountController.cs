using CdStore.Models;
using CdStore.Services;
using CdStore.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CdStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly CartService _cartService;
        private const string CartCookieName = "CartId";

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, CartService cartService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._cartService = cartService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (user != null && Request.Cookies.TryGetValue(CartCookieName, out var cookieId) && !string.IsNullOrEmpty(cookieId))
                {
                    var anonItems = _cartService.GetCartItems(cookieId);
                    foreach (var albumId in anonItems)
                    {
                        _cartService.Add(user.Id, albumId);
                    }
                    _cartService.Clear(cookieId);
                    Response.Cookies.Delete(CartCookieName);
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if(result.Succeeded)
            {
                var roleExist = await roleManager.RoleExistsAsync("User");

                if(!roleExist)
                {
                    var role = new IdentityRole("User");
                    await roleManager.CreateAsync(role);
                }

                await userManager.AddToRoleAsync(user, "User");

                await signInManager.SignInAsync(user, isPersistent: false);

                if (Request.Cookies.TryGetValue(CartCookieName, out var cookieId) && !string.IsNullOrEmpty(cookieId))
                {
                    var anonItems = _cartService.GetCartItems(cookieId);
                    foreach (var albumId in anonItems)
                    {
                        _cartService.Add(user.Id, albumId);
                    }
                    _cartService.Clear(cookieId);
                    Response.Cookies.Delete(CartCookieName);
                }

                return RedirectToAction("Login", "Account");
            }

            foreach(var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
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
}
