using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using LibraryManagementSystem.Controllers;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Tests
{
    public class ReservationQueueTests
    {
        private static LibraryManagementSystemContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<LibraryManagementSystemContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new LibraryManagementSystemContext(options);
        }

        private static ReservationController CreateController(LibraryManagementSystemContext context)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = Mock.Of<ITempDataProvider>();

            return new ReservationController(context)
            {
                TempData = new TempDataDictionary(httpContext, tempDataProvider),
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public async Task CreateReservation_AssignsCorrectFifoQueuePosition()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 1,
                Name = "C# in Depth",
                LibraryCode = "BK-2001",
                Status = ItemStatus.Borrowed,
                Author = "Jon Skeet",
                Genre = "Technology"
            };
            var patron1 = new Borrower { Id = 1, FullName = "Alice Smith", CardNumber = "BW-201", IsActive = true };
            var patron2 = new Borrower { Id = 2, FullName = "Bob Jones", CardNumber = "BW-202", IsActive = true };
            var patron3 = new Borrower { Id = 3, FullName = "Charlie Brown", CardNumber = "BW-203", IsActive = true };

            context.LibraryItems.Add(book);
            context.Borrowers.AddRange(patron1, patron2, patron3);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            // Act: 3 sequential reservations for the same checked-out item
            await controller.Create(new Reservation { LibraryItemId = 1, BorrowerId = 1 }, null, null);
            await controller.Create(new Reservation { LibraryItemId = 1, BorrowerId = 2 }, null, null);
            await controller.Create(new Reservation { LibraryItemId = 1, BorrowerId = 3 }, null, null);

            // Assert: Queue positions are FIFO 1, 2, 3
            var res1 = await context.Reservations.FirstOrDefaultAsync(r => r.BorrowerId == 1 && r.LibraryItemId == 1);
            var res2 = await context.Reservations.FirstOrDefaultAsync(r => r.BorrowerId == 2 && r.LibraryItemId == 1);
            var res3 = await context.Reservations.FirstOrDefaultAsync(r => r.BorrowerId == 3 && r.LibraryItemId == 1);

            Assert.NotNull(res1);
            Assert.Equal(1, res1.QueuePosition);
            Assert.Equal(ReservationStatus.Pending, res1.Status);

            Assert.NotNull(res2);
            Assert.Equal(2, res2.QueuePosition);

            Assert.NotNull(res3);
            Assert.Equal(3, res3.QueuePosition);
        }

        [Fact]
        public async Task CancelReservation_RenumberSubsequentQueuePositions()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 10,
                Name = "Clean Code",
                LibraryCode = "BK-3001",
                Status = ItemStatus.Borrowed,
                Author = "Robert Martin",
                Genre = "Technology"
            };
            var p1 = new Borrower { Id = 10, FullName = "Patron One", CardNumber = "BW-301", IsActive = true };
            var p2 = new Borrower { Id = 20, FullName = "Patron Two", CardNumber = "BW-302", IsActive = true };
            var p3 = new Borrower { Id = 30, FullName = "Patron Three", CardNumber = "BW-303", IsActive = true };

            context.LibraryItems.Add(book);
            context.Borrowers.AddRange(p1, p2, p3);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            // Create 3 reservations
            await controller.Create(new Reservation { LibraryItemId = 10, BorrowerId = 10 }, null, null);
            await controller.Create(new Reservation { LibraryItemId = 10, BorrowerId = 20 }, null, null);
            await controller.Create(new Reservation { LibraryItemId = 10, BorrowerId = 30 }, null, null);

            var res2 = await context.Reservations.FirstAsync(r => r.BorrowerId == 20);

            // Act: Cancel patron 2 who was at position #2
            var result = await controller.Cancel(res2.ReservationId);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            var updatedRes2 = await context.Reservations.FindAsync(res2.ReservationId);
            Assert.NotNull(updatedRes2);
            Assert.Equal(ReservationStatus.Cancelled, updatedRes2.Status);

            var res1 = await context.Reservations.FirstAsync(r => r.BorrowerId == 10);
            Assert.Equal(1, res1.QueuePosition); // Remains #1

            var res3 = await context.Reservations.FirstAsync(r => r.BorrowerId == 30);
            Assert.Equal(2, res3.QueuePosition); // Re-numbered from #3 to #2!
        }

        [Fact]
        public async Task CreateReservation_RejectsWhenItemIsAvailable()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 50,
                Name = "Design Patterns",
                LibraryCode = "BK-4001",
                Status = ItemStatus.Available, // On shelf!
                Author = "GoF",
                Genre = "Technology"
            };
            var patron = new Borrower { Id = 50, FullName = "Diana Prince", CardNumber = "BW-401", IsActive = true };

            context.LibraryItems.Add(book);
            context.Borrowers.Add(patron);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            // Act: Attempt to place reservation on an Available item
            var result = await controller.Create(new Reservation { LibraryItemId = 50, BorrowerId = 50 }, null, null);

            // Assert
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("currently Available")));

            var count = await context.Reservations.CountAsync();
            Assert.Equal(0, count); // No reservation was saved
        }

        [Fact]
        public async Task CreateReservation_RejectsDuplicateActiveReservationForSameBorrower()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 99,
                Name = "Refactoring",
                LibraryCode = "BK-5001",
                Status = ItemStatus.Borrowed,
                Author = "Martin Fowler",
                Genre = "Technology"
            };
            var patron = new Borrower { Id = 99, FullName = "Clark Kent", CardNumber = "BW-501", IsActive = true };

            context.LibraryItems.Add(book);
            context.Borrowers.Add(patron);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            // 1st reservation succeeds
            await controller.Create(new Reservation { LibraryItemId = 99, BorrowerId = 99 }, null, null);
            Assert.True(controller.ModelState.IsValid);

            // Act: Attempt duplicate reservation for same patron & item
            controller.ModelState.Clear();
            var duplicateResult = await controller.Create(new Reservation { LibraryItemId = 99, BorrowerId = 99 }, null, null);

            // Assert: Rejected as duplicate
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("already has an active reservation")));

            var count = await context.Reservations.CountAsync(r => r.BorrowerId == 99 && r.LibraryItemId == 99);
            Assert.Equal(1, count); // Still only 1 reservation in DB
        }
    }
}
