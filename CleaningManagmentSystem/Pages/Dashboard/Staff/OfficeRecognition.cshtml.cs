using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class OfficeRecognitionModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string EmployeeName { get; set; } = "";

        [BindProperty]
        public string AwardTitle { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public DateTime AwardDate { get; set; }

        [BindProperty]
        public string AwardType { get; set; } = "";

        public List<OfficeRecognition> Recognitions { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public OfficeRecognitionModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadRecognitions();
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
                    "INSERT INTO office_recognitions (employee_name, award_title, description, award_date, award_type) VALUES (@EmployeeName, @AwardTitle, @Description, @AwardDate, @AwardType)",
                    new { EmployeeName, AwardTitle, Description, AwardDate, AwardType });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadRecognitions();
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
                    "UPDATE office_recognitions SET employee_name = @EmployeeName, award_title = @AwardTitle, description = @Description, award_date = @AwardDate, award_type = @AwardType WHERE id = @Id",
                    new { Id, EmployeeName, AwardTitle, Description, AwardDate, AwardType });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadRecognitions();
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
                connection.Execute("DELETE FROM office_recognitions WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadRecognitions();
            return Page();
        }

        private void LoadRecognitions()
        {
            using var connection = new MySqlConnection(_connectionString);
            Recognitions = connection.Query<OfficeRecognition>("SELECT * FROM office_recognitions ORDER BY award_date DESC").ToList();
        }
    }

    public class OfficeRecognition
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string AwardTitle { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime AwardDate { get; set; }
        public string AwardType { get; set; } = "";
    }
}