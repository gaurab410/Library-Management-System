using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BranchController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public BranchController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Branch/Index
        public async Task<IActionResult> Index(string? search, bool? activeOnly)
        {
            var query = _context.Branches
                .Include(b => b.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(term) ||
                                         b.Address.ToLower().Contains(term) ||
                                         (b.Email != null && b.Email.ToLower().Contains(term)) ||
                                         (b.Phone != null && b.Phone.ToLower().Contains(term)));
            }

            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(b => b.IsActive);
            }

            ViewBag.Search = search;
            ViewBag.ActiveOnly = activeOnly ?? false;

            var branches = await query.OrderBy(b => b.Name).ToListAsync();
            return View(branches);
        }

        
    }
}
