using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Reception")]
    public class ReceptionController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public ReceptionController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Reception/Index (Dashboard & Active Loans)
        public async Task<IActionResult> Index(string? search)
        {
            var activeLoansQuery = _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .Where(t => t.ReturnDate == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                activeLoansQuery = activeLoansQuery.Where(t =>
                    t.LibraryItem.LibraryCode.ToLower().Contains(term) ||
                    t.LibraryItem.Name.ToLower().Contains(term) ||
                    t.Borrower.CardNumber.ToLower().Contains(term) ||
                    t.Borrower.FullName.ToLower().Contains(term));
            }

            ViewBag.Search = search;
            var activeLoans = await activeLoansQuery.OrderByDescending(t => t.BorrowDate).ToListAsync();
            return View(activeLoans);
        }

        // GET: Reception/Borrow
        public async Task<IActionResult> Borrow(int? libraryItemId)
        {
            var availableItems = await _context.LibraryItems
                .Where(i => i.Status == ItemStatus.Available)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var activeBorrowers = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.FullName)
                .ToListAsync();

            ViewBag.AvailableItems = new SelectList(availableItems, "Id", "Name", libraryItemId);
            ViewBag.ActiveBorrowers = new SelectList(activeBorrowers, "Id", "FullName");

            var model = new BorrowItemViewModel
            {
                LibraryItemId = libraryItemId ?? 0,
                DaysToBorrow = 14
            };

            return View(model);
        }

        // POST: Reception/Borrow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(BorrowItemViewModel model)
        {
            LibraryItem? item = null;
            Borrower? borrower = null;

            // 1. Resolve LibraryItem by direct code or dropdown ID
            if (!string.IsNullOrWhiteSpace(model.DirectLibraryCode))
            {
                var code = model.DirectLibraryCode.Trim().ToUpper();
                item = await _context.LibraryItems.FirstOrDefaultAsync(i => i.LibraryCode == code);
                if (item == null)
                {
                    ModelState.AddModelError("DirectLibraryCode", $"No library item found with code '{code}'.");
                }
            }
            else if (model.LibraryItemId > 0)
            {
                item = await _context.LibraryItems.FindAsync(model.LibraryItemId);
            }
            else
            {
                ModelState.AddModelError("LibraryItemId", "Please select an item or enter a Library Code.");
            }

            // 2. Resolve Borrower by direct card number or dropdown ID
            if (!string.IsNullOrWhiteSpace(model.DirectCardNumber))
            {
                var card = model.DirectCardNumber.Trim().ToUpper();
                borrower = await _context.Borrowers.FirstOrDefaultAsync(b => b.CardNumber == card);
                if (borrower == null)
                {
                    ModelState.AddModelError("DirectCardNumber", $"No borrower found with Card Number '{card}'.");
                }
            }
            else if (model.BorrowerId > 0)
            {
                borrower = await _context.Borrowers.FindAsync(model.BorrowerId);
            }
            else
            {
                ModelState.AddModelError("BorrowerId", "Please select a borrower or enter a Card Number.");
            }

            // 3. Business Rule Validation: Check availability
            if (item != null && item.Status != ItemStatus.Available)
            {
                ModelState.AddModelError(string.Empty, $"Item '{item.Name}' cannot be borrowed because its current status is '{item.Status}'. Only 'Available' items can be borrowed.");
            }

            if (borrower != null && !borrower.IsActive)
            {
                ModelState.AddModelError(string.Empty, $"Borrower '{borrower.FullName}' account is inactive.");
            }

            if (ModelState.IsValid && item != null && borrower != null)
            {
                // Set loan dates and update item status
                var days = model.DaysToBorrow > 0 ? model.DaysToBorrow : 14;
                var transaction = new BorrowTransaction
                {
                    LibraryItemId = item.Id,
                    BorrowerId = borrower.Id,
                    BorrowDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(days),
                    ReturnDate = null,
                    FineAmount = 0.00m,
                    IsFinePaid = false,
                    Notes = model.Notes
                };

                item.Status = ItemStatus.Borrowed;

                _context.BorrowTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Item '{item.Name}' ({item.LibraryCode}) successfully checked out to {borrower.FullName}. Due date: {transaction.DueDate:MMM dd, yyyy}.";
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns on validation failure
            var availableItemsList = await _context.LibraryItems.Where(i => i.Status == ItemStatus.Available).ToListAsync();
            var activeBorrowersList = await _context.Borrowers.Where(b => b.IsActive).ToListAsync();
            ViewBag.AvailableItems = new SelectList(availableItemsList, "Id", "Name", model.LibraryItemId);
            ViewBag.ActiveBorrowers = new SelectList(activeBorrowersList, "Id", "FullName", model.BorrowerId);

            return View(model);
        }

        // GET: Reception/Return/5
        public async Task<IActionResult> Return(int id)
        {
            var transaction = await _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();

            var returnDate = DateTime.Now;
            int daysLate = 0;
            if (returnDate > transaction.DueDate)
            {
                daysLate = (returnDate.Date - transaction.DueDate.Date).Days;
                if (daysLate < 0) daysLate = 0;
            }
            decimal fine = daysLate * 1.50m;

            var model = new ReturnItemViewModel
            {
                TransactionId = transaction.Id,
                ItemName = transaction.LibraryItem.Name,
                LibraryCode = transaction.LibraryItem.LibraryCode,
                BorrowerName = transaction.Borrower.FullName,
                CardNumber = transaction.Borrower.CardNumber,
                BorrowDate = transaction.BorrowDate,
                DueDate = transaction.DueDate,
                ReturnDate = returnDate,
                DaysLate = daysLate,
                FineAmount = fine,
                IsFinePaid = fine == 0 ? true : false,
                Notes = transaction.Notes
            };

            return View(model);
        }

        // POST: Reception/Return
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(ReturnItemViewModel model)
        {
            var transaction = await _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

            if (transaction == null) return NotFound();

            var returnDate = DateTime.Now;
            int daysLate = 0;
            if (returnDate > transaction.DueDate)
            {
                daysLate = (returnDate.Date - transaction.DueDate.Date).Days;
                if (daysLate < 0) daysLate = 0;
            }
            decimal fineAmount = daysLate * 1.50m;

            transaction.ReturnDate = returnDate;
            transaction.FineAmount = fineAmount;
            transaction.IsFinePaid = model.IsFinePaid;
            if (!string.IsNullOrWhiteSpace(model.Notes))
            {
                transaction.Notes = model.Notes;
            }

            // Return item back to Available status
            transaction.LibraryItem.Status = ItemStatus.Available;

            await _context.SaveChangesAsync();

            string fineMsg = fineAmount > 0
                ? $" Fine of ${fineAmount:F2} calculated ({daysLate} days late). Fine status: {(model.IsFinePaid ? "PAID" : "UNPAID")}."
                : "";

            TempData["SuccessMessage"] = $"Item '{transaction.LibraryItem.Name}' returned successfully.{fineMsg}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Reception/Borrowers
        public async Task<IActionResult> Borrowers(string? search)
        {
            var query = _context.Borrowers.Include(b => b.BorrowTransactions).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.FullName.ToLower().Contains(term) ||
                                         b.CardNumber.ToLower().Contains(term) ||
                                         (b.Email != null && b.Email.ToLower().Contains(term)));
            }

            ViewBag.Search = search;
            var list = await query.OrderBy(b => b.FullName).ToListAsync();
            return View(list);
        }

        // GET: Reception/CreateBorrower
        public IActionResult CreateBorrower()
        {
            return View(new Borrower { CardNumber = $"BW-{new Random().Next(1000, 9999)}" });
        }

        // POST: Reception/CreateBorrower
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBorrower(Borrower borrower)
        {
            if (!string.IsNullOrWhiteSpace(borrower.CardNumber) &&
                await _context.Borrowers.AnyAsync(b => b.CardNumber.ToLower() == borrower.CardNumber.Trim().ToLower()))
            {
                ModelState.AddModelError("CardNumber", "A borrower with this Card Number already exists.");
            }

            if (ModelState.IsValid)
            {
                borrower.CardNumber = borrower.CardNumber.Trim().ToUpper();
                borrower.MembershipDate = DateTime.Now;
                _context.Borrowers.Add(borrower);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Borrower '{borrower.FullName}' created successfully.";
                return RedirectToAction(nameof(Borrowers));
            }

            return View(borrower);
        }

        // GET: Reception/EditBorrower/5
        public async Task<IActionResult> EditBorrower(int? id)
        {
            if (id == null) return NotFound();

            var borrower = await _context.Borrowers.FindAsync(id);
            if (borrower == null) return NotFound();

            return View(borrower);
        }

        // POST: Reception/EditBorrower/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBorrower(int id, Borrower borrower)
        {
            if (id != borrower.Id) return NotFound();

            if (await _context.Borrowers.AnyAsync(b => b.Id != id && b.CardNumber.ToLower() == borrower.CardNumber.Trim().ToLower()))
            {
                ModelState.AddModelError("CardNumber", "Another borrower with this Card Number already exists.");
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Borrowers.FindAsync(id);
                if (existing == null) return NotFound();

                existing.CardNumber = borrower.CardNumber.Trim().ToUpper();
                existing.FullName = borrower.FullName;
                existing.Email = borrower.Email;
                existing.Phone = borrower.Phone;
                existing.Address = borrower.Address;
                existing.IsActive = borrower.IsActive;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Borrower '{borrower.FullName}' updated successfully.";
                return RedirectToAction(nameof(Borrowers));
            }

            return View(borrower);
        }

        // GET: Reception/BorrowerDetails/5
        public async Task<IActionResult> BorrowerDetails(int? id)
        {
            if (id == null) return NotFound();

            var borrower = await _context.Borrowers
                .Include(b => b.BorrowTransactions)
                    .ThenInclude(t => t.LibraryItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null) return NotFound();

            return View(borrower);
        }

        // GET: Reception/DeleteBorrower/5
        public async Task<IActionResult> DeleteBorrower(int? id)
        {
            if (id == null) return NotFound();

            var borrower = await _context.Borrowers
                .Include(b => b.BorrowTransactions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null) return NotFound();

            return View(borrower);
        }

        // POST: Reception/DeleteBorrower/5
        [HttpPost, ActionName("DeleteBorrower")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBorrowerConfirmed(int id)
        {
            var borrower = await _context.Borrowers
                .Include(b => b.BorrowTransactions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null) return NotFound();

            // Check if borrower has historical transactions
            if (borrower.BorrowTransactions.Any())
            {
                // Soft-delete / Deactivate borrower to prevent breaking foreign keys
                borrower.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Borrower '{borrower.FullName}' deactivated (preserved to maintain borrowing history).";
            }
            else
            {
                _context.Borrowers.Remove(borrower);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Borrower '{borrower.FullName}' permanently deleted.";
            }

            return RedirectToAction(nameof(Borrowers));
        }
    }
}
