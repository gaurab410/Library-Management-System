using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [AllowAnonymous]
    public class PublicController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public PublicController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Public/Index (Public Search Portal)
        public async Task<IActionResult> Index(string? search, string itemType = "All", string availability = "All")
        {
            var query = _context.LibraryItems.AsQueryable();

            // Exclude destroyed items from public view if desired, or show all except Destroy
            query = query.Where(i => i.Status != ItemStatus.Destroy);

            // Filter by Item Type
            if (!string.IsNullOrWhiteSpace(itemType) && itemType != "All")
            {
                query = itemType switch
                {
                    "Book" => query.OfType<BookItem>(),
                    "Music" => query.OfType<MusicItem>(),
                    "Toy" => query.OfType<ToyItem>(),
                    _ => query
                };
            }

            // Filter by Availability
            if (!string.IsNullOrWhiteSpace(availability) && availability != "All")
            {
                if (availability == "Available")
                {
                    query = query.Where(i => i.Status == ItemStatus.Available);
                }
                else if (availability == "Borrowed")
                {
                    query = query.Where(i => i.Status == ItemStatus.Borrowed);
                }
            }

            // Search Term filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    i.LibraryCode.ToLower().Contains(term) ||
                    (i.Description != null && i.Description.ToLower().Contains(term)) ||
                    (i is BookItem && ((BookItem)i).Author.ToLower().Contains(term)) ||
                    (i is MusicItem && ((MusicItem)i).Artist.ToLower().Contains(term)) ||
                    (i is ToyItem && ((ToyItem)i).ToyType != null && ((ToyItem)i).ToyType!.ToLower().Contains(term))
                );
            }

            var results = await query.OrderBy(i => i.Name).ToListAsync();

            var viewModel = new PublicSearchViewModel
            {
                SearchTerm = search,
                SelectedItemType = itemType,
                SelectedAvailability = availability,
                Results = results
            };

            return View(viewModel);
        }
    }
}
