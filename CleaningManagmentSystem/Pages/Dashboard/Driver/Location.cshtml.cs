using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.Driver
{
    public class LocationModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public DriverLocation? Location { get; set; }

        [BindProperty]
        public string? Latitude { get; set; }

        [BindProperty]
        public string? Longitude { get; set; }

        [BindProperty]
        public string? Address { get; set; }

        [BindProperty]
        public string? Notes { get; set; }

        public IEnumerable<DriverLocation>? Locations { get; set; }

        public LocationModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            LoadDriverLocations(userId.Value);
            return Page();
        }

        public IActionResult OnPost()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(Latitude) || string.IsNullOrEmpty(Longitude))
            {
                ModelState.AddModelError(string.Empty, "Latitude and Longitude are required");
                LoadDriverLocations(userId.Value);
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Check if location record exists for this driver
                var existingLocation = connection.QueryFirstOrDefault<DriverLocation>(
                    "SELECT * FROM driver_locations WHERE driver_id = @DriverId",
                    new { DriverId = userId });

                if (existingLocation != null)
                {
                    // Update existing location
                    connection.Execute(
                        @"UPDATE driver_locations 
                        SET latitude = @Latitude, longitude = @Longitude, address = @Address, 
                            notes = @Notes, updated_at = NOW() 
                        WHERE driver_id = @DriverId",
                        new
                        {
                            Latitude,
                            Longitude,
                            Address,
                            Notes,
                            DriverId = userId
                        });
                }
                else
                {
                    // Insert new location
                    connection.Execute(
                        @"INSERT INTO driver_locations (driver_id, latitude, longitude, address, notes, created_at, updated_at)
                        VALUES (@DriverId, @Latitude, @Longitude, @Address, @Notes, NOW(), NOW())",
                        new
                        {
                            DriverId = userId,
                            Latitude,
                            Longitude,
                            Address,
                            Notes
                        });
                }

                TempData["SuccessMessage"] = "Location updated successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Location] Database error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error saving location. Please try again.");
                LoadDriverLocations(userId.Value);
                return Page();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "DELETE FROM driver_locations WHERE id = @Id AND driver_id = @DriverId",
                    new { Id = id, DriverId = userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Location] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting location";
            }

            return RedirectToPage();
        }

        private void LoadDriverLocations(int driverId)
        {
            using var connection = new MySqlConnection(_connectionString);
            Locations = connection.Query<DriverLocation>(
                "SELECT * FROM driver_locations WHERE driver_id = @DriverId ORDER BY updated_at DESC",
                new { DriverId = driverId });
        }
    }
}
