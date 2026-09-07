using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public ManagerController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Manager/Index (Analytics Dashboard)
        public async Task<IActionResult> Index()
        {
            var items = await _context.LibraryItems.ToListAsync();
            var transactions = await _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .ToListAsync();

            var now = DateTime.Now;

            // Borrowing Statistics
            int totalTransactions = transactions.Count;
            int activeLoans = transactions.Count(t => t.ReturnDate == null);
            int returnedLoans = transactions.Count(t => t.ReturnDate != null);
            int overdueLoans = transactions.Count(t => t.ReturnDate == null && now > t.DueDate);

            // Item Status Statistics
            int availableCount = items.Count(i => i.Status == ItemStatus.Available);
            int borrowedCount = items.Count(i => i.Status == ItemStatus.Borrowed);
            int damagedCount = items.Count(i => i.Status == ItemStatus.Damaged);
            int destroyCount = items.Count(i => i.Status == ItemStatus.Destroy);
            int totalItems = items.Count;

            int booksCount = items.OfType<BookItem>().Count();
            int musicCount = items.OfType<MusicItem>().Count();
            int toysCount = items.OfType<ToyItem>().Count();

            // Fine Statistics
            // Calculate fines for returned items with fines
            decimal recordedTotalFines = transactions.Sum(t => t.FineAmount);
            decimal recordedPaidFines = transactions.Where(t => t.IsFinePaid).Sum(t => t.FineAmount);
            decimal recordedUnpaidFines = transactions.Where(t => !t.IsFinePaid).Sum(t => t.FineAmount);
            int finedTransactionsCount = transactions.Count(t => t.FineAmount > 0);

            // Also include pending fines for active overdue loans
            decimal pendingFines = 0.00m;
            foreach (var loan in transactions.Where(t => t.ReturnDate == null && now > t.DueDate))
            {
                int daysOverdue = (now.Date - loan.DueDate.Date).Days;
                if (daysOverdue > 0)
                {
                    pendingFines += daysOverdue * 1.50m;
                }
            }

            var model = new ManagerDashboardViewModel
            {
                TotalTransactions = totalTransactions,
                ActiveLoans = activeLoans,
                ReturnedLoans = returnedLoans,
                OverdueLoans = overdueLoans,

                AvailableCount = availableCount,
                BorrowedCount = borrowedCount,
                DamagedCount = damagedCount,
                DestroyCount = destroyCount,
                TotalItems = totalItems,
                BooksCount = booksCount,
                MusicCount = musicCount,
                ToysCount = toysCount,

                TotalFines = recordedTotalFines + pendingFines,
                TotalPaidFines = recordedPaidFines,
                TotalUnpaidFines = recordedUnpaidFines + pendingFines,
                FinedTransactionsCount = finedTransactionsCount + overdueLoans
            };

            ViewBag.TransactionsList = transactions.OrderByDescending(t => t.BorrowDate).Take(10).ToList();

            return View(model);
        }
    }
}
