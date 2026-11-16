using AutoMapper;
using AutoMapper.QueryableExtensions; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationShop.Data;
using WebApplicationShop.Dto;

namespace WebApplicationShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public UsersController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(string? role)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role != null && u.Role.Name == role);
            List<UserDto> users = await query.OrderBy(u => u.Id).ProjectTo<UserDto>(_mapper.ConfigurationProvider).ToListAsync();
            ViewBag.Role = role;
            ViewBag.Roles = await _context.Roles.Select(r => r.Name).ToListAsync();
            return View(users);
        }
    }
}