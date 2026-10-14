using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public NotificationController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Notification/Index
        public async Task<IActionResult> Index(bool? unreadOnly, NotificationType? typeFilter)
        {
            // Run background simulation checks for due soon and overdue items
            await NotificationService.CheckDueSoonNotificationsAsync(_context);
            await NotificationService.CheckOverdueNotificationsAsync(_context);

            var query = _context.Notifications
                .Include(n => n.Borrower)
                .Include(n => n.LibraryItem)
                .AsQueryable();

            if (unreadOnly == true)
            {
                query = query.Where(n => !n.IsRead);
            }

            if (typeFilter.HasValue)
            {
                query = query.Where(n => n.Type == typeFilter.Value);
            }

            var notifications = await query
                .OrderBy(n => n.IsRead ? 1 : 0)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.UnreadCount = await _context.Notifications.CountAsync(n => !n.IsRead);
            ViewBag.TotalCount = await _context.Notifications.CountAsync();
            ViewBag.UnreadOnly = unreadOnly ?? false;
            ViewBag.TypeFilter = typeFilter;

            return View(notifications);
        }

        // POST: Notification/MarkRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Notification marked as read.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Notification/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var unreadList = await _context.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync();

            if (unreadList.Any())
            {
                foreach (var item in unreadList)
                {
                    item.IsRead = true;
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"All {unreadList.Count} notification(s) marked as read.";
            }
            else
            {
                TempData["SuccessMessage"] = "No unread notifications to mark.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
