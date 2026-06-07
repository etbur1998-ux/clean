using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;
using System.Text.Json;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class YakaReportModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string ReportTitle { get; set; } = "";

        [BindProperty]
        public string ReportDescription { get; set; } = "";

        [BindProperty]
        public string Category { get; set; } = "Performance";

        [BindProperty]
        public string Period { get; set; } = "Monthly";

        [BindProperty]
        public string YakaZone { get; set; } = "";

        [BindProperty]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [BindProperty]
        public string AttachmentUrl { get; set; } = "";

        [BindProperty]
        public string Status { get; set; } = "Pending";

        public List<CleaningManagmentSystem.Models.YakaReport> YakaReports { get; set; } = new();
        public string ReportDataJson { get; set; } = "[]";
        public string ErrorMessage { get; set; } = "";

        public YakaReportModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadReports();
            return Page();
        }

        public IActionResult OnPostAdd()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    @"INSERT INTO yaka_reports 
                        (report_number, title, category, description, period, file_path, generated_by, generated_at, created_at)
                      VALUES 
                        (@ReportNumber, @Title, @Category, @Description, @Period, @FilePath, @GeneratedBy, @GeneratedAt, NOW())",
                    new 
                    {
                        ReportNumber = $"YKA-{DateTime.Now:yyyyMMddHHmmss}",
                        Title = ReportTitle,
                        Category = string.IsNullOrWhiteSpace(Category) ? "Performance" : Category,
                        Description = ReportDescription,
                        Period = string.IsNullOrWhiteSpace(Period) ? "Monthly" : Period,
                        FilePath = AttachmentUrl,
                        GeneratedBy = userId.Value,
                        GeneratedAt = ReportDate
                    });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadReports();
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    @"UPDATE yaka_reports SET 
                        title = @Title,
                        category = @Category,
                        description = @Description,
                        period = @Period,
                        file_path = @FilePath,
                        generated_at = @GeneratedAt
                      WHERE id = @Id",
                    new 
                    {
                        Id,
                        Title = ReportTitle,
                        Category = string.IsNullOrWhiteSpace(Category) ? "Performance" : Category,
                        Description = ReportDescription,
                        Period = string.IsNullOrWhiteSpace(Period) ? "Monthly" : Period,
                        FilePath = AttachmentUrl,
                        GeneratedAt = ReportDate
                    });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadReports();
            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM yaka_reports WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadReports();
            return Page();
        }

        private void LoadReports()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                YakaReports = connection.Query<CleaningManagmentSystem.Models.YakaReport>(@"
                    SELECT 
                        id AS Id,
                        report_number AS ReportNumber,
                        title AS Title,
                        category AS Category,
                        description AS Description,
                        period AS Period,
                        file_path AS FilePath,
                        generated_by AS GeneratedBy,
                        generated_at AS GeneratedAt,
                        created_at AS CreatedAt
                    FROM yaka_reports
                    ORDER BY generated_at DESC").ToList();

                ReportDataJson = BuildReportDataJson();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                YakaReports = new List<CleaningManagmentSystem.Models.YakaReport>();
                ReportDataJson = "[]";
            }
        }

        private string BuildReportDataJson()
        {
            var rows = YakaReports.Select(report => new
            {
                category = NormalizeCategory(report.Category),
                subcategory = string.IsNullOrWhiteSpace(report.Period) ? "Monthly" : report.Period,
                code = string.IsNullOrWhiteSpace(report.ReportNumber) ? $"YKA-{report.Id:D4}" : report.ReportNumber,
                name = string.IsNullOrWhiteSpace(report.Title) ? "Untitled Yaka Report" : report.Title,
                qty = 1,
                rate = string.IsNullOrWhiteSpace(report.FilePath) ? 80m : 100m,
                catKey = NormalizeCategory(report.Category),
                status = string.IsNullOrWhiteSpace(report.FilePath) ? "Pending" : "Approved",
                date = report.GeneratedAt != default ? report.GeneratedAt.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd")
            }).ToList();

            return JsonSerializer.Serialize(rows, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private static string NormalizeCategory(string? category)
        {
            var normalized = (category ?? string.Empty).Trim().ToLowerInvariant();

            if (normalized.Contains("hr") || normalized.Contains("attendance"))
                return "HR";

            if (normalized.Contains("oper") || normalized.Contains("equipment"))
                return "Operations";

            return "Performance";
        }
    }
}