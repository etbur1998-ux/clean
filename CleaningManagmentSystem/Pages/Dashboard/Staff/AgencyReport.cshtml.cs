using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class AgencyReportModel : PageModel
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Report title is required")]
        [StringLength(200)]
        public string ReportTitle { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Report type is required")]
        public string ReportType { get; set; } = "";
        
        [BindProperty]
        public string? AgencyName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Report period is required")]
        public string Period { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Description is required")]
        public string Summary { get; set; } = "";

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [BindProperty]
        public string Status { get; set; } = "Pending";

        [BindProperty]
        public IFormFile? AttachmentFile { get; set; }

        public List<AgencyReport> AgencyReports { get; set; } = new();
        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public AgencyReportModel(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _env = env;
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            LoadReports();
            return Page();
        }

        public IActionResult OnPostAdd()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                LoadReports();
                return Page();
            }

            try
            {
                string filePath = null;
                if (AttachmentFile != null && AttachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"RPT_{Guid.NewGuid()}_{AttachmentFile.FileName}";
                    filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        AttachmentFile.CopyTo(stream);
                    }
                    filePath = $"/uploads/{uniqueFileName}";
                }

                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    @"INSERT INTO agency_reports 
                        (report_number, agency_name, report_type, period, summary, 
                         file_path, generated_by, generated_at, status, created_at)
                      VALUES 
                        (@ReportNumber, @AgencyName, @ReportType, @Period, @Summary,
                         @FilePath, @GeneratedBy, @GeneratedAt, @Status, NOW())",
                    new {
                        ReportNumber = $"RPT-{DateTime.Now:yyyyMMddHHmmss}",
                        AgencyName = ReportTitle,
                        ReportType,
                        Period,
                        Summary,
                        FilePath = filePath,
                        GeneratedBy = userName,
                        GeneratedAt = ReportDate,
                        Status
                    });

                Message = "Report generated successfully!";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"Error generating report: {ex.Message}";
                IsSuccess = false;
            }

            LoadReports();
            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var report = connection.QueryFirstOrDefault<AgencyReport>(
                    "SELECT file_path FROM agency_reports WHERE id = @Id", new { Id = id });

                if (report != null && !string.IsNullOrEmpty(report.FilePath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, report.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                connection.Execute("DELETE FROM agency_reports WHERE id = @Id", new { Id = id });
                Message = "Report deleted successfully!";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"Error deleting report: {ex.Message}";
                IsSuccess = false;
            }

            LoadReports();
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                LoadReports();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var existingFilePath = connection.QueryFirstOrDefault<string>(
                    "SELECT file_path FROM agency_reports WHERE id = @Id", new { Id });

                string newFilePath = existingFilePath;
                if (AttachmentFile != null && AttachmentFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingFilePath))
                    {
                        var oldFullPath = Path.Combine(_env.WebRootPath, existingFilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFullPath))
                            System.IO.File.Delete(oldFullPath);
                    }

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"RPT_{Guid.NewGuid()}_{AttachmentFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        AttachmentFile.CopyTo(stream);
                    }
                    newFilePath = $"/uploads/{uniqueFileName}";
                }

                connection.Execute(
                    @"UPDATE agency_reports SET 
                        report_title = @ReportTitle,
                        report_type = @ReportType,
                        period = @Period,
                        summary = @Summary,
                        file_path = @FilePath,
                        status = @Status,
                        generated_at = @GeneratedAt
                      WHERE id = @Id",
                    new
                    {
                        ReportTitle,
                        ReportType,
                        Period,
                        Summary,
                        FilePath = newFilePath,
                        Status,
                        GeneratedAt = ReportDate,
                        Id
                    });

                Message = "Report updated successfully!";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"Error updating report: {ex.Message}";
                IsSuccess = false;
            }

            LoadReports();
            return Page();
        }

        private void LoadReports()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                AgencyReports = connection.Query<AgencyReport>(
                    @"SELECT 
                        id AS Id,
                        report_title AS ReportTitle,
                        report_type AS ReportType,
                        agency_name AS AgencyName,
                        period AS Period,
                        summary AS Summary,
                        file_path AS FilePath,
                        generated_by AS GeneratedBy,
                        generated_at AS GeneratedAt,
                        status AS Status,
                        created_at AS CreatedAt
                    FROM agency_reports 
                    ORDER BY generated_at DESC").ToList();
            }
            catch (Exception ex)
            {
                Message = $"Error loading reports: {ex.Message}";
                AgencyReports = new List<AgencyReport>();
            }
        }
    }

    public class AgencyReport
    {
        public int Id { get; set; }
        public string ReportTitle { get; set; } = "";
        public string ReportType { get; set; } = "";
        public string AgencyName { get; set; } = "";
        public string Period { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Description { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string GeneratedBy { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
