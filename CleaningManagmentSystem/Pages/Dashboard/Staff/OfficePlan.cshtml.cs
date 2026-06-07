using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class OfficePlanModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string FloorName { get; set; } = "";

        [BindProperty]
        public string PlanDescription { get; set; } = "";

        [BindProperty]
        public string ImageUrl { get; set; } = "";

        [BindProperty]
        public int FloorNumber { get; set; }

        public List<OfficePlan> OfficePlans { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public OfficePlanModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadOfficePlans();
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
                    "INSERT INTO office_plans (floor_name, plan_description, image_url, floor_number) VALUES (@FloorName, @PlanDescription, @ImageUrl, @FloorNumber)",
                    new { FloorName, PlanDescription, ImageUrl, FloorNumber });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadOfficePlans();
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
                    "UPDATE office_plans SET floor_name = @FloorName, plan_description = @PlanDescription, image_url = @ImageUrl, floor_number = @FloorNumber WHERE id = @Id",
                    new { Id, FloorName, PlanDescription, ImageUrl, FloorNumber });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadOfficePlans();
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
                connection.Execute("DELETE FROM office_plans WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadOfficePlans();
            return Page();
        }

        private void LoadOfficePlans()
        {
            using var connection = new MySqlConnection(_connectionString);
            OfficePlans = connection.Query<OfficePlan>("SELECT * FROM office_plans ORDER BY floor_number ASC").ToList();
        }
    }

    public class OfficePlan
    {
        public int Id { get; set; }
        public string FloorName { get; set; } = "";
        public string PlanDescription { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int FloorNumber { get; set; }
    }
}