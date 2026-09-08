namespace LibraryManagementSystem.Models
{
    // Model class used to pass error tracing details from HomeController to the Error view
    public class ErrorViewModel
    {
        // Holds the unique request identifier string (nullable)
        public string? RequestId { get; set; }

        // Evaluation property that returns true only if RequestId contains a valid string value
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
