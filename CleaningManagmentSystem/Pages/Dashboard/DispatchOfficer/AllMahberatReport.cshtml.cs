using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    public class AllMahberatReportModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public List<MahberatReport> Reports { get; set; } = new();

        [BindProperty]
        public string FilterType { get; set; } = "";

        [BindProperty]
        public string FilterDate { get; set; } = "";

        [BindProperty]
        public string SearchTerm { get; set; } = "";

        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public int TotalReports { get; set; }
        public int CompletedReports { get; set; }
        public int PendingReports { get; set; }

        public AllMahberatReportModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[AllMahberatReport] OnGet called");

            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole");

            Console.WriteLine($"[AllMahberatReport] Session - UserId: {userId}, UserName: {userName}, Role: {role}");

            if (userId == null || userId == 0)
            {
                Console.WriteLine("[AllMahberatReport] User not logged in, redirecting to Login");
                return RedirectToPage("/Login");
            }

            if (role?.ToLower() != "dispatch_officer")
            {
                Console.WriteLine($"[AllMahberatReport] User role {role} not authorized for this page");
                return RedirectToPage("/Login");
            }

            try
            {
                LoadReports();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AllMahberatReport] Error loading data: {ex.Message}");
                ErrorMessage = "Failed to load report data";
            }

            return Page();
        }

        public IActionResult OnPostFilter()
        {
            Console.WriteLine($"[AllMahberatReport] OnPostFilter called - Type: {FilterType}, Date: {FilterDate}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                LoadReports();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AllMahberatReport] Error filtering reports: {ex.Message}");
                ErrorMessage = "Failed to filter reports";
            }

            return Page();
        }

        public IActionResult OnPostSearch()
        {
            Console.WriteLine($"[AllMahberatReport] OnPostSearch called - Term: {SearchTerm}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                LoadReports();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AllMahberatReport] Error searching reports: {ex.Message}");
                ErrorMessage = "Failed to search reports";
            }

            return Page();
        }

        public IActionResult OnGetDownload(int id)
        {
            Console.WriteLine($"[AllMahberatReport] OnGetDownload called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var report = connection.QueryFirstOrDefault<MahberatReport>(
                    "SELECT * FROM mahberat_reports WHERE id = @Id", new { Id = id });

                if (report != null && !string.IsNullOrEmpty(report.FilePath))
                {
                    Console.WriteLine($"[AllMahberatReport] Downloading report: {report.FilePath}");
                    return Redirect(report.FilePath);
                }

                ErrorMessage = "Report file not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AllMahberatReport] Error downloading report: {ex.Message}");
                ErrorMessage = "Failed to download report";
            }

            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            Console.WriteLine($"[AllMahberatReport] OnPostDelete called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM mahberat_reports WHERE id = @Id", new { Id = id });

                Console.WriteLine($"[AllMahberatReport] Deleted report ID: {id}");
                SuccessMessage = "Report deleted successfully";
                LoadReports();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AllMahberatReport] Error deleting report: {ex.Message}");
                ErrorMessage = "Failed to delete report";
            }

            return Page();
        }

        private void LoadReports()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            var sql = "SELECT * FROM mahberat_reports WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(FilterType))
            {
                sql += " AND report_type = @ReportType";
                parameters.Add("ReportType", FilterType);
            }

            if (!string.IsNullOrEmpty(FilterDate))
            {
                sql += " AND DATE(created_at) = @FilterDate";
                parameters.Add("FilterDate", FilterDate);
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                sql += " AND (title LIKE @Search OR description LIKE @Search OR report_number LIKE @Search)";
                parameters.Add("Search", $"%{SearchTerm}%");
            }

            sql += " ORDER BY created_at DESC";

            Reports = connection.Query<MahberatReport>(sql, parameters).ToList();
            Console.WriteLine($"[AllMahberatReport] Loaded {Reports.Count} reports");
        }

        private void LoadStatistics()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            TotalReports = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM mahberat_reports") ?? 0;
            CompletedReports = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM mahberat_reports WHERE status = 'Completed'") ?? 0;
            PendingReports = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM mahberat_reports WHERE status IN ('Pending', 'In Progress')") ?? 0;
            
            Console.WriteLine($"[AllMahberatReport] Statistics - Total: {TotalReports}, Completed: {CompletedReports}, Pending: {PendingReports}");
        }
    }
}
