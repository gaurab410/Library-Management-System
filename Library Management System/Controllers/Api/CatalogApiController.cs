using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers.Api
{
    /// <summary>
    /// Public Read-Only REST API for Library Catalog and Operational Status.
    /// Exposes catalog items, inventory statistics, and operating branches to external consumers.
    /// </summary>
    [ApiController]
    [Route("api/catalog")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class CatalogApiController : ControllerBase
    {
        private readonly LibraryManagementSystemContext _context;

        public CatalogApiController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a filtered list of all active catalog items.
        /// </summary>
        /// <param name="type">Optional item type filter: Book, Music, Toy, or All.</param>
        /// <param name="branchId">Optional physical branch location ID filter.</param>
        /// <param name="availability">Optional status filter: Available, Borrowed, or All.</param>
        /// <param name="search">Optional search query matching name, code, author, or description.</param>
        /// <returns>A JSON array of catalog item objects.</returns>
        [HttpGet("items")]
        [ProducesResponseType(typeof(IEnumerable<CatalogItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetItems(
            [FromQuery] string? type = "All",
            [FromQuery] int? branchId = null,
            [FromQuery] string? availability = "All",
            [FromQuery] string? search = null)
        {
            var query = _context.LibraryItems
                .Where(i => i.Status != ItemStatus.Destroy)
                .AsQueryable();

            // 1. Filter by item type (Polymorphic TPH)
            if (!string.IsNullOrWhiteSpace(type) && !type.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = type.Trim().ToLower() switch
                {
                    "book" => query.Where(i => i is BookItem),
                    "music" => query.Where(i => i is MusicItem),
                    "toy" => query.Where(i => i is ToyItem),
                    _ => query
                };
            }

            // 2. Filter by availability status
            if (!string.IsNullOrWhiteSpace(availability) && !availability.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (availability.Equals("Available", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Status == ItemStatus.Available);
                }
                else if (availability.Equals("Borrowed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Status == ItemStatus.Borrowed);
                }
            }

            // 3. Search query filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    i.LibraryCode.ToLower().Contains(term) ||
                    (i.Description != null && i.Description.ToLower().Contains(term)) ||
                    (i is BookItem && ((BookItem)i).Author.ToLower().Contains(term)) ||
                    (i is MusicItem && ((MusicItem)i).Artist.ToLower().Contains(term)) ||
                    (i is ToyItem && ((ToyItem)i).ToyType != null && ((ToyItem)i).ToyType!.ToLower().Contains(term)));
            }

            var items = await query.OrderBy(i => i.Name).ToListAsync();
            var dtos = items.Select(MapToDto).ToList();

            // If branch filter specified, filter in memory
            if (branchId.HasValue && branchId.Value > 0)
            {
                dtos = dtos.Where(d => d.BranchId == branchId.Value).ToList();
            }

            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a single catalog item by its unique Library Code.
        /// </summary>
        /// <param name="libraryCode">The barcode or identifier (e.g. BK-1001).</param>
        /// <returns>The catalog item details or 404 Not Found.</returns>
        [HttpGet("items/{libraryCode}")]
        [ProducesResponseType(typeof(CatalogItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CatalogItemDto>> GetItemByCode(string libraryCode)
        {
            if (string.IsNullOrWhiteSpace(libraryCode))
            {
                return BadRequest(new { message = "Library Code cannot be empty." });
            }

            var code = libraryCode.Trim().ToUpper();
            var item = await _context.LibraryItems
                .Where(i => i.Status != ItemStatus.Destroy)
                .FirstOrDefaultAsync(i => i.LibraryCode == code);

            if (item == null)
            {
                return NotFound(new { message = $"Catalog item with code '{code}' was not found." });
            }

            return Ok(MapToDto(item));
        }

        /// <summary>
        /// Retrieves all public library branch locations and operating hours.
        /// </summary>
        /// <returns>A list of library branches.</returns>
        [HttpGet("branches")]
        [ProducesResponseType(typeof(IEnumerable<BranchDto>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<BranchDto>> GetBranches()
        {
            var branches = GetDefaultBranches();
            return Ok(branches);
        }

        /// <summary>
        /// Retrieves high-level operational statistics of catalog inventory.
        /// </summary>
        /// <returns>Total items, available items, borrowed items, and branch count.</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(CatalogStatsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CatalogStatsDto>> GetStats()
        {
            var activeItems = await _context.LibraryItems
                .Where(i => i.Status != ItemStatus.Destroy)
                .ToListAsync();

            var stats = new CatalogStatsDto
            {
                TotalItems = activeItems.Count,
                AvailableItems = activeItems.Count(i => i.Status == ItemStatus.Available),
                BorrowedItems = activeItems.Count(i => i.Status == ItemStatus.Borrowed),
                TotalBranches = GetDefaultBranches().Count
            };

            return Ok(stats);
        }

        private static CatalogItemDto MapToDto(LibraryItem item)
        {
            var dto = new CatalogItemDto
            {
                Id = item.Id,
                Name = item.Name,
                LibraryCode = item.LibraryCode,
                Type = item.ItemType,
                Status = item.Status.ToString(),
                Description = item.Description,
                BranchId = 1,
                BranchName = "City Central Library"
            };

            if (item is BookItem book)
            {
                dto.Author = book.Author;
                dto.Genre = book.Genre;
                dto.ISBN = book.ISBN;
            }
            else if (item is MusicItem music)
            {
                dto.Artist = music.Artist;
                dto.ReleaseYear = music.ReleaseYear;
                dto.Album = music.Album;
                dto.Format = music.Format;
            }
            else if (item is ToyItem toy)
            {
                dto.ToyType = toy.ToyType;
                dto.TargetAgeGroup = toy.TargetAgeGroup;
                dto.Manufacturer = toy.Manufacturer;
            }

            return dto;
        }

        private static List<BranchDto> GetDefaultBranches()
        {
            return new List<BranchDto>
            {
                new BranchDto
                {
                    BranchId = 1,
                    Name = "City Central Library",
                    Address = "100 Grand Library Plaza, Central City",
                    OpeningHours = "Mon - Fri: 08:00 - 20:00, Sat: 09:00 - 18:00",
                    IsActive = true
                },
                new BranchDto
                {
                    BranchId = 2,
                    Name = "Westside Branch",
                    Address = "450 West Academic Boulevard, Westside",
                    OpeningHours = "Mon - Fri: 09:00 - 18:00, Sat: 10:00 - 16:00",
                    IsActive = true
                },
                new BranchDto
                {
                    BranchId = 3,
                    Name = "Eastside Community Branch",
                    Address = "720 East Valley Road, Eastside",
                    OpeningHours = "Mon - Fri: 09:00 - 17:00",
                    IsActive = true
                }
            };
        }
    }

    #region API Data Transfer Objects (DTOs)

    public class CatalogItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LibraryCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        // Book Fields
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public string? ISBN { get; set; }

        // Music Fields
        public string? Artist { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Album { get; set; }
        public string? Format { get; set; }

        // Toy Fields
        public string? ToyType { get; set; }
        public string? TargetAgeGroup { get; set; }
        public string? Manufacturer { get; set; }
    }

    public class BranchDto
    {
        public int BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? OpeningHours { get; set; }
        public bool IsActive { get; set; }
    }

    public class CatalogStatsDto
    {
        public int TotalItems { get; set; }
        public int AvailableItems { get; set; }
        public int BorrowedItems { get; set; }
        public int TotalBranches { get; set; }
    }

    #endregion
}
