using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    /// <summary>
    /// Raw CSV record mapped directly from uploaded CSV file columns.
    /// </summary>
    public class CsvItemRecord
    {
        [Name("Type")]
        public string? Type { get; set; }

        [Name("Name")]
        public string? Name { get; set; }

        [Name("Description")]
        public string? Description { get; set; }

        [Name("LibraryCode")]
        public string? LibraryCode { get; set; }

        [Name("Author")]
        public string? Author { get; set; }

        [Name("Genre")]
        public string? Genre { get; set; }

        [Name("ISBN")]
        public string? ISBN { get; set; }

        [Name("Artist")]
        public string? Artist { get; set; }

        [Name("ReleaseYear")]
        public string? ReleaseYear { get; set; }

        [Name("Album")]
        public string? Album { get; set; }

        [Name("Format")]
        public string? Format { get; set; }

        [Name("ToyType")]
        public string? ToyType { get; set; }

        [Name("TargetAgeGroup")]
        public string? TargetAgeGroup { get; set; }

        [Name("Manufacturer")]
        public string? Manufacturer { get; set; }

        [Name("BranchId")]
        public string? BranchId { get; set; }
    }

    /// <summary>
    /// Represents a parsed and validated row from the uploaded CSV, used for preview.
    /// </summary>
    public class ImportRowPreviewModel
    {
        public int RowIndex { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string LibraryCode { get; set; } = string.Empty;

        // Book fields
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public string? ISBN { get; set; }

        // Music fields
        public string? Artist { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Album { get; set; }
        public string? Format { get; set; }

        // Toy fields
        public string? ToyType { get; set; }
        public string? TargetAgeGroup { get; set; }
        public string? Manufacturer { get; set; }

        // Multi-branch support
        public int? BranchId { get; set; }

        // Validation outcome
        public bool IsValid { get; set; }
        public bool IsDuplicate { get; set; }
        public string? ValidationMessage { get; set; }
    }

    /// <summary>
    /// View model for the preview stage displaying parsed items and status statistics.
    /// </summary>
    public class ImportPreviewViewModel
    {
        public string SessionKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalRows => Rows.Count;
        public int ValidCount => Rows.Count(r => r.IsValid);
        public int InvalidCount => Rows.Count(r => !r.IsValid && !r.IsDuplicate);
        public int DuplicateCount => Rows.Count(r => r.IsDuplicate);
        public List<ImportRowPreviewModel> Rows { get; set; } = new List<ImportRowPreviewModel>();
    }

    /// <summary>
    /// View model for the final import result page showing execution metrics.
    /// </summary>
    public class ImportResultViewModel
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
        public int TotalProcessed => Imported + Skipped + Errors;
    }
}
