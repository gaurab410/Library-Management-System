using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Controllers;
using LibraryManagementSystem.Controllers.Api;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Xunit;

namespace LibraryManagementSystem.Tests
{
    public class PublicSearchTests
    {
        private static (LibraryManagementSystemContext context, SqliteConnection connection) CreateSqliteContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<LibraryManagementSystemContext>()
                .UseSqlite(connection)
                .Options;

            var context = new LibraryManagementSystemContext(options);
            context.Database.EnsureCreated();

            return (context, connection);
        }

        private static async Task SeedSampleItems(LibraryManagementSystemContext context)
        {
            var branch = new Branch { BranchId = 1, Name = "Main" };
            context.Branches.Add(branch);
            context.LibraryItems.Add(new BookItem { Name = "Clean Code", LibraryCode = "BK-1", Author = "Robert Martin", Genre = "Tech", Status = ItemStatus.Available, BranchId = 1 });
            context.LibraryItems.Add(new BookItem { Name = "Refactoring", LibraryCode = "BK-2", Author = "Martin Fowler", Genre = "Tech", Status = ItemStatus.Borrowed, BranchId = 1 });
            context.LibraryItems.Add(new MusicItem { Name = "Thriller", LibraryCode = "MU-1", Artist = "Michael Jackson", Status = ItemStatus.Available, BranchId = 1 });
            context.LibraryItems.Add(new ToyItem { Name = "Lego Star Wars", LibraryCode = "TY-1", ToyType = "Building Blocks", Status = ItemStatus.Available, BranchId = 1 });
            await context.SaveChangesAsync();
        }

        [Theory]
        [InlineData("Book", "All", "Clean")]
        [InlineData("Book", "Available", "t")]
        [InlineData("Music", "Available", "Michael")]
        [InlineData("Toy", "All", "Blocks")]
        [InlineData("All", "Available", "t")]
        [InlineData("All", "All", "")]
        public async Task Search_PublicController_WithVariousFilters_ExecutesSuccessfully(string itemType, string availability, string search)
        {
            var (context, connection) = CreateSqliteContext();
            try
            {
                await SeedSampleItems(context);
                var controller = new PublicController(context);

                var result = await controller.Index(search: search, itemType: itemType, availability: availability) as ViewResult;
                Assert.NotNull(result);
                var model = Assert.IsType<PublicSearchViewModel>(result.Model);
                Assert.NotNull(model.Results);
            }
            finally
            {
                connection.Dispose();
            }
        }

        [Theory]
        [InlineData("book", "All", "Clean")]
        [InlineData("book", "Available", "t")]
        [InlineData("music", "Available", "Michael")]
        [InlineData("toy", "All", "Blocks")]
        public async Task Search_CatalogApiController_WithVariousFilters_ExecutesSuccessfully(string type, string availability, string search)
        {
            var (context, connection) = CreateSqliteContext();
            try
            {
                await SeedSampleItems(context);
                var controller = new CatalogApiController(context);

                var result = await controller.GetItems(type: type, availability: availability, search: search);
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                Assert.NotNull(okResult.Value);
            }
            finally
            {
                connection.Dispose();
            }
        }
    }
}
