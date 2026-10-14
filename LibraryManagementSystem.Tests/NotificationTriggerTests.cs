using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using LibraryManagementSystem.Controllers;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem.Tests
{
    public class NotificationTriggerTests
    {
        private static LibraryManagementSystemContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<LibraryManagementSystemContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new LibraryManagementSystemContext(options);
        }

        private static ReceptionController CreateReceptionController(LibraryManagementSystemContext context)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = Mock.Of<ITempDataProvider>();

            return new ReceptionController(context)
            {
                TempData = new TempDataDictionary(httpContext, tempDataProvider),
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public async Task ReturningOverdueItem_CreatesFineIssuedNotification()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 1,
                Name = "The Pragmatic Programmer",
                LibraryCode = "BK-1001",
                Status = ItemStatus.Borrowed,
                Author = "Andrew Hunt",
                Genre = "Technology"
            };

            var borrower = new Borrower
            {
                Id = 10,
                FullName = "John Doe",
                CardNumber = "BW-1001",
                Email = "john@example.com",
                IsActive = true
            };

            // Due 4 days ago
            var transaction = new BorrowTransaction
            {
                Id = 100,
                LibraryItemId = 1,
                LibraryItem = book,
                BorrowerId = 10,
                Borrower = borrower,
                BorrowDate = DateTime.Now.AddDays(-18),
                DueDate = DateTime.Now.AddDays(-4),
                ReturnDate = null,
                FineAmount = 0.00m,
                IsFinePaid = false
            };

            context.LibraryItems.Add(book);
            context.Borrowers.Add(borrower);
            context.BorrowTransactions.Add(transaction);
            await context.SaveChangesAsync();

            var controller = CreateReceptionController(context);

            var returnModel = new ReturnItemViewModel
            {
                TransactionId = transaction.Id,
                IsFinePaid = false,
                Notes = "Returned late"
            };

            // Act: Return the overdue item
            var result = await controller.Return(returnModel);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            // Verify transaction and item state
            var updatedTx = await context.BorrowTransactions.FindAsync(transaction.Id);
            Assert.NotNull(updatedTx);
            Assert.NotNull(updatedTx.ReturnDate);
            Assert.True(updatedTx.FineAmount > 0, "Fine amount should be calculated.");
            Assert.Equal(ItemStatus.Available, book.Status);

            // Verify that a FineIssued notification was created for John Doe
            var fineNotification = await context.Notifications
                .FirstOrDefaultAsync(n => n.BorrowerId == borrower.Id && n.Type == NotificationType.FineIssued);

            Assert.NotNull(fineNotification);
            Assert.Equal(book.Id, fineNotification.LibraryItemId);
            Assert.False(fineNotification.IsRead);
            Assert.Contains("Fine of $", fineNotification.Message);
            Assert.Contains("issued for returning 'The Pragmatic Programmer'", fineNotification.Message);
        }

        [Fact]
        public async Task ReturnTriggersReservationReadyNotification_ForNextInQueue()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 2,
                Name = "Domain-Driven Design",
                LibraryCode = "BK-1002",
                Status = ItemStatus.Borrowed,
                Author = "Eric Evans",
                Genre = "Software"
            };

            var currentBorrower = new Borrower
            {
                Id = 20,
                FullName = "Current Borrower",
                CardNumber = "BW-2001",
                IsActive = true
            };

            var waitingBorrower = new Borrower
            {
                Id = 21,
                FullName = "Next In Line",
                CardNumber = "BW-2002",
                Email = "next@example.com",
                IsActive = true
            };

            var transaction = new BorrowTransaction
            {
                Id = 200,
                LibraryItemId = 2,
                LibraryItem = book,
                BorrowerId = 20,
                Borrower = currentBorrower,
                BorrowDate = DateTime.Now.AddDays(-7),
                DueDate = DateTime.Now.AddDays(7), // On time return
                ReturnDate = null
            };

            var queueReservation = new Reservation
            {
                ReservationId = 50,
                LibraryItemId = 2,
                BorrowerId = 21,
                Borrower = waitingBorrower,
                CreatedAt = DateTime.Now.AddDays(-2),
                Status = ReservationStatus.Pending,
                QueuePosition = 1
            };

            context.LibraryItems.Add(book);
            context.Borrowers.AddRange(currentBorrower, waitingBorrower);
            context.BorrowTransactions.Add(transaction);
            context.Reservations.Add(queueReservation);
            await context.SaveChangesAsync();

            var controller = CreateReceptionController(context);

            var returnModel = new ReturnItemViewModel
            {
                TransactionId = transaction.Id,
                IsFinePaid = false
            };

            // Act: Return the item
            var result = await controller.Return(returnModel);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            // Verify reservation transitioned to ReadyForPickup
            var updatedReservation = await context.Reservations.FindAsync(queueReservation.ReservationId);
            Assert.NotNull(updatedReservation);
            Assert.Equal(ReservationStatus.ReadyForPickup, updatedReservation.Status);
            Assert.NotNull(updatedReservation.NotifiedAt);
            Assert.NotNull(updatedReservation.ExpiresAt);

            // Verify ReservationReady notification was dispatched to the waiting patron
            var reservationNotification = await context.Notifications
                .FirstOrDefaultAsync(n => n.BorrowerId == waitingBorrower.Id && n.Type == NotificationType.ReservationReady);

            Assert.NotNull(reservationNotification);
            Assert.Equal(book.Id, reservationNotification.LibraryItemId);
            Assert.False(reservationNotification.IsRead);
            Assert.Contains("ready for pickup", reservationNotification.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Domain-Driven Design", reservationNotification.Message);
        }

        [Fact]
        public async Task ReturnOnTimeItem_WithNoReservations_DoesNotCreateNotifications()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 3,
                Name = "Refactoring UI",
                LibraryCode = "BK-1003",
                Status = ItemStatus.Borrowed,
                Author = "Steve Schoger",
                Genre = "Design"
            };

            var borrower = new Borrower
            {
                Id = 30,
                FullName = "Clean Patron",
                CardNumber = "BW-3001",
                IsActive = true
            };

            var transaction = new BorrowTransaction
            {
                Id = 300,
                LibraryItemId = 3,
                LibraryItem = book,
                BorrowerId = 30,
                Borrower = borrower,
                BorrowDate = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(5), // On time
                ReturnDate = null
            };

            context.LibraryItems.Add(book);
            context.Borrowers.Add(borrower);
            context.BorrowTransactions.Add(transaction);
            await context.SaveChangesAsync();

            var controller = CreateReceptionController(context);

            var returnModel = new ReturnItemViewModel
            {
                TransactionId = transaction.Id,
                IsFinePaid = false
            };

            // Act: Return on time without any waiting reservations
            var result = await controller.Return(returnModel);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var notificationCount = await context.Notifications.CountAsync();
            Assert.Equal(0, notificationCount);
        }
    }
}
