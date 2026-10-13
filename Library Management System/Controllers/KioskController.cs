using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [AllowAnonymous]
    public class KioskController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public KioskController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Kiosk/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: Kiosk/Scan?libraryCode=BK-1001
        public async Task<IActionResult> Scan(string? libraryCode)
        {
            if (string.IsNullOrWhiteSpace(libraryCode))
            {
                TempData["ErrorMessage"] = "Please scan or enter a valid Library Code.";
                return RedirectToAction(nameof(Index));
            }

            var code = libraryCode.Trim().ToUpper();
            var item = await _context.LibraryItems
                .FirstOrDefaultAsync(i => i.LibraryCode == code);

            if (item == null)
            {
                TempData["ErrorMessage"] = $"No item found with Library Code '{code}'. Please verify the code and try again.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(ItemDetail), new { id = item.Id });
        }

        // GET: Kiosk/ItemDetail/5
        public async Task<IActionResult> ItemDetail(int id)
        {
            var item = await _context.LibraryItems
                .Include(i => i.BorrowTransactions)
                    .ThenInclude(t => t.Borrower)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                TempData["ErrorMessage"] = "The requested library item could not be found.";
                return RedirectToAction(nameof(Index));
            }

            // Query active loan if currently borrowed
            var activeLoan = item.BorrowTransactions
                .Where(t => t.ReturnDate == null)
                .OrderByDescending(t => t.BorrowDate)
                .FirstOrDefault();

            ViewBag.ActiveLoan = activeLoan;

            // Check if item has any waiting reservations (if table exists)
            var activeReservationCount = await _context.Reservations
                .CountAsync(r => r.LibraryItemId == item.Id &&
                                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.ReadyForPickup));
            ViewBag.ActiveReservationCount = activeReservationCount;

            return View(item);
        }

        // GET: Kiosk/AccountLookup?cardNumber=BW-1001
        public async Task<IActionResult> AccountLookup(string? cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                TempData["ErrorMessage"] = "Please scan or enter your Library Card Number.";
                return RedirectToAction(nameof(Index));
            }

            var card = cardNumber.Trim().ToUpper();
            var borrower = await _context.Borrowers
                .Include(b => b.BorrowTransactions)
                    .ThenInclude(t => t.LibraryItem)
                .FirstOrDefaultAsync(b => b.CardNumber == card);

            if (borrower == null)
            {
                TempData["ErrorMessage"] = $"Card Number '{card}' was not found. Please check your card or visit reception desk for assistance.";
                return RedirectToAction(nameof(Index));
            }

            // Calculate pending fines for active overdue items ($1.50 per day late)
            var now = DateTime.Now;
            var activeLoans = borrower.BorrowTransactions
                .Where(t => t.ReturnDate == null)
                .OrderBy(t => t.DueDate)
                .ToList();

            decimal accruedPendingFines = 0.00m;
            foreach (var loan in activeLoans.Where(l => now > l.DueDate))
            {
                int daysLate = (now.Date - loan.DueDate.Date).Days;
                if (daysLate > 0)
                {
                    accruedPendingFines += daysLate * 1.50m;
                }
            }

            // Outstanding unpaid fines from past completed returns
            decimal unpaidHistoricalFines = borrower.BorrowTransactions
                .Where(t => t.ReturnDate != null && !t.IsFinePaid)
                .Sum(t => t.FineAmount);

            ViewBag.ActiveLoans = activeLoans;
            ViewBag.TotalUnpaidFines = accruedPendingFines + unpaidHistoricalFines;
            ViewBag.PendingOverdueFines = accruedPendingFines;

            return View(borrower);
        }
    }
}
