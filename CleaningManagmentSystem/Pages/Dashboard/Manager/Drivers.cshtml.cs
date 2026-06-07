using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;
using DriverModel = CleaningManagmentSystem.Models.Driver;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    [IgnoreAntiforgeryToken]
    public class DriversModel : PageModel
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
        public string LicenseNumber { get; set; } = "";

        [BindProperty]
        public string LicenseType { get; set; } = "";

        [BindProperty]
        public DateTime LicenseExpiry { get; set; }

        [BindProperty]
        public string Address { get; set; } = "";

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<DriverModel> Drivers { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public DriversModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "manager")
            {
                return RedirectToPage("/Login");
            }

            LoadDrivers();
            return Page();
        }

        public IActionResult OnPostAdd()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "INSERT INTO drivers (full_name, phone, email, license_number, license_type, license_expiry, address, is_active, created_at) VALUES (@FullName, @Phone, @Email, @LicenseNumber, @LicenseType, @LicenseExpiry, @Address, @IsActive, NOW())",
                    new { FullName, Phone, Email, LicenseNumber, LicenseType, LicenseExpiry, Address, IsActive });
                SuccessMessage = "Driver added successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding driver: " + ex.Message;
            }

            LoadDrivers();
            return RedirectToPage();
        }

        public IActionResult OnPostUpdate()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "UPDATE drivers SET full_name = @FullName, phone = @Phone, email = @Email, license_number = @LicenseNumber, license_type = @LicenseType, license_expiry = @LicenseExpiry, address = @Address, is_active = @IsActive, updated_at = NOW() WHERE id = @Id",
                    new { Id, FullName, Phone, Email, LicenseNumber, LicenseType, LicenseExpiry, Address, IsActive });
                SuccessMessage = "Driver updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating driver: " + ex.Message;
            }

            LoadDrivers();
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM drivers WHERE id = @Id", new { Id = id });
                SuccessMessage = "Driver deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting driver: " + ex.Message;
            }

            LoadDrivers();
            return RedirectToPage();
        }

        private void LoadDrivers()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                Drivers = connection.Query<DriverModel>(@"
                    SELECT 
                        id AS Id,
                        full_name AS FullName,
                        phone AS Phone,
                        email AS Email,
                        license_number AS LicenseNumber,
                        license_type AS LicenseType,
                        license_expiry AS LicenseExpiry,
                        address AS Address,
                        is_active AS IsActive,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM drivers 
                    ORDER BY full_name ASC").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading drivers: " + ex.Message;
            }
        }
    }
}