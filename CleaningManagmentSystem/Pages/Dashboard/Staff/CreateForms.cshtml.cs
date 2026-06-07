using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CleaningManagmentSystem.Models;
using System.ComponentModel.DataAnnotations;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    [IgnoreAntiforgeryToken]
    public class CreateFormsModel : PageModel
    {
        private readonly string _connectionString;

        public CreateFormsModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<RecentForm> RecentForms { get; set; } = new();

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public class RecentForm
        {
            public string FormType { get; set; } = "";
            public string Title { get; set; } = "";
            public DateTime CreatedDate { get; set; }
            public string Status { get; set; } = "";
            public string EditUrl { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();

            if (string.IsNullOrEmpty(userName) || userRole != "staff")
            {
                return RedirectToPage("/Login");
            }

            LoadRecentForms();
            return Page();
        }

        private void LoadRecentForms()
        {
            try
            {
                using var connection = new MySqlConnector.MySqlConnection(_connectionString);

                // Load recent staff receipts
                var receipts = connection.Query(@"
                    SELECT plate_number, receipt_date, kilogram, status, id
                    FROM staff_receipts 
                    ORDER BY registered_at DESC 
                    LIMIT 5").ToList();

                foreach (var r in receipts)
                {
                    RecentForms.Add(new RecentForm
                    {
                        FormType = "Receipt",
                        Title = $"Receipt for {r.plate_number} - {r.kilogram}kg",
                        CreatedDate = r.receipt_date,
                        Status = r.status ?? "Registered",
                        EditUrl = $"/Dashboard/Staff/RegisterReceiptStandalone"
                    });
                }

                // Load recent agency reports
                var reports = connection.Query(@"
                    SELECT report_title, created_at, status, id
                    FROM agency_reports 
                    ORDER BY created_at DESC 
                    LIMIT 5").ToList();

                foreach (var r in reports)
                {
                    RecentForms.Add(new RecentForm
                    {
                        FormType = "Agency Report",
                        Title = r.report_title ?? "Agency Report",
                        CreatedDate = r.created_at,
                        Status = r.status ?? "Active",
                        EditUrl = $"/Dashboard/Staff/AgencyReport"
                    });
                }

                // Sort by creation date descending
                RecentForms = RecentForms.OrderByDescending(f => f.CreatedDate).Take(10).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Forms Load Error] {ex.Message}");
                // If table doesn't exist, return empty list
                RecentForms = new List<RecentForm>();
            }
        }
    }
}
