using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Reception,Manager")]
    public class ReservationController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public ReservationController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Reservation/Index
        public async Task<IActionResult> Index(int? itemId, int? borrowerId, ReservationStatus? status)
        {
            var query = _context.Reservations
                .Include(r => r.LibraryItem)
                .Include(r => r.Borrower)
                .AsQueryable();

            if (itemId.HasValue && itemId.Value > 0)
            {
                query = query.Where(r => r.LibraryItemId == itemId.Value);
            }

            if (borrowerId.HasValue && borrowerId.Value > 0)
            {
                query = query.Where(r => r.BorrowerId == borrowerId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var reservations = await query
                .OrderBy(r => r.Status == ReservationStatus.ReadyForPickup ? 0 : r.Status == ReservationStatus.Pending ? 1 : 2)
                .ThenBy(r => r.QueuePosition)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            var itemsList = await _context.LibraryItems
                .Where(i => i.Status != ItemStatus.Destroy)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var borrowersList = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.FullName)
                .ToListAsync();

            ViewBag.Items = new SelectList(itemsList, "Id", "Name", itemId);
            ViewBag.Borrowers = new SelectList(borrowersList, "Id", "FullName", borrowerId);
            ViewBag.SelectedItemId = itemId;
            ViewBag.SelectedBorrowerId = borrowerId;
            ViewBag.SelectedStatus = status;

            return View(reservations);
        }

        // GET: Reservation/Create
        public async Task<IActionResult> Create(int? libraryItemId)
        {
            await PopulateDropdowns(libraryItemId, null);

            var model = new Reservation
            {
                LibraryItemId = libraryItemId ?? 0
            };

            return View(model);
        }

        // POST: Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation, string? directLibraryCode, string? directCardNumber)
        {
            LibraryItem? item = null;
            Borrower? borrower = null;

            // 1. Resolve LibraryItem by code or ID
            if (!string.IsNullOrWhiteSpace(directLibraryCode))
            {
                var code = directLibraryCode.Trim().ToUpper();
                item = await _context.LibraryItems.FirstOrDefaultAsync(i => i.LibraryCode == code);
                if (item == null)
                {
                    ModelState.AddModelError("directLibraryCode", $"No library item found with code '{code}'.");
                }
            }
            else if (reservation.LibraryItemId > 0)
            {
                item = await _context.LibraryItems.FindAsync(reservation.LibraryItemId);
            }
            else
            {
                ModelState.AddModelError("LibraryItemId", "Please select an item or enter a Library Code.");
            }

            // 2. Resolve Borrower by card number or ID
            if (!string.IsNullOrWhiteSpace(directCardNumber))
            {
                var card = directCardNumber.Trim().ToUpper();
                borrower = await _context.Borrowers.FirstOrDefaultAsync(b => b.CardNumber == card);
                if (borrower == null)
                {
                    ModelState.AddModelError("directCardNumber", $"No borrower found with Card Number '{card}'.");
                }
            }
            else if (reservation.BorrowerId > 0)
            {
                borrower = await _context.Borrowers.FindAsync(reservation.BorrowerId);
            }
            else
            {
                ModelState.AddModelError("BorrowerId", "Please select a borrower or enter a Card Number.");
            }

            // 3. Business rule: Cannot reserve Available items (must borrow instead)
            if (item != null)
            {
                if (item.Status == ItemStatus.Available)
                {
                    ModelState.AddModelError(string.Empty, $"Item '{item.Name}' is currently Available. Patrons can directly borrow this item rather than placing a reservation.");
                }
                else if (item.Status == ItemStatus.Destroy)
                {
                    ModelState.AddModelError(string.Empty, $"Item '{item.Name}' is destroyed and cannot be reserved.");
                }
            }

            // 4. Validate active borrower account
            if (borrower != null && !borrower.IsActive)
            {
                ModelState.AddModelError(string.Empty, $"Borrower '{borrower.FullName}' account is inactive.");
            }

            // 5. Check if borrower already has an active reservation in queue for this item
            if (item != null && borrower != null)
            {
                bool alreadyInQueue = await _context.Reservations.AnyAsync(r =>
                    r.LibraryItemId == item.Id &&
                    r.BorrowerId == borrower.Id &&
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.ReadyForPickup));

                if (alreadyInQueue)
                {
                    ModelState.AddModelError(string.Empty, $"Borrower '{borrower.FullName}' already has an active reservation in the queue for '{item.Name}'.");
                }
            }

            if (ModelState.IsValid && item != null && borrower != null)
            {
                // Auto-assign queue position (FIFO)
                int currentQueueCount = await _context.Reservations.CountAsync(r =>
                    r.LibraryItemId == item.Id &&
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.ReadyForPickup));

                reservation.LibraryItemId = item.Id;
                reservation.BorrowerId = borrower.Id;
                reservation.QueuePosition = currentQueueCount + 1;
                reservation.Status = ReservationStatus.Pending;
                reservation.CreatedAt = DateTime.Now;

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Reservation successfully created for '{item.Name}'! Borrower '{borrower.FullName}' is at Queue Position #{reservation.QueuePosition}.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(reservation.LibraryItemId, reservation.BorrowerId);
            return View(reservation);
        }

        // POST: Reservation/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.LibraryItem)
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null) return NotFound();

            if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.ReadyForPickup)
            {
                TempData["ErrorMessage"] = "Only active reservations (Pending or Ready for Pickup) can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            int cancelledPosition = reservation.QueuePosition;
            int itemId = reservation.LibraryItemId;

            reservation.Status = ReservationStatus.Cancelled;

            // Re-number remaining active queue positions for this item
            var remainingQueue = await _context.Reservations
                .Where(r => r.LibraryItemId == itemId &&
                            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.ReadyForPickup) &&
                            r.ReservationId != id &&
                            r.QueuePosition > cancelledPosition)
                .OrderBy(r => r.QueuePosition)
                .ToListAsync();

            foreach (var queuedItem in remainingQueue)
            {
                queuedItem.QueuePosition--;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Reservation for '{reservation.LibraryItem.Name}' by '{reservation.Borrower.FullName}' cancelled. Remaining queue positions updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Reservation/Fulfill/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.LibraryItem)
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null) return NotFound();

            if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.ReadyForPickup)
            {
                TempData["ErrorMessage"] = "Only Pending or Ready for Pickup reservations can be fulfilled.";
                return RedirectToAction(nameof(Index));
            }

            int fulfilledPosition = reservation.QueuePosition;
            int itemId = reservation.LibraryItemId;

            reservation.Status = ReservationStatus.Fulfilled;

            // Re-number remaining active reservations for this item
            var remainingQueue = await _context.Reservations
                .Where(r => r.LibraryItemId == itemId &&
                            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.ReadyForPickup) &&
                            r.ReservationId != id &&
                            r.QueuePosition > fulfilledPosition)
                .OrderBy(r => r.QueuePosition)
                .ToListAsync();

            foreach (var queuedItem in remainingQueue)
            {
                queuedItem.QueuePosition--;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Reservation for '{reservation.LibraryItem.Name}' marked as Fulfilled. Patron '{reservation.Borrower.FullName}' received the item.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Reservation/Queue/5
        public async Task<IActionResult> Queue(int id)
        {
            var item = await _context.LibraryItems.FindAsync(id);
            if (item == null) return NotFound();

            var activeQueue = await _context.Reservations
                .Include(r => r.Borrower)
                .Where(r => r.LibraryItemId == id &&
                            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.ReadyForPickup))
                .OrderBy(r => r.QueuePosition)
                .ToListAsync();

            ViewBag.LibraryItem = item;
            return View(activeQueue);
        }

        private async Task PopulateDropdowns(int? selectedItemId, int? selectedBorrowerId)
        {
            var itemsList = await _context.LibraryItems
                .Where(i => i.Status != ItemStatus.Destroy)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var borrowersList = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.FullName)
                .ToListAsync();

            ViewBag.LibraryItems = new SelectList(itemsList, "Id", "Name", selectedItemId);
            ViewBag.ActiveBorrowers = new SelectList(borrowersList, "Id", "FullName", selectedBorrowerId);
        }
    }
}
