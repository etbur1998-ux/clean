using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;
using DriverModel = CleaningManagmentSystem.Models.Driver;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    [IgnoreAntiforgeryToken]
    public class VehiclesModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string PlateNumber { get; set; } = "";

        [BindProperty]
        public string VehicleType { get; set; } = "";

        [BindProperty]
        public string Model { get; set; } = "";

        [BindProperty]
        public string Color { get; set; } = "";

        [BindProperty]
        public int? DriverId { get; set; }

        [BindProperty]
        public string Status { get; set; } = "Available";

        public List<Vehicle> Vehicles { get; set; } = new();
        public List<DriverModel> Drivers { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public VehiclesModel(IConfiguration configuration)
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
            LoadVehicles();
            return Page();
        }

        public IActionResult OnPostAdd()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                string driverName = "";
                if (DriverId.HasValue && DriverId > 0)
                {
                    using var connection = new MySqlConnection(_connectionString);
                    var driver = connection.QueryFirstOrDefault<DriverModel>("SELECT full_name FROM drivers WHERE id = @DriverId", new { DriverId });
                    if (driver != null)
                    {
                        driverName = driver.FullName;
                    }
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Execute(
                    "INSERT INTO vehicles (plate_number, vehicle_type, model, color, driver_id, driver_name, status, created_at) VALUES (@PlateNumber, @VehicleType, @Model, @Color, @DriverId, @DriverName, @Status, NOW())",
                    new { PlateNumber, VehicleType, Model, Color, DriverId, DriverName = driverName, Status });
                SuccessMessage = "Vehicle added successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding vehicle: " + ex.Message;
            }

            LoadVehicles();
            return RedirectToPage();
        }

        public IActionResult OnPostUpdate()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                string driverName = "";
                if (DriverId.HasValue && DriverId > 0)
                {
                    using var connection = new MySqlConnection(_connectionString);
                    var driver = connection.QueryFirstOrDefault<DriverModel>("SELECT full_name FROM drivers WHERE id = @DriverId", new { DriverId });
                    if (driver != null)
                    {
                        driverName = driver.FullName;
                    }
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Execute(
                    "UPDATE vehicles SET plate_number = @PlateNumber, vehicle_type = @VehicleType, model = @Model, color = @Color, driver_id = @DriverId, driver_name = @DriverName, status = @Status, updated_at = NOW() WHERE id = @Id",
                    new { Id, PlateNumber, VehicleType, Model, Color, DriverId, DriverName = driverName, Status });
                SuccessMessage = "Vehicle updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating vehicle: " + ex.Message;
            }

            LoadVehicles();
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
                connection.Execute("DELETE FROM vehicles WHERE id = @Id", new { Id = id });
                SuccessMessage = "Vehicle deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting vehicle: " + ex.Message;
            }

            LoadVehicles();
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
                    WHERE is_active = 1 
                    ORDER BY full_name ASC").ToList();
            }
            catch { }
        }

        private void LoadVehicles()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                Vehicles = connection.Query<Vehicle>(@"
                    SELECT 
                        id AS Id,
                        plate_number AS PlateNumber,
                        vehicle_type AS VehicleType,
                        model AS Model,
                        color AS Color,
                        driver_id AS DriverId,
                        driver_name AS DriverName,
                        status AS Status,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM vehicles 
                    ORDER BY plate_number ASC").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading vehicles: " + ex.Message;
            }
        }
    }
}
