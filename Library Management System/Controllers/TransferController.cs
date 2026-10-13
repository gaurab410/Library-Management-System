using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Reception")]
    public class TransferController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public TransferController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Transfer/Index
        public async Task<IActionResult> Index(int? branchId, TransferStatus? statusFilter)
        {
            var query = _context.BranchTransfers
                .Include(t => t.LibraryItem)
                    .ThenInclude(i => i.Branch)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(t => t.FromBranchId == branchId.Value || t.ToBranchId == branchId.Value);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.Status == statusFilter.Value);
            }

            var branches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Branches = branches;
            ViewBag.SelectedBranchId = branchId;
            ViewBag.SelectedStatus = statusFilter;

            var transfers = await query.OrderByDescending(t => t.RequestedAt).ToListAsync();
            return View(transfers);
        }

    }
}
