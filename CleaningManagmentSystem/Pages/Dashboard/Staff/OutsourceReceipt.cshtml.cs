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
    public class OutsourceReceiptModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int WeredaId { get; set; }

        [BindProperty]
        public int CompanyId { get; set; }

        [BindProperty]
        public int VehicleId { get; set; }

        [BindProperty]
        public int DriverId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Time is required")]
        public string TimeString { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Kilogram is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kilogram must be greater than 0")]
        public decimal Kilogram { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Price per kg is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        // Edit properties
        [BindProperty(SupportsGet = true)]
        public int EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EditMode { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public int? FilterWeredaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterCompanyId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterPlate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterDriverId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterEndDate { get; set; }

        public List<Wereda> Weredas { get; set; } = new();
        public List<OutsourceCompany> Companies { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<DriverModel> Drivers { get; set; } = new();
        public List<dynamic> RecentReceipts { get; set; } = new();

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public OutsourceReceiptModel(IConfiguration configuration)
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
                var companyName = GetCompanyName(CompanyId);
                var plateNumber = GetPlateNumber(VehicleId);
                var driverName = GetDriverName(DriverId);

                using var connection = new MySqlConnection(_connectionString);

                connection.Execute(
                    @"INSERT INTO outsource_receipts 
                        (wereda_id, wereda_name, company_id, company_name, 
                         vehicle_id, plate_number, driver_id, driver_name,
                         receipt_time, receipt_date, kilogram, price, 
                         registered_by, status, registered_at)
                      VALUES 
                        (@WeredaId, @WeredaName, @CompanyId, @CompanyName,
                         @VehicleId, @PlateNumber, @DriverId, @DriverName,
                         @ReceiptTime, @Date, @Kilogram, @Price,
                         @RegisteredBy, 'Registered', NOW())",
                    new
                    {
                        WeredaId,
                        WeredaName = weredaName,
                        CompanyId,
                        CompanyName = companyName,
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
                CompanyId = 0;
                VehicleId = 0;
                DriverId = 0;
                TimeString = "";
                Date = default;
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
                return RedirectToPage("/Dashboard/Staff/OutsourceReceipt");

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var receipt = connection.QueryFirstOrDefault(@"
                    SELECT id, wereda_id, company_id, vehicle_id, driver_id,
                           receipt_time, receipt_date, kilogram, price
                    FROM outsource_receipts WHERE id = @Id",
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
                CompanyId = receipt.company_id ?? 0;
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
                var companyName = GetCompanyName(CompanyId);
                var plateNumber = GetPlateNumber(VehicleId);
                var driverName = GetDriverName(DriverId);

                using var connection = new MySqlConnection(_connectionString);

                var rowsAffected = connection.Execute(
                    @"UPDATE outsource_receipts SET 
                          wereda_id = @WeredaId,
                          wereda_name = @WeredaName,
                          company_id = @CompanyId,
                          company_name = @CompanyName,
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
                        CompanyId,
                        CompanyName = companyName,
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
                CompanyId = 0;
                VehicleId = 0;
                DriverId = 0;
                TimeString = "";
                Date = default;
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
                    "DELETE FROM outsource_receipts WHERE id = @Id",
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

        public async Task<IActionResult> OnPostGenerateBillAsync(int[] selectedReceipts)
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (selectedReceipts == null || selectedReceipts.Length == 0)
            {
                Message = "Please select at least one receipt to generate a bill.";
                IsSuccess = false;
                LoadData();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var ids = string.Join(",", selectedReceipts);

                // Redirect to the new Invoice page passing the IDs and Type
                // Database update to 'Billed' will happen on the invoice page upon confirmation
                return RedirectToPage("/Dashboard/Staff/Invoice", new { ids = ids, type = "Outsource" });
            }
            catch (Exception ex)
            {
                Message = $"Error generating bill: {ex.Message}";
                IsSuccess = false;
            }

            LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostPayAsync(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (id <= 0)
            {
                Message = "Invalid receipt ID.";
                IsSuccess = false;
                LoadData();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.ExecuteAsync("UPDATE outsource_receipts SET status = 'Paid' WHERE id = @Id", new { Id = id });
                Message = "Receipt marked as Paid successfully!";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"Error marking as paid: {ex.Message}";
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

        private string GetCompanyName(int companyId)
        {
            if (companyId <= 0) return "";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return connection.QueryFirstOrDefault<string>(
                    "SELECT company_name FROM outsource_companies WHERE id = @Id",
                    new { Id = companyId }) ?? "";
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

                var weredas = connection.Query<Wereda>(@"
                    SELECT id AS Id, name AS Name, description AS Description,
                           subcity AS Subcity, is_active AS IsActive,
                           created_at AS CreatedAt, updated_at AS UpdatedAt
                    FROM weredas WHERE is_active = 1 ORDER BY name ASC").ToList();

                var companies = connection.Query<OutsourceCompany>(@"
                    SELECT id AS Id, company_name AS CompanyName
                    FROM outsource_companies WHERE status = 'Active' ORDER BY company_name ASC").ToList();

                var vehicles = connection.Query<Vehicle>(@"
                    SELECT id AS Id, plate_number AS PlateNumber,
                           vehicle_type AS VehicleType, model AS Model,
                           color AS Color, driver_id AS DriverId,
                           driver_name AS DriverName, status AS Status,
                           created_at AS CreatedAt, updated_at AS UpdatedAt
                    FROM vehicles WHERE status IN ('Available', 'Assigned')
                    ORDER BY plate_number ASC").ToList();

                var drivers = connection.Query<DriverModel>(@"
                    SELECT id AS Id, full_name AS FullName, phone AS Phone,
                           email AS Email, license_number AS LicenseNumber,
                           license_type AS LicenseType, license_expiry AS LicenseExpiry,
                           address AS Address, is_active AS IsActive,
                           created_at AS CreatedAt, updated_at AS UpdatedAt
                    FROM drivers WHERE is_active = 1 ORDER BY full_name ASC").ToList();

                Weredas = weredas;
                Companies = companies;
                Vehicles = vehicles;
                Drivers = drivers;

                ViewData["Weredas"] = weredas;
                ViewData["Companies"] = companies;
                ViewData["Vehicles"] = vehicles;
                ViewData["Drivers"] = drivers;

                // Load recent receipts with optional filters
                var query = @"SELECT id, wereda_name, company_name, plate_number, driver_name,
                                       receipt_date, receipt_time, kilogram, price,
                                       kilogram * price AS total_price, status, registered_at
                                FROM outsource_receipts
                                WHERE status IN ('Registered', 'Approved', 'Billed')";

                var parameters = new DynamicParameters();

                if (FilterWeredaId.HasValue && FilterWeredaId.Value > 0)
                {
                    query += " AND wereda_id = @FilterWeredaId";
                    parameters.Add("FilterWeredaId", FilterWeredaId.Value);
                }

                if (FilterCompanyId.HasValue && FilterCompanyId.Value > 0)
                {
                    query += " AND company_id = @FilterCompanyId";
                    parameters.Add("FilterCompanyId", FilterCompanyId.Value);
                }

                if (!string.IsNullOrEmpty(FilterPlate))
                {
                    query += " AND plate_number LIKE @FilterPlate";
                    parameters.Add("FilterPlate", $"%{FilterPlate}%");
                }

                if (FilterDriverId.HasValue && FilterDriverId.Value > 0)
                {
                    query += " AND driver_id = @FilterDriverId";
                    parameters.Add("FilterDriverId", FilterDriverId.Value);
                }

                if (FilterStartDate.HasValue)
                {
                    query += " AND receipt_date >= @FilterStartDate";
                    parameters.Add("FilterStartDate", FilterStartDate.Value);
                }

                if (FilterEndDate.HasValue)
                {
                    query += " AND receipt_date <= @FilterEndDate";
                    parameters.Add("FilterEndDate", FilterEndDate.Value);
                }

                query += " ORDER BY registered_at DESC LIMIT 10";

                var receipts = connection.Query(query, parameters).ToList();
                RecentReceipts = receipts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Receipt Load Error] {ex.Message}");
            }
        }
    }
}