using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services
{
    public class NotificationService
    {
        /// <summary>
        /// Creates and saves a Notification record asynchronously.
        /// </summary>
        public static async Task<Notification> CreateNotificationAsync(
            LibraryManagementSystemContext context,
            int borrowerId,
            int? itemId,
            string message,
            NotificationType type)
        {
            var notification = new Notification
            {
                BorrowerId = borrowerId,
                LibraryItemId = itemId,
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            return notification;
        }

        /// <summary>
        /// Convenience method supporting both naming styles.
        /// </summary>
        public static Task<Notification> CreateNotification(
            LibraryManagementSystemContext context,
            int borrowerId,
            int? itemId,
            string message,
            NotificationType type)
        {
            return CreateNotificationAsync(context, borrowerId, itemId, message, type);
        }

        /// <summary>
        /// Queries active loans due within the next 2 days and creates DueSoon notifications
        /// if the patron has not already been notified today for that item.
        /// </summary>
        public static async Task<int> CheckDueSoonNotificationsAsync(LibraryManagementSystemContext context)
        {
            var now = DateTime.Now;
            var twoDaysAhead = now.AddDays(2);
            var today = now.Date;

            var dueSoonLoans = await context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .Where(t => t.ReturnDate == null && t.DueDate >= now && t.DueDate <= twoDaysAhead)
                .ToListAsync();

            int createdCount = 0;

            foreach (var loan in dueSoonLoans)
            {
                bool alreadyNotifiedToday = await context.Notifications.AnyAsync(n =>
                    n.BorrowerId == loan.BorrowerId &&
                    n.LibraryItemId == loan.LibraryItemId &&
                    n.Type == NotificationType.DueSoon &&
                    n.CreatedAt >= today);

                if (!alreadyNotifiedToday)
                {
                    int daysLeft = (loan.DueDate.Date - today).Days;
                    string timeText = daysLeft == 0 ? "today" : daysLeft == 1 ? "tomorrow" : $"in {daysLeft} days";
                    string msg = $"Reminder: Item '{loan.LibraryItem.Name}' ({loan.LibraryItem.LibraryCode}) is due {timeText} on {loan.DueDate:MMM dd, yyyy}.";

                    var notif = new Notification
                    {
                        BorrowerId = loan.BorrowerId,
                        LibraryItemId = loan.LibraryItemId,
                        Message = msg,
                        Type = NotificationType.DueSoon,
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };

                    context.Notifications.Add(notif);
                    createdCount++;
                }
            }

            if (createdCount > 0)
            {
                await context.SaveChangesAsync();
            }

            return createdCount;
        }

        /// <summary>
        /// Convenience alias for CheckDueSoonNotificationsAsync.
        /// </summary>
        public static Task<int> CheckDueSoonNotifications(LibraryManagementSystemContext context)
        {
            return CheckDueSoonNotificationsAsync(context);
        }

        /// <summary>
        /// Queries overdue active loans and creates Overdue notifications
        /// if the patron has not already received an overdue notification today for that item.
        /// </summary>
        public static async Task<int> CheckOverdueNotificationsAsync(LibraryManagementSystemContext context)
        {
            var now = DateTime.Now;
            var today = now.Date;

            var overdueLoans = await context.BorrowTransactions
                .Include(t => t.LibraryItem)
                .Include(t => t.Borrower)
                .Where(t => t.ReturnDate == null && now > t.DueDate)
                .ToListAsync();

            int createdCount = 0;

            foreach (var loan in overdueLoans)
            {
                bool alreadyNotifiedToday = await context.Notifications.AnyAsync(n =>
                    n.BorrowerId == loan.BorrowerId &&
                    n.LibraryItemId == loan.LibraryItemId &&
                    n.Type == NotificationType.Overdue &&
                    n.CreatedAt >= today);

                if (!alreadyNotifiedToday)
                {
                    int daysLate = (today - loan.DueDate.Date).Days;
                    if (daysLate < 1) daysLate = 1;
                    decimal fineAccrued = daysLate * 1.50m;
                    string msg = $"URGENT: Item '{loan.LibraryItem.Name}' ({loan.LibraryItem.LibraryCode}) is {daysLate} day(s) overdue (Due date was {loan.DueDate:MMM dd}). Accrued fine: ${fineAccrued:F2}. Please return to reception.";

                    var notif = new Notification
                    {
                        BorrowerId = loan.BorrowerId,
                        LibraryItemId = loan.LibraryItemId,
                        Message = msg,
                        Type = NotificationType.Overdue,
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };

                    context.Notifications.Add(notif);
                    createdCount++;
                }
            }

            if (createdCount > 0)
            {
                await context.SaveChangesAsync();
            }

            return createdCount;
        }

        /// <summary>
        /// Convenience alias for CheckOverdueNotificationsAsync.
        /// </summary>
        public static Task<int> CheckOverdueNotifications(LibraryManagementSystemContext context)
        {
            return CheckOverdueNotificationsAsync(context);
        }
    }
}
