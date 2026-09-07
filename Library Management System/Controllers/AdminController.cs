using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public AdminController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index(string? search, string? typeFilter, ItemStatus? statusFilter)
        {
            var query = _context.LibraryItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(term) ||
                                         i.LibraryCode.ToLower().Contains(term) ||
                                         (i.Description != null && i.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter) && typeFilter != "All")
            {
                query = typeFilter switch
                {
                    "Book" => query.OfType<BookItem>(),
                    "Music" => query.OfType<MusicItem>(),
                    "Toy" => query.OfType<ToyItem>(),
                    _ => query
                };
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(i => i.Status == statusFilter.Value);
            }

            ViewBag.Search = search;
            ViewBag.TypeFilter = typeFilter ?? "All";
            ViewBag.StatusFilter = statusFilter;

            var items = await query.OrderByDescending(i => i.Id).ToListAsync();
            return View(items);
        }

        // GET: Admin/CreateBook
        public IActionResult CreateBook()
        {
            var model = new ItemFormViewModel { ItemType = "Book", Status = ItemStatus.Available };
            return View(model);
        }

        // POST: Admin/CreateBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(ItemFormViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.LibraryCode) &&
                await _context.LibraryItems.AnyAsync(i => i.LibraryCode.ToLower() == model.LibraryCode.Trim().ToLower()))
            {
                ModelState.AddModelError("LibraryCode", "An item with this Library Code already exists.");
            }

            if (string.IsNullOrWhiteSpace(model.Author))
            {
                ModelState.AddModelError("Author", "Author is required for books.");
            }

            if (string.IsNullOrWhiteSpace(model.Genre))
            {
                ModelState.AddModelError("Genre", "Genre is required for books.");
            }

            if (ModelState.IsValid)
            {
                var book = new BookItem
                {
                    Name = model.Name,
                    Description = model.Description,
                    LibraryCode = model.LibraryCode.Trim().ToUpper(),
                    Status = model.Status,
                    Author = model.Author!,
                    Genre = model.Genre!,
                    ISBN = model.ISBN,
                    CreatedAt = DateTime.Now
                };

                _context.BookItems.Add(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Book '{book.Name}' created successfully with code {book.LibraryCode}.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/CreateMusic
        public IActionResult CreateMusic()
        {
            var model = new ItemFormViewModel { ItemType = "Music", Status = ItemStatus.Available, ReleaseYear = DateTime.Now.Year };
            return View(model);
        }

        // POST: Admin/CreateMusic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMusic(ItemFormViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.LibraryCode) &&
                await _context.LibraryItems.AnyAsync(i => i.LibraryCode.ToLower() == model.LibraryCode.Trim().ToLower()))
            {
                ModelState.AddModelError("LibraryCode", "An item with this Library Code already exists.");
            }

            if (string.IsNullOrWhiteSpace(model.Artist))
            {
                ModelState.AddModelError("Artist", "Artist is required for music items.");
            }

            if (ModelState.IsValid)
            {
                var music = new MusicItem
                {
                    Name = model.Name,
                    Description = model.Description,
                    LibraryCode = model.LibraryCode.Trim().ToUpper(),
                    Status = model.Status,
                    Artist = model.Artist!,
                    ReleaseYear = model.ReleaseYear,
                    Album = model.Album,
                    Format = model.Format,
                    CreatedAt = DateTime.Now
                };

                _context.MusicItems.Add(music);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Music item '{music.Name}' created successfully with code {music.LibraryCode}.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/CreateToy
        public IActionResult CreateToy()
        {
            var model = new ItemFormViewModel { ItemType = "Toy", Status = ItemStatus.Available };
            return View(model);
        }

        // POST: Admin/CreateToy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateToy(ItemFormViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.LibraryCode) &&
                await _context.LibraryItems.AnyAsync(i => i.LibraryCode.ToLower() == model.LibraryCode.Trim().ToLower()))
            {
                ModelState.AddModelError("LibraryCode", "An item with this Library Code already exists.");
            }

            if (ModelState.IsValid)
            {
                var toy = new ToyItem
                {
                    Name = model.Name,
                    Description = model.Description,
                    LibraryCode = model.LibraryCode.Trim().ToUpper(),
                    Status = model.Status,
                    ToyType = model.ToyType,
                    TargetAgeGroup = model.TargetAgeGroup,
                    Manufacturer = model.Manufacturer,
                    CreatedAt = DateTime.Now
                };

                _context.ToyItems.Add(toy);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Toy '{toy.Name}' created successfully with code {toy.LibraryCode}.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.LibraryItems.FindAsync(id);
            if (item == null) return NotFound();

            var model = new ItemFormViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                LibraryCode = item.LibraryCode,
                Status = item.Status,
                ItemType = item.ItemType
            };

            if (item is BookItem book)
            {
                model.Author = book.Author;
                model.Genre = book.Genre;
                model.ISBN = book.ISBN;
            }
            else if (item is MusicItem music)
            {
                model.Artist = music.Artist;
                model.ReleaseYear = music.ReleaseYear;
                model.Album = music.Album;
                model.Format = music.Format;
            }
            else if (item is ToyItem toy)
            {
                model.ToyType = toy.ToyType;
                model.TargetAgeGroup = toy.TargetAgeGroup;
                model.Manufacturer = toy.Manufacturer;
            }

            return View(model);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var item = await _context.LibraryItems.FindAsync(id);
            if (item == null) return NotFound();

            // Unique LibraryCode check excluding current item
            if (await _context.LibraryItems.AnyAsync(i => i.Id != id && i.LibraryCode.ToLower() == model.LibraryCode.Trim().ToLower()))
            {
                ModelState.AddModelError("LibraryCode", "Another item with this Library Code already exists.");
            }

            if (model.ItemType == "Book" && string.IsNullOrWhiteSpace(model.Author))
            {
                ModelState.AddModelError("Author", "Author is required for books.");
            }
            if (model.ItemType == "Book" && string.IsNullOrWhiteSpace(model.Genre))
            {
                ModelState.AddModelError("Genre", "Genre is required for books.");
            }
            if (model.ItemType == "Music" && string.IsNullOrWhiteSpace(model.Artist))
            {
                ModelState.AddModelError("Artist", "Artist is required for music.");
            }

            if (ModelState.IsValid)
            {
                item.Name = model.Name;
                item.Description = model.Description;
                item.LibraryCode = model.LibraryCode.Trim().ToUpper();
                item.Status = model.Status;

                if (item is BookItem book)
                {
                    book.Author = model.Author!;
                    book.Genre = model.Genre!;
                    book.ISBN = model.ISBN;
                }
                else if (item is MusicItem music)
                {
                    music.Artist = model.Artist!;
                    music.ReleaseYear = model.ReleaseYear;
                    music.Album = model.Album;
                    music.Format = model.Format;
                }
                else if (item is ToyItem toy)
                {
                    toy.ToyType = model.ToyType;
                    toy.TargetAgeGroup = model.TargetAgeGroup;
                    toy.Manufacturer = model.Manufacturer;
                }

                _context.Update(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Item '{item.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.LibraryItems
                .Include(i => i.BorrowTransactions)
                    .ThenInclude(t => t.Borrower)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        // POST: Admin/ChangeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, ItemStatus newStatus)
        {
            var item = await _context.LibraryItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Status of '{item.Name}' changed to {newStatus}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.LibraryItems
                .Include(i => i.BorrowTransactions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool markAsDestroyedOnly = true)
        {
            var item = await _context.LibraryItems
                .Include(i => i.BorrowTransactions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            // Requirement: "The Destroy status should preserve the item in the database rather than automatically permanently deleting it. This is important because borrowing history should remain available."
            if (markAsDestroyedOnly || item.BorrowTransactions.Any())
            {
                item.Status = ItemStatus.Destroy;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Item '{item.Name}' status updated to 'Destroy' (preserved in database to maintain borrowing history).";
            }
            else
            {
                _context.LibraryItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Item '{item.Name}' permanently deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
