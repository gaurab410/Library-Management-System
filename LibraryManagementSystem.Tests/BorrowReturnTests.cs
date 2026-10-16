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
    public class BorrowReturnTests
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
        public async Task BorrowItem_ChangesItemStatusToBorrowed()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 1,
                Name = "The Hobbit",
                LibraryCode = "BK-101",
                Status = ItemStatus.Available, // Starts Available on shelf
                Author = "J.R.R. Tolkien",
                Genre = "Fantasy"
            };

            var borrower = new Borrower
            {
                Id = 1,
                FullName = "Frodo Baggins",
                CardNumber = "BW-001",
                IsActive = true
            };

            context.LibraryItems.Add(book);
            context.Borrowers.Add(borrower);
            await context.SaveChangesAsync();

            var controller = CreateReceptionController(context);

            var model = new BorrowItemViewModel
            {
                LibraryItemId = book.Id,
                BorrowerId = borrower.Id,
                DaysToBorrow = 14
            };

            // Act
            var result = await controller.Borrow(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedItem = await context.LibraryItems.FindAsync(book.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal(ItemStatus.Borrowed, updatedItem.Status);

            var transaction = await context.BorrowTransactions
                .FirstOrDefaultAsync(t => t.LibraryItemId == book.Id && t.BorrowerId == borrower.Id);
            Assert.NotNull(transaction);
            Assert.Null(transaction.ReturnDate);
            Assert.True(transaction.DueDate > DateTime.Now.AddDays(13));
        }

        [Fact]
        public async Task ReturnItem_ChangesItemStatusToAvailable()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 2,
                Name = "1984",
                LibraryCode = "BK-102",
                Status = ItemStatus.Borrowed, // Starts Borrowed
                Author = "George Orwell",
                Genre = "Dystopian"
            };

            var borrower = new Borrower
            {
                Id = 2,
                FullName = "Winston Smith",
                CardNumber = "BW-002",
                IsActive = true
            };

            var transaction = new BorrowTransaction
            {
                Id = 10,
                LibraryItemId = book.Id,
                LibraryItem = book,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                BorrowDate = DateTime.Now.AddDays(-7),
                DueDate = DateTime.Now.AddDays(7), // On time
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
                IsFinePaid = false,
                Notes = "Returned in good condition"
            };

            // Act
            var result = await controller.Return(returnModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedItem = await context.LibraryItems.FindAsync(book.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal(ItemStatus.Available, updatedItem.Status);

            var updatedTx = await context.BorrowTransactions.FindAsync(transaction.Id);
            Assert.NotNull(updatedTx);
            Assert.NotNull(updatedTx.ReturnDate);
            Assert.Equal(0.00m, updatedTx.FineAmount);
        }

        [Fact]
        public async Task ReturnItem_WhenOverdue_CalculatesFineAccurately()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var book = new BookItem
            {
                Id = 3,
                Name = "Clean Architecture",
                LibraryCode = "BK-103",
                Status = ItemStatus.Borrowed,
                Author = "Robert C. Martin",
                Genre = "Technology"
            };

            var borrower = new Borrower
            {
                Id = 3,
                FullName = "Bob Martin",
                CardNumber = "BW-003",
                IsActive = true
            };

            // Due exactly 6 days ago -> Expected fine = 6 * 1.50 = 9.00
            int daysLate = 6;
            decimal expectedFine = daysLate * 1.50m; // 9.00m

            var transaction = new BorrowTransaction
            {
                Id = 20,
                LibraryItemId = book.Id,
                LibraryItem = book,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                BorrowDate = DateTime.Now.AddDays(-20),
                DueDate = DateTime.Now.Date.AddDays(-daysLate),
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
                IsFinePaid = true,
                Notes = "6 days late return"
            };

            // Act
            var result = await controller.Return(returnModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedTx = await context.BorrowTransactions.FindAsync(transaction.Id);
            Assert.NotNull(updatedTx);
            Assert.NotNull(updatedTx.ReturnDate);
            Assert.Equal(expectedFine, updatedTx.FineAmount);
            Assert.True(updatedTx.IsFinePaid);
        }
    }
}
