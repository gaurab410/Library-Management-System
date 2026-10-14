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

        // GET: Transfer/Create
        public async Task<IActionResult> Create(int? libraryItemId)
        {
            var items = await _context.LibraryItems
                .Include(i => i.Branch)
                .Where(i => i.Status != ItemStatus.Destroy)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.ItemsList = items;
            ViewBag.BranchesList = new SelectList(branches, "BranchId", "Name");

            var transfer = new BranchTransfer
            {
                LibraryItemId = libraryItemId ?? 0
            };

            if (libraryItemId.HasValue)
            {
                var selectedItem = items.FirstOrDefault(i => i.Id == libraryItemId.Value);
                if (selectedItem != null)
                {
                    transfer.FromBranchId = selectedItem.BranchId;
                }
            }

            return View(transfer);
        }

        // POST: Transfer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchTransfer transfer)
        {
            var item = await _context.LibraryItems
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i => i.Id == transfer.LibraryItemId);

            if (item == null)
            {
                ModelState.AddModelError("LibraryItemId", "Please select a valid library item.");
            }
            else
            {
                // Ensure origin branch matches the item's current branch location
                transfer.FromBranchId = item.BranchId;
                ModelState.Remove(nameof(transfer.FromBranchId));
            }

            ModelState.Remove(nameof(transfer.RequestedByRole));
            ModelState.Remove(nameof(transfer.LibraryItem));
            ModelState.Remove(nameof(transfer.FromBranch));
            ModelState.Remove(nameof(transfer.ToBranch));

            if (transfer.ToBranchId <= 0)
            {
                ModelState.AddModelError("ToBranchId", "Destination branch is required.");
            }
            else if (item != null && transfer.ToBranchId == item.BranchId)
            {
                ModelState.AddModelError("ToBranchId", "Destination branch must be different from current branch location.");
            }

            if (ModelState.IsValid && item != null)
            {
                transfer.RequestedAt = DateTime.Now;
                transfer.Status = TransferStatus.Requested;
                transfer.RequestedByRole = User.IsInRole("Admin") ? "Admin" : "Reception";

                _context.BranchTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Transfer request created for item '{item.Name}' ({item.LibraryCode}) to destination branch.";
                return RedirectToAction(nameof(Index));
            }

            var items = await _context.LibraryItems
                .Include(i => i.Branch)
                .Where(i => i.Status != ItemStatus.Destroy)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.ItemsList = items;
            ViewBag.BranchesList = new SelectList(branches, "BranchId", "Name", transfer.ToBranchId);

            return View(transfer);
        }

        // POST: Transfer/Receive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id)
        {
            var transfer = await _context.BranchTransfers
                .Include(t => t.LibraryItem)
                .Include(t => t.ToBranch)
                .FirstOrDefaultAsync(t => t.BranchTransferId == id);

            if (transfer == null) return NotFound();

            if (transfer.Status == TransferStatus.Received)
            {
                TempData["ErrorMessage"] = "This transfer has already been marked as received.";
                return RedirectToAction(nameof(Index));
            }

            if (transfer.Status == TransferStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot receive a transfer that has been cancelled.";
                return RedirectToAction(nameof(Index));
            }

            // Update item.BranchId = transfer.ToBranchId, set transfer.Status = Received, set transfer.CompletedAt = DateTime.Now
            transfer.LibraryItem.BranchId = transfer.ToBranchId;
            transfer.Status = TransferStatus.Received;
            transfer.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Item '{transfer.LibraryItem.Name}' ({transfer.LibraryItem.LibraryCode}) successfully marked as received at '{transfer.ToBranch.Name}'. Item location updated!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Transfer/MarkInTransit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInTransit(int id)
        {
            var transfer = await _context.BranchTransfers
                .Include(t => t.LibraryItem)
                .FirstOrDefaultAsync(t => t.BranchTransferId == id);

            if (transfer == null) return NotFound();

            if (transfer.Status != TransferStatus.Requested)
            {
                TempData["ErrorMessage"] = "Only pending requested transfers can be marked in transit.";
                return RedirectToAction(nameof(Index));
            }

            transfer.Status = TransferStatus.InTransit;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Transfer #{transfer.BranchTransferId} for '{transfer.LibraryItem.Name}' is now in transit.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Transfer/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var transfer = await _context.BranchTransfers
                .Include(t => t.LibraryItem)
                .FirstOrDefaultAsync(t => t.BranchTransferId == id);

            if (transfer == null) return NotFound();

            if (transfer.Status == TransferStatus.Received)
            {
                TempData["ErrorMessage"] = "Cannot cancel an already completed transfer.";
                return RedirectToAction(nameof(Index));
            }

            transfer.Status = TransferStatus.Cancelled;
            transfer.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Transfer #{transfer.BranchTransferId} for '{transfer.LibraryItem.Name}' has been cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
