using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationShop.Data;
using WebApplicationShop.Dto;
using WebApplicationShop.Models;

namespace WebApplicationShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();
        public AccountController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // вход
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == model.Email);
            // проверка хэша
            if (user == null ||
                _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Неверный email или пароль");
                return View(model);
            }
            await Authenticate(user);
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Products");
        }
        // регистрация (для роли user)
        [HttpGet]
        [AllowAnonymous]    
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);
            var user = _mapper.Map<User>(dto);
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Пользователь с таким email уже есть");
                return View(dto);
            }
            // новый пользователь всегда получает роль user
            var userRole = await _context.Roles.FirstAsync(r => r.Name == "User");
            user.RoleId = userRole.Id;
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            user.Role = userRole;
            await Authenticate(user); // сразу входим после регистрации
            return RedirectToAction("Index", "Products");
        }
        // logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Products");
        }

        // запрет доступа
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // профиль пользователя
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == User.Identity!.Name);
            if (user == null)
                return RedirectToAction("Login");
            var dto = _mapper.Map<UserDto>(user);
            return View(dto);
        }

        // метод аутентификации
        private async Task Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name ?? "User")
            };
            var scheme = CookieAuthenticationDefaults.AuthenticationScheme;
            var identity = new ClaimsIdentity(claims, scheme);
            await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(identity));
        }
    }
}