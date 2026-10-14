using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;
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
            var model = await BuildDashboardViewModelAsync();

            var recentTransactions = await _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .OrderByDescending(t => t.BorrowDate)
                .Take(10)
                .ToListAsync();

            ViewBag.TransactionsList = recentTransactions;

            return View(model);
        }

        // GET: Manager/ExportCsv
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var transactions = await _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .OrderByDescending(t => t.BorrowDate)
                .ToListAsync();

            var now = DateTime.Now;
            var exportRows = transactions.Select(t =>
            {
                int daysLate = 0;
                if (t.ReturnDate.HasValue)
                {
                    if (t.ReturnDate.Value.Date > t.DueDate.Date)
                    {
                        daysLate = (t.ReturnDate.Value.Date - t.DueDate.Date).Days;
                    }
                }
                else if (now.Date > t.DueDate.Date)
                {
                    daysLate = (now.Date - t.DueDate.Date).Days;
                }

                return new
                {
                    TransactionId = t.Id,
                    ItemName = t.LibraryItem?.Name ?? "Unknown Item",
                    LibraryCode = t.LibraryItem?.LibraryCode ?? "N/A",
                    BorrowerName = t.Borrower?.FullName ?? "Unknown Borrower",
                    CardNumber = t.Borrower?.CardNumber ?? "N/A",
                    BorrowDate = t.BorrowDate.ToString("yyyy-MM-dd"),
                    DueDate = t.DueDate.ToString("yyyy-MM-dd"),
                    ReturnDate = t.ReturnDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    DaysLate = daysLate,
                    FineAmount = t.FineAmount,
                    IsFinePaid = t.IsFinePaid
                };
            }).ToList();

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(exportRows);
            }

            var bytes = memoryStream.ToArray();
            return File(bytes, "text/csv", $"library-report-{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Manager/ExportPdf
        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var model = await BuildDashboardViewModelAsync();
            var topTransactions = await _context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .OrderByDescending(t => t.BorrowDate)
                .Take(20)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("Library Management System").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                titleCol.Item().Text("Executive Manager Analytics Report").FontSize(11).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(180).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text($"Report Date: {DateTime.Now:yyyy-MM-dd}").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                metaCol.Item().Text($"Generated: {DateTime.Now:HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });
                        col.Item().PaddingTop(6).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        // 1. Summary Statistics Table
                        col.Item().Text("1. Executive Summary Statistics").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().PaddingTop(4).PaddingBottom(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            // Row 1
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Total Transactions:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(model.TotalTransactions.ToString());
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Total Items in Catalog:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(model.TotalItems.ToString());

                            // Row 2
                            table.Cell().Padding(4).Text("Active Loans:").Bold();
                            table.Cell().Padding(4).Text(model.ActiveLoans.ToString());
                            table.Cell().Padding(4).Text("Available Items:").Bold();
                            table.Cell().Padding(4).Text(model.AvailableCount.ToString());

                            // Row 3
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Returned Loans:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(model.ReturnedLoans.ToString());
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Currently Borrowed:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(model.BorrowedCount.ToString());

                            // Row 4
                            table.Cell().Padding(4).Text("Overdue Loans:").Bold();
                            table.Cell().Padding(4).Text(model.OverdueLoans.ToString()).FontColor(Colors.Red.Medium);
                            table.Cell().Padding(4).Text("Damaged / Destroyed:").Bold();
                            table.Cell().Padding(4).Text($"{model.DamagedCount} / {model.DestroyCount}");

                            // Row 5
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Total Fines Accrued:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text($"${model.TotalFines:F2}");
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Books / Music / Toys:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text($"{model.BooksCount} / {model.MusicCount} / {model.ToysCount}");

                            // Row 6
                            table.Cell().Padding(4).Text("Collected / Unpaid Fines:").Bold();
                            table.Cell().Padding(4).Text($"${model.TotalPaidFines:F2} / ${model.TotalUnpaidFines:F2}");
                            table.Cell().Padding(4).Text("Fined Transactions:").Bold();
                            table.Cell().Padding(4).Text(model.FinedTransactionsCount.ToString());
                        });

                        // 2. Top 20 Transactions Table
                        col.Item().Text("2. Recent Borrowing Transactions (Top 20)").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(22);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("#").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Item Title").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Borrower").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Borrowed").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Due").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Returned").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Fine").Bold().FontColor(Colors.White);
                            });

                            int idx = 1;
                            foreach (var t in topTransactions)
                            {
                                var bg = (idx % 2 == 0) ? Colors.Grey.Lighten5 : Colors.White;

                                table.Cell().Background(bg).Padding(4).Text(idx.ToString());
                                table.Cell().Background(bg).Padding(4).Text(t.LibraryItem?.Name ?? "Item");
                                table.Cell().Background(bg).Padding(4).Text(t.Borrower?.FullName ?? "Borrower");
                                table.Cell().Background(bg).Padding(4).Text(t.BorrowDate.ToString("yyyy-MM-dd"));
                                table.Cell().Background(bg).Padding(4).Text(t.DueDate.ToString("yyyy-MM-dd"));
                                table.Cell().Background(bg).Padding(4).Text(t.ReturnDate.HasValue ? t.ReturnDate.Value.ToString("yyyy-MM-dd") : "Active");
                                table.Cell().Background(bg).Padding(4).Text(t.FineAmount > 0 ? $"${t.FineAmount:F2} ({(t.IsFinePaid ? "Paid" : "Unpaid")})" : "$0.00");

                                idx++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                        x.Span(" | Generated by Library Management System");
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"library-report-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: Manager/BorrowingTrend
        [HttpGet]
        public async Task<IActionResult> BorrowingTrend()
        {
            var now = DateTime.Now;
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            var transactions = await _context.BorrowTransactions
                .Where(t => t.BorrowDate >= startMonth)
                .Select(t => t.BorrowDate)
                .ToListAsync();

            var labels = new List<string>();
            var counts = new List<int>();
            var monthlyCounts = new Dictionary<string, int>();
            var details = new List<object>();

            for (int i = 0; i < 12; i++)
            {
                var targetDate = startMonth.AddMonths(i);
                var label = targetDate.ToString("MMM yyyy");
                int count = transactions.Count(t => t.Year == targetDate.Year && t.Month == targetDate.Month);

                labels.Add(label);
                counts.Add(count);
                monthlyCounts[label] = count;
                details.Add(new
                {
                    month = label,
                    yearMonth = targetDate.ToString("yyyy-MM"),
                    count = count
                });
            }

            return Json(new
            {
                labels = labels,
                counts = counts,
                data = counts,
                monthlyCounts = monthlyCounts,
                details = details
            });
        }

        private async Task<ManagerDashboardViewModel> BuildDashboardViewModelAsync()
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
            decimal recordedTotalFines = transactions.Sum(t => t.FineAmount);
            decimal recordedPaidFines = transactions.Where(t => t.IsFinePaid).Sum(t => t.FineAmount);
            decimal recordedUnpaidFines = transactions.Where(t => !t.IsFinePaid).Sum(t => t.FineAmount);
            int finedTransactionsCount = transactions.Count(t => t.FineAmount > 0);

            // Pending fines for active overdue loans ($1.50/day late rate)
            decimal pendingFines = 0.00m;
            foreach (var loan in transactions.Where(t => t.ReturnDate == null && now > t.DueDate))
            {
                int daysOverdue = (now.Date - loan.DueDate.Date).Days;
                if (daysOverdue > 0)
                {
                    pendingFines += daysOverdue * 1.50m;
                }
            }

            // Monthly borrow counts (last 12 months)
            var monthlyCounts = new Dictionary<string, int>();
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            for (int i = 0; i < 12; i++)
            {
                var targetDate = startMonth.AddMonths(i);
                var label = targetDate.ToString("MMM yyyy");
                int count = transactions.Count(t => t.BorrowDate.Year == targetDate.Year && t.BorrowDate.Month == targetDate.Month);
                monthlyCounts[label] = count;
            }

            // Branch summaries
            var branchSummaries = new List<BranchSummaryViewModel>();
            var branchProp = typeof(LibraryItem).GetProperty("BranchId");
            if (branchProp != null)
            {
                var branchGroups = items
                    .GroupBy(i => (int?)branchProp.GetValue(i) ?? 1)
                    .OrderBy(g => g.Key);

                foreach (var group in branchGroups)
                {
                    branchSummaries.Add(new BranchSummaryViewModel
                    {
                        BranchName = $"Branch #{group.Key}",
                        TotalItems = group.Count(),
                        AvailableItems = group.Count(i => i.Status == ItemStatus.Available),
                        BorrowedItems = group.Count(i => i.Status == ItemStatus.Borrowed)
                    });
                }
            }
            else
            {
                branchSummaries.Add(new BranchSummaryViewModel
                {
                    BranchName = "Main Library (Central)",
                    TotalItems = totalItems,
                    AvailableItems = availableCount,
                    BorrowedItems = borrowedCount
                });
            }

            return new ManagerDashboardViewModel
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
                FinedTransactionsCount = finedTransactionsCount + overdueLoans,

                BranchSummaries = branchSummaries,
                MonthlyBorrowCounts = monthlyCounts
            };
        }
    }
}
