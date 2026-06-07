using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class PdfReportsModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public string ReportType { get; set; } = "";

        [BindProperty]
        public DateTime? StartDate { get; set; }

        [BindProperty]
        public DateTime? EndDate { get; set; }

        [BindProperty]
        public string SearchQuery { get; set; } = "";

        public List<ReportItem> AvailableReports { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public PdfReportsModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[PdfReports] OnGet called");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[PdfReports] No session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            LoadAvailableReports();
            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine($"[PdfReports] OnPost called with action");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            var action = Request.Form["action"];
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                if (action == "generate")
                {
                    GenerateReport(connection);
                    SuccessMessage = "Report generated successfully!";
                }
                else if (action == "download")
                {
                    DownloadReport(connection);
                    SuccessMessage = "Report download started!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PdfReports] Database error: {ex.Message}");
                ErrorMessage = "Database error occurred.";
            }

            LoadAvailableReports();
            return Page();
        }

        private void LoadAvailableReports()
        {
            try
            {
                AvailableReports = new List<ReportItem>
                {
                    new ReportItem { Id = 1, Name = "User Report", Description = "List of all registered users", Category = "Users" },
                    new ReportItem { Id = 2, Name = "Receipt Report", Description = "All receipt transactions", Category = "Finance" },
                    new ReportItem { Id = 3, Name = "Payroll Report", Description = "Employee payroll records", Category = "HR" },
                    new ReportItem { Id = 4, Name = "Capital Report", Description = "Capital transactions summary", Category = "Finance" },
                    new ReportItem { Id = 5, Name = "Checklist Report", Description = "All checklist items", Category = "Tasks" },
                    new ReportItem { Id = 6, Name = "Outsource Company Report", Description = "Outsource company list", Category = "Companies" },
                    new ReportItem { Id = 7, Name = "Private Company Report", Description = "Private cleaning companies", Category = "Companies" },
                    new ReportItem { Id = 8, Name = "Post Report", Description = "All posts and announcements", Category = "Content" }
                };
                
                Console.WriteLine($"[PdfReports] Loaded {AvailableReports.Count} available reports");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PdfReports] Load error: {ex.Message}");
                ErrorMessage = "Error loading reports.";
            }
        }

        private void GenerateReport(MySqlConnection connection)
        {
            Console.WriteLine($"[PdfReports] Generating report: {ReportType}");
            Console.WriteLine($"[PdfReports] Date range: {StartDate} to {EndDate}");
        }

        private void DownloadReport(MySqlConnection connection)
        {
            Console.WriteLine($"[PdfReports] Downloading report: {ReportType}");
        }
    }

    public class ReportItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
    }
}