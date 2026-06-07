using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class SubcityDriverModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string FullName { get; set; } = "";

        [BindProperty]
        public string Phone { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Subcity { get; set; } = "";

        [BindProperty]
        public string LicenseNumber { get; set; } = "";

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<SubcityDriver> Drivers { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public SubcityDriverModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadDrivers();
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
                    "INSERT INTO subcity_drivers (full_name, phone, email, subcity, license_number, is_active) VALUES (@FullName, @Phone, @Email, @Subcity, @LicenseNumber, @IsActive)",
                    new { FullName, Phone, Email, Subcity, LicenseNumber, IsActive });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadDrivers();
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
                    "UPDATE subcity_drivers SET full_name = @FullName, phone = @Phone, email = @Email, subcity = @Subcity, license_number = @LicenseNumber, is_active = @IsActive WHERE id = @Id",
                    new { Id, FullName, Phone, Email, Subcity, LicenseNumber, IsActive });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadDrivers();
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
                connection.Execute("DELETE FROM subcity_drivers WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadDrivers();
            return Page();
        }

        private void LoadDrivers()
        {
            using var connection = new MySqlConnection(_connectionString);
            Drivers = connection.Query<SubcityDriver>("SELECT * FROM subcity_drivers ORDER BY full_name ASC").ToList();
        }
    }

    public class SubcityDriver
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Subcity { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public bool IsActive { get; set; }
    }
}