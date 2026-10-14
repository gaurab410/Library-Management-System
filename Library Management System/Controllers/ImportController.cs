using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImportController : Controller
    {
        private readonly LibraryManagementSystemContext _context;
        private readonly ILogger<ImportController> _logger;

        public ImportController(LibraryManagementSystemContext context, ILogger<ImportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Import
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Import/DownloadTemplate
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Type,Name,Description,LibraryCode,Author,Genre,ISBN,Artist,ReleaseYear,Album,Format,ToyType,TargetAgeGroup,Manufacturer,BranchId");
            builder.AppendLine("Book,To Kill a Mockingbird,Classic novel about justice,BK-5001,Harper Lee,Fiction,9780061120084,,,,,,,1");
            builder.AppendLine("Music,Abbey Road,Iconic Beatles studio album,MU-5001,,,,The Beatles,1969,Abbey Road,Vinyl,,,,1");
            builder.AppendLine("Toy,LEGO City Fire Station,Interactive building bricks set,TY-5001,,,,,,,,,Building,6-12,LEGO,1");

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(bytes, "text/csv", "library_items_template.csv");
        }

        // GET: /Import/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        // POST: /Import/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a non-empty CSV file to upload.");
                return View();
            }

            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("file", "Invalid file extension. Please select a .csv file.");
                return View();
            }

            var previewRows = new List<ImportRowPreviewModel>();

            try
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    TrimOptions = TrimOptions.Trim,
                    IgnoreBlankLines = true,
                    PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? string.Empty
                };

                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<CsvItemRecord>().ToList();

                if (!records.Any())
                {
                    ModelState.AddModelError("file", "The uploaded CSV file does not contain any data rows.");
                    return View();
                }

                // Query existing LibraryCodes in the database (case-insensitive)
                var existingDbCodes = (await _context.LibraryItems
                    .Select(i => i.LibraryCode.Trim().ToLower())
                    .ToListAsync())
                    .ToHashSet();

                // Track codes within the CSV to detect intra-file duplicates
                var seenCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                int rowIndex = 1; // Row 1 is the header line
                foreach (var record in records)
                {
                    rowIndex++;
                    var errors = new List<string>();
                    bool isDuplicate = false;

                    var rawType = (record.Type ?? string.Empty).Trim();
                    var name = (record.Name ?? string.Empty).Trim();
                    var code = (record.LibraryCode ?? string.Empty).Trim();
                    var desc = string.IsNullOrWhiteSpace(record.Description) ? null : record.Description.Trim();
                    var author = string.IsNullOrWhiteSpace(record.Author) ? null : record.Author.Trim();
                    var genre = string.IsNullOrWhiteSpace(record.Genre) ? null : record.Genre.Trim();
                    var isbn = string.IsNullOrWhiteSpace(record.ISBN) ? null : record.ISBN.Trim();
                    var artist = string.IsNullOrWhiteSpace(record.Artist) ? null : record.Artist.Trim();
                    var album = string.IsNullOrWhiteSpace(record.Album) ? null : record.Album.Trim();
                    var format = string.IsNullOrWhiteSpace(record.Format) ? null : record.Format.Trim();
                    var toyType = string.IsNullOrWhiteSpace(record.ToyType) ? null : record.ToyType.Trim();
                    var ageGroup = string.IsNullOrWhiteSpace(record.TargetAgeGroup) ? null : record.TargetAgeGroup.Trim();
                    var manufacturer = string.IsNullOrWhiteSpace(record.Manufacturer) ? null : record.Manufacturer.Trim();

                    // Parse ReleaseYear
                    int? releaseYear = null;
                    if (!string.IsNullOrWhiteSpace(record.ReleaseYear))
                    {
                        if (int.TryParse(record.ReleaseYear.Trim(), out var parsedYear) && parsedYear >= 1800 && parsedYear <= 2100)
                        {
                            releaseYear = parsedYear;
                        }
                        else
                        {
                            errors.Add("ReleaseYear must be a 4-digit year between 1800 and 2100");
                        }
                    }

                    // Parse BranchId
                    int? branchId = null;
                    if (!string.IsNullOrWhiteSpace(record.BranchId))
                    {
                        if (int.TryParse(record.BranchId.Trim(), out var parsedBranch))
                        {
                            branchId = parsedBranch;
                        }
                        else
                        {
                            errors.Add("BranchId must be an integer");
                        }
                    }

                    // 1. Validate Type (Book, Music, Toy)
                    string normalizedType = string.Empty;
                    if (string.Equals(rawType, "Book", StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedType = "Book";
                    }
                    else if (string.Equals(rawType, "Music", StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedType = "Music";
                    }
                    else if (string.Equals(rawType, "Toy", StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedType = "Toy";
                    }
                    else
                    {
                        errors.Add(string.IsNullOrWhiteSpace(rawType)
                            ? "Item Type is required"
                            : $"Invalid Type '{rawType}'. Allowed values: Book, Music, Toy");
                    }

                    // 2. Validate Name
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add("Item Name is required");
                    }

                    // 3. Validate LibraryCode & uniqueness
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        errors.Add("Library Code is required");
                    }
                    else
                    {
                        var lowerCode = code.ToLowerInvariant();
                        if (existingDbCodes.Contains(lowerCode))
                        {
                            isDuplicate = true;
                            errors.Add($"Library Code '{code}' already exists in database");
                        }
                        else if (seenCodesInFile.Contains(lowerCode))
                        {
                            isDuplicate = true;
                            errors.Add($"Duplicate Library Code '{code}' within uploaded CSV");
                        }
                        else
                        {
                            seenCodesInFile.Add(lowerCode);
                        }
                    }

                    // 4. Type-specific validations
                    if (normalizedType == "Book")
                    {
                        if (string.IsNullOrWhiteSpace(author))
                        {
                            errors.Add("Author is required for Book items");
                        }
                        if (string.IsNullOrWhiteSpace(genre))
                        {
                            errors.Add("Genre is required for Book items");
                        }
                    }
                    else if (normalizedType == "Music")
                    {
                        if (string.IsNullOrWhiteSpace(artist))
                        {
                            errors.Add("Artist is required for Music items");
                        }
                        if (!releaseYear.HasValue)
                        {
                            releaseYear = DateTime.Now.Year;
                        }
                    }
                    else if (normalizedType == "Toy")
                    {
                        // ToyType is optional as specified
                    }

                    bool isValid = errors.Count == 0;

                    previewRows.Add(new ImportRowPreviewModel
                    {
                        RowIndex = rowIndex,
                        Type = string.IsNullOrEmpty(normalizedType) ? rawType : normalizedType,
                        Name = name,
                        Description = desc,
                        LibraryCode = code,
                        Author = author,
                        Genre = genre,
                        ISBN = isbn,
                        Artist = artist,
                        ReleaseYear = releaseYear,
                        Album = album,
                        Format = format,
                        ToyType = toyType,
                        TargetAgeGroup = ageGroup,
                        Manufacturer = manufacturer,
                        BranchId = branchId,
                        IsValid = isValid,
                        IsDuplicate = isDuplicate,
                        ValidationMessage = errors.Count > 0 ? string.Join("; ", errors) : "Valid"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CSV file: {FileName}", file.FileName);
                ModelState.AddModelError("file", $"Error processing CSV file: {ex.Message}");
                return View();
            }

            var sessionKey = Guid.NewGuid().ToString("N");
            var json = JsonSerializer.Serialize(previewRows);
            TempData[sessionKey] = json;
            TempData.Keep(sessionKey);

            var previewModel = new ImportPreviewViewModel
            {
                SessionKey = sessionKey,
                FileName = Path.GetFileName(file.FileName),
                Rows = previewRows
            };

            return View("Preview", previewModel);
        }

        // POST: /Import/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string sessionKey)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                TempData["ErrorMessage"] = "Import session key is missing. Please upload the file again.";
                return RedirectToAction("Upload");
            }

            var json = TempData[sessionKey]?.ToString();
            if (string.IsNullOrEmpty(json))
            {
                TempData["ErrorMessage"] = "Import preview session has expired or already been confirmed. Please re-upload your file.";
                return RedirectToAction("Upload");
            }

            List<ImportRowPreviewModel>? rows;
            try
            {
                rows = JsonSerializer.Deserialize<List<ImportRowPreviewModel>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize import preview data.");
                TempData["ErrorMessage"] = "Unable to deserialize import preview data. Please re-upload.";
                return RedirectToAction("Upload");
            }

            if (rows == null || !rows.Any())
            {
                TempData["ErrorMessage"] = "No rows found to import.";
                return RedirectToAction("Upload");
            }

            var existingDbCodes = (await _context.LibraryItems
                .Select(i => i.LibraryCode.Trim().ToLower())
                .ToListAsync())
                .ToHashSet();

            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var branchProp = typeof(LibraryItem).GetProperty("BranchId");

            foreach (var row in rows)
            {
                if (!row.IsValid)
                {
                    if (row.IsDuplicate)
                    {
                        skippedCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                    continue;
                }

                var normalizedCode = row.LibraryCode.Trim().ToLowerInvariant();
                if (existingDbCodes.Contains(normalizedCode))
                {
                    skippedCount++;
                    continue;
                }

                LibraryItem item;
                if (string.Equals(row.Type, "Book", StringComparison.OrdinalIgnoreCase))
                {
                    item = new BookItem
                    {
                        Name = row.Name.Trim(),
                        Description = row.Description?.Trim(),
                        LibraryCode = row.LibraryCode.Trim(),
                        Status = ItemStatus.Available,
                        CreatedAt = DateTime.Now,
                        Author = row.Author?.Trim() ?? string.Empty,
                        Genre = row.Genre?.Trim() ?? string.Empty,
                        ISBN = row.ISBN?.Trim()
                    };
                }
                else if (string.Equals(row.Type, "Music", StringComparison.OrdinalIgnoreCase))
                {
                    item = new MusicItem
                    {
                        Name = row.Name.Trim(),
                        Description = row.Description?.Trim(),
                        LibraryCode = row.LibraryCode.Trim(),
                        Status = ItemStatus.Available,
                        CreatedAt = DateTime.Now,
                        Artist = row.Artist?.Trim() ?? string.Empty,
                        ReleaseYear = row.ReleaseYear ?? DateTime.Now.Year,
                        Album = row.Album?.Trim(),
                        Format = row.Format?.Trim()
                    };
                }
                else if (string.Equals(row.Type, "Toy", StringComparison.OrdinalIgnoreCase))
                {
                    item = new ToyItem
                    {
                        Name = row.Name.Trim(),
                        Description = row.Description?.Trim(),
                        LibraryCode = row.LibraryCode.Trim(),
                        Status = ItemStatus.Available,
                        CreatedAt = DateTime.Now,
                        ToyType = row.ToyType?.Trim(),
                        TargetAgeGroup = row.TargetAgeGroup?.Trim(),
                        Manufacturer = row.Manufacturer?.Trim()
                    };
                }
                else
                {
                    errorCount++;
                    continue;
                }

                // If multi-branch property exists on LibraryItem, set BranchId
                if (branchProp != null && branchProp.CanWrite && row.BranchId.HasValue)
                {
                    branchProp.SetValue(item, row.BranchId.Value);
                }

                _context.LibraryItems.Add(item);
                existingDbCodes.Add(normalizedCode);
                importedCount++;
            }

            if (importedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Import confirmed: {Imported} items imported, {Skipped} items skipped, {Errors} errors.",
                importedCount, skippedCount, errorCount);

            TempData["SuccessMessage"] = $"Bulk import complete! {importedCount} items imported, {skippedCount} skipped, {errorCount} errors.";

            return RedirectToAction("Result", new { imported = importedCount, skipped = skippedCount, errors = errorCount });
        }

        // GET: /Import/Result
        [HttpGet]
        public IActionResult Result(int imported, int skipped, int errors)
        {
            var model = new ImportResultViewModel
            {
                Imported = imported,
                Skipped = skipped,
                Errors = errors
            };
            return View(model);
        }
    }
}
