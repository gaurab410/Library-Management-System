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

          // GET: Branch/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(m => m.BranchId == id);

            if (branch == null) return NotFound();

            return View(branch);
        }

        // GET: Branch/Create
        public IActionResult Create()
        {
            var branch = new Branch
            {
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            return View(branch);
        }
    }
}
