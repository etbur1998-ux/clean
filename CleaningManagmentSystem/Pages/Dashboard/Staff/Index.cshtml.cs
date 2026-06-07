using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;
using System.ComponentModel.DataAnnotations;
using DriverModel = CleaningManagmentSystem.Models.Driver;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int WeredaId { get; set; }

        [BindProperty]
        public int MahberatId { get; set; }

        [BindProperty]
        public int VehicleId { get; set; }

        [BindProperty]
        public int DriverId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Time is required")]
        public string TimeString { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; } = DateTime.Today;

        [BindProperty]
        [Required(ErrorMessage = "Kilogram is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kilogram must be greater than 0")]
        public decimal Kilogram { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Price per kg is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public int? FilterWeredaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterMahberatId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterPlate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterDriverId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterEndDate { get; set; }

        // Edit properties
        [BindProperty(SupportsGet = true)]
        public int EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EditMode { get; set; }

        public List<Wereda> Weredas { get; set; } = new();
        public List<Mahberat> Mahberats { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<DriverModel> Drivers { get; set; } = new();
        public List<dynamic> RecentReceipts { get; set; } = new();

        // Chart data
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartValues { get; set; } = new();
        public List<string> VehicleLabels { get; set; } = new();
        public List<int> VehicleValues { get; set; } = new();
        public List<string> TrendLabels { get; set; } = new();
        public List<decimal> TrendValues { get; set; } = new();
        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusValues { get; set; } = new();

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public IndexModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            LoadData();
            return Page();
        }

        public IActionResult OnPost()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                LoadData();
                return Page();
            }

            try
            {
                // Parse time string to TimeSpan
                TimeSpan receiptTime = TimeSpan.Zero;
                if (!string.IsNullOrEmpty(TimeString))
                {
                    if (TimeSpan.TryParse(TimeString, out var parsedTime))
                    {
                        receiptTime = parsedTime;
                    }
                }

                // Look up names from IDs
                var weredaName = GetWeredaName(WeredaId);
                var mahberatName = GetMahberatName(MahberatId);
                var plateNumber = GetPlateNumber(VehicleId);
                var driverName = GetDriverName(DriverId);

                using var connection = new MySqlConnection(_connectionString);

                connection.Execute(
                    @"INSERT INTO staff_receipts 
                        (wereda_id, wereda_name, mahberat_id, mahberat_name, 
                         vehicle_id, plate_number, driver_id, driver_name,
                         receipt_time, receipt_date, kilogram, price, 
                         registered_by, status, registered_at)
                      VALUES 
                        (@WeredaId, @WeredaName, @MahberatId, @MahberatName,
                         @VehicleId, @PlateNumber, @DriverId, @DriverName,
                         @ReceiptTime, @Date, @Kilogram, @Price,
                         @RegisteredBy, 'Registered', NOW())",
                    new
                    {
                        WeredaId,
                        WeredaName = weredaName,
                        MahberatId,
                        MahberatName = mahberatName,
                        VehicleId,
                        PlateNumber = plateNumber,
                        DriverId,
                        DriverName = driverName,
                        ReceiptTime = receiptTime,
                        Date,
                        Kilogram,
                        Price,
                        RegisteredBy = userName
                    });

                Message = "Receipt registered successfully!";
                IsSuccess = true;

                // Reset form fields
                WeredaId = 0;
                MahberatId = 0;
                VehicleId = 0;
                DriverId = 0;
                TimeString = "";
                Date = DateTime.Today;
                Kilogram = 0;
                Price = 0;
                EditId = 0;
                EditMode = null;
            }
            catch (Exception ex)
            {
                Message = $"Error registering receipt: {ex.Message}";
                IsSuccess = false;
            }

            LoadData();
            return Page();
        }

        public IActionResult OnGetEdit()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (EditId <= 0)
                return RedirectToPage("/Dashboard/Staff/Index");

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var receipt = connection.QueryFirstOrDefault(@"
                    SELECT id, wereda_id, mahberat_id, vehicle_id, driver_id,
                           receipt_time, receipt_date, kilogram, price
                    FROM staff_receipts WHERE id = @Id",
                    new { Id = EditId });

                if (receipt == null)
                {
                    Message = "Receipt not found!";
                    IsSuccess = false;
                    LoadData();
                    return Page();
                }

                // Pre-populate form fields
                EditId = receipt.id;
                WeredaId = receipt.wereda_id ?? 0;
                MahberatId = receipt.mahberat_id ?? 0;
                VehicleId = receipt.vehicle_id ?? 0;
                DriverId = receipt.driver_id ?? 0;
                TimeString = receipt.receipt_time.ToString(@"hh\:mm");
                Date = receipt.receipt_date;
                Kilogram = receipt.kilogram;
                Price = receipt.price;
                EditMode = "true";
            }
            catch (Exception ex)
            {
                Message = $"Error loading receipt for edit: {ex.Message}";
                IsSuccess = false;
            }

            LoadData();
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                LoadData();
                EditMode = "true";
                return Page();
            }

            if (EditId <= 0)
            {
                Message = "Invalid receipt ID for update!";
                IsSuccess = false;
                LoadData();
                return Page();
            }

            try
            {
                // Parse time string to TimeSpan
                TimeSpan receiptTime = TimeSpan.Zero;
                if (!string.IsNullOrEmpty(TimeString))
                {
                    if (TimeSpan.TryParse(TimeString, out var parsedTime))
                    {
                        receiptTime = parsedTime;
                    }
                }

                // Look up names from IDs
                var weredaName = GetWeredaName(WeredaId);
                var mahberatName = GetMahberatName(MahberatId);
                var plateNumber = GetPlateNumber(VehicleId);
                var driverName = GetDriverName(DriverId);

                using var connection = new MySqlConnection(_connectionString);

                var rowsAffected = connection.Execute(
                    @"UPDATE staff_receipts SET 
                          wereda_id = @WeredaId,
                          wereda_name = @WeredaName,
                          mahberat_id = @MahberatId,
                          mahberat_name = @MahberatName,
                          vehicle_id = @VehicleId,
                          plate_number = @PlateNumber,
                          driver_id = @DriverId,
                          driver_name = @DriverName,
                          receipt_time = @ReceiptTime,
                          receipt_date = @Date,
                          kilogram = @Kilogram,
                          price = @Price,
                          updated_at = NOW()
                      WHERE id = @EditId",
                    new
                    {
                        EditId,
                        WeredaId,
                        WeredaName = weredaName,
                        MahberatId,
                        MahberatName = mahberatName,
                        VehicleId,
                        PlateNumber = plateNumber,
                        DriverId,
                        DriverName = driverName,
                        ReceiptTime = receiptTime,
                        Date,
                        Kilogram,
                        Price
                    });

                if (rowsAffected > 0)
                {
                    Message = "Receipt updated successfully!";
                    IsSuccess = true;
                }
                else
                {
                    Message = "Receipt not found or no changes made.";
                    IsSuccess = false;
                }

                // Reset form fields
                WeredaId = 0;
                MahberatId = 0;
                VehicleId = 0;
                DriverId = 0;
                TimeString = "";
                Date = DateTime.Today;
                Kilogram = 0;
                Price = 0;
                EditId = 0;
                EditMode = null;
            }
            catch (Exception ex)
            {
                Message = $"Error updating receipt: {ex.Message}";
                IsSuccess = false;
            }

            LoadData();
            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (id <= 0)
            {
                Message = "Invalid receipt ID!";
                IsSuccess = false;
                LoadData();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var rowsAffected = connection.Execute(
                    "DELETE FROM staff_receipts WHERE id = @Id",
                    new { Id = id });

                if (rowsAffected > 0)
                {
                    Message = "Receipt deleted successfully!";
                    IsSuccess = true;
                }
                else
                {
                    Message = "Receipt not found.";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting receipt: {ex.Message}";
                IsSuccess = false;
            }

            LoadData();
            return Page();
        }

        private string GetWeredaName(int weredaId)
        {
            if (weredaId <= 0) return "";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return connection.QueryFirstOrDefault<string>(
                    "SELECT name FROM weredas WHERE id = @Id",
                    new { Id = weredaId }) ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetMahberatName(int mahberatId)
        {
            if (mahberatId <= 0) return "";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return connection.QueryFirstOrDefault<string>(
                    "SELECT name FROM mahberats WHERE id = @Id",
                    new { Id = mahberatId }) ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetPlateNumber(int vehicleId)
        {
            if (vehicleId <= 0) return "";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return connection.QueryFirstOrDefault<string>(
                    "SELECT plate_number FROM vehicles WHERE id = @Id",
                    new { Id = vehicleId }) ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetDriverName(int driverId)
        {
            if (driverId <= 0) return "";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return connection.QueryFirstOrDefault<string>(
                    "SELECT full_name FROM drivers WHERE id = @Id",
                    new { Id = driverId }) ?? "";
            }
            catch
            {
                return "";
            }
        }

        private void LoadData()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Load dropdown data with column aliases to match PascalCase model properties
                var weredas = connection.Query<Wereda>(@"
                    SELECT 
                        id AS Id,
                        name AS Name,
                        description AS Description,
                        subcity AS Subcity,
                        is_active AS IsActive,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM weredas 
                    WHERE is_active = 1 
                    ORDER BY name ASC").ToList();

                var mahberats = connection.Query<Mahberat>(@"
                    SELECT 
                        id AS Id,
                        name AS Name,
                        wereda_id AS WeredaId,
                        wereda_name AS WeredaName,
                        contact_person AS ContactPerson,
                        phone AS Phone,
                        email AS Email,
                        address AS Address,
                        is_active AS IsActive,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM mahberats 
                    WHERE is_active = 1 
                    ORDER BY name ASC").ToList();

                var vehicles = connection.Query<Vehicle>(@"
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
                    WHERE status IN ('Available', 'Assigned') 
                    ORDER BY plate_number ASC").ToList();

                var drivers = connection.Query<DriverModel>(@"
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

                Weredas = weredas;
                Mahberats = mahberats;
                Vehicles = vehicles;
                Drivers = drivers;

                // Set ViewBag for sidebar quick register form
                ViewData["Weredas"] = weredas;
                ViewData["Mahberats"] = mahberats;
                ViewData["Vehicles"] = vehicles;
                ViewData["Drivers"] = drivers;

                // Load recent receipts (last 10) with optional filters
                var baseStaffQuery = @"SELECT id, wereda_name, mahberat_name, plate_number, driver_name, 
                                      receipt_date, receipt_time, kilogram, price, 
                                      kilogram * price AS total_price, status, registered_at, 'Mahberat' as receipt_type
                               FROM staff_receipts 
                               WHERE status IN ('Registered', 'Approved')";

                var baseOutsourceQuery = @"SELECT id, wereda_name, company_name as mahberat_name, plate_number, driver_name, 
                                      receipt_date, receipt_time, kilogram, price, 
                                      kilogram * price AS total_price, status, registered_at, 'Outsource' as receipt_type
                               FROM outsource_receipts 
                               WHERE status IN ('Registered', 'Approved')";

                var combinedQuery = $"SELECT * FROM ({baseStaffQuery} UNION ALL {baseOutsourceQuery}) as combined WHERE 1=1";

                if (FilterWeredaId.HasValue && FilterWeredaId.Value > 0)
                    combinedQuery += " AND wereda_id = @FilterWeredaId";
                if (!string.IsNullOrWhiteSpace(FilterPlate))
                    combinedQuery += " AND plate_number LIKE CONCAT('%', @FilterPlate, '%')";
                if (FilterDriverId.HasValue && FilterDriverId.Value > 0)
                    combinedQuery += " AND driver_id = @FilterDriverId";
                if (FilterStartDate.HasValue)
                    combinedQuery += " AND receipt_date >= @FilterStartDate";
                if (FilterEndDate.HasValue)
                    combinedQuery += " AND receipt_date <= @FilterEndDate";

                combinedQuery += " ORDER BY registered_at DESC LIMIT 10";

                var receipts = connection.Query(combinedQuery, new {
                    FilterWeredaId,
                    FilterMahberatId,
                    FilterPlate,
                    FilterDriverId,
                    FilterStartDate,
                    FilterEndDate
                }).ToList();

                RecentReceipts = receipts;

                // Chart Data: Receipts by Wereda
                var weredaStats = connection.Query(@"
                    SELECT w.name AS Label, COUNT(combined.id) AS Value
                    FROM (
                        SELECT id, wereda_id FROM staff_receipts
                        UNION ALL
                        SELECT id, wereda_id FROM outsource_receipts
                    ) as combined
                    LEFT JOIN weredas w ON combined.wereda_id = w.id
                    GROUP BY w.name
                    ORDER BY Value DESC
                    LIMIT 10").ToList();

                ChartLabels = weredaStats.Select(w => (string?)(w.Label) ?? "Unknown").ToList();
                ChartValues = weredaStats.Select(w => (int)w.Value).ToList();

                // Chart Data: Receipts by Vehicle
                var vehicleStats = connection.Query(@"
                    SELECT plate_number AS Label, COUNT(id) AS Value
                    FROM (
                        SELECT id, plate_number FROM staff_receipts
                        UNION ALL
                        SELECT id, plate_number FROM outsource_receipts
                    ) as combined 
                    GROUP BY plate_number
                    ORDER BY Value DESC
                    LIMIT 10").ToList();

                VehicleLabels = vehicleStats.Select(v => (string?)(v.Label) ?? "Unknown").ToList();
                VehicleValues = vehicleStats.Select(v => (int)v.Value).ToList();

                // Chart Data: Kilogram Trend (last 10 receipts)
                var trendData = connection.Query(@"
                    SELECT receipt_date AS Label, kilogram AS Value
                    FROM (
                        SELECT receipt_date, kilogram, registered_at FROM staff_receipts
                        UNION ALL
                        SELECT receipt_date, kilogram, registered_at FROM outsource_receipts
                    ) as combined 
                    ORDER BY registered_at DESC 
                    LIMIT 10").ToList();

                TrendLabels = trendData.Select(t => ((DateTime)t.Label).ToString("MM-dd")).ToList();
                TrendValues = trendData.Select(t => (decimal)t.Value).ToList();

                // Chart Data: Status Distribution
                var statusStats = connection.Query(@"
                    SELECT status AS Label, COUNT(id) AS Value
                    FROM (
                        SELECT id, status FROM staff_receipts
                        UNION ALL
                        SELECT id, status FROM outsource_receipts
                    ) as combined 
                    GROUP BY status").ToList();

                StatusLabels = statusStats.Select(s => (string?)(s.Label) ?? "Unknown").ToList();
                StatusValues = statusStats.Select(s => (int)s.Value).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Load Data Error] {ex.Message}");
                Message = $"Error loading data: {ex.Message}";
                IsSuccess = false;
            }
        }
    }
}