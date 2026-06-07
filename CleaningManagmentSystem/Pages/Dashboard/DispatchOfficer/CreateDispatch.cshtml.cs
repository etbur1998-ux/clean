using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    public class CreateDispatchModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public List<Dispatch> Dispatches { get; set; } = new();

        [BindProperty]
        public Dispatch NewDispatch { get; set; } = new();

        [BindProperty]
        public int EditId { get; set; }

        [BindProperty]
        public Dispatch EditDispatch { get; set; } = new();

        [BindProperty]
        public string FilterStatus { get; set; } = "";

        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public int ActiveDispatches { get; set; }
        public int CompletedToday { get; set; }
        public int PendingDispatches { get; set; }

        public CreateDispatchModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[CreateDispatch] OnGet called");

            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole");

            Console.WriteLine($"[CreateDispatch] Session - UserId: {userId}, UserName: {userName}, Role: {role}");

            if (userId == null || userId == 0)
            {
                Console.WriteLine("[CreateDispatch] User not logged in, redirecting to Login");
                return RedirectToPage("/Login");
            }

            if (role?.ToLower() != "dispatch_officer")
            {
                Console.WriteLine($"[CreateDispatch] User role {role} not authorized for this page");
                return RedirectToPage("/Login");
            }

            try
            {
                LoadDispatches();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateDispatch] Error loading data: {ex.Message}");
                ErrorMessage = "Failed to load dispatch data";
            }

            return Page();
        }

        public IActionResult OnPostCreate()
        {
            Console.WriteLine("[CreateDispatch] OnPostCreate called");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(NewDispatch.Destination) || string.IsNullOrEmpty(NewDispatch.DriverName))
            {
                ErrorMessage = "Destination and Driver Name are required";
                OnGet();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                var dispatchNumber = GenerateDispatchNumber(connection);
                
                var sql = @"INSERT INTO dispatches (dispatch_number, origin, destination, driver_name, vehicle_number, 
                           dispatch_date, expected_arrival, status, contents, priority, created_by, created_at) 
                           VALUES (@DispatchNumber, @Origin, @Destination, @DriverName, @VehicleNumber, 
                           @DispatchDate, @ExpectedArrival, @Status, @Contents, @Priority, @CreatedBy, @CreatedAt)";
                
                NewDispatch.DispatchNumber = dispatchNumber;
                NewDispatch.Status = "Pending";
                NewDispatch.Priority = "Normal";
                NewDispatch.CreatedBy = userId ?? 0;
                NewDispatch.CreatedAt = DateTime.Now;
                NewDispatch.DispatchDate = DateTime.Now;
                NewDispatch.ExpectedArrival = DateTime.Now.AddHours(4);

                connection.Execute(sql, new
                {
                    NewDispatch.DispatchNumber,
                    NewDispatch.Origin,
                    NewDispatch.Destination,
                    NewDispatch.DriverName,
                    NewDispatch.VehicleNumber,
                    NewDispatch.DispatchDate,
                    NewDispatch.ExpectedArrival,
                    NewDispatch.Status,
                    NewDispatch.Contents,
                    NewDispatch.Priority,
                    NewDispatch.CreatedBy,
                    NewDispatch.CreatedAt
                });

                Console.WriteLine($"[CreateDispatch] Created new dispatch: {dispatchNumber}");
                SuccessMessage = "Dispatch created successfully";
                NewDispatch = new Dispatch();
                LoadDispatches();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateDispatch] Error creating dispatch: {ex.Message}");
                ErrorMessage = "Failed to create dispatch";
            }

            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            Console.WriteLine($"[CreateDispatch] OnPostUpdate called for ID: {EditId}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"UPDATE dispatches SET origin = @Origin, destination = @Destination, 
                           driver_name = @DriverName, vehicle_number = @VehicleNumber, 
                           dispatch_date = @DispatchDate, expected_arrival = @ExpectedArrival, 
                           status = @Status, contents = @Contents, priority = @Priority 
                           WHERE id = @Id";
                
                connection.Execute(sql, new
                {
                    Id = EditId,
                    EditDispatch.Origin,
                    EditDispatch.Destination,
                    EditDispatch.DriverName,
                    EditDispatch.VehicleNumber,
                    EditDispatch.DispatchDate,
                    EditDispatch.ExpectedArrival,
                    EditDispatch.Status,
                    EditDispatch.Contents,
                    EditDispatch.Priority
                });

                Console.WriteLine($"[CreateDispatch] Updated dispatch ID: {EditId}");
                SuccessMessage = "Dispatch updated successfully";
                LoadDispatches();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateDispatch] Error updating dispatch: {ex.Message}");
                ErrorMessage = "Failed to update dispatch";
            }

            return Page();
        }

        public IActionResult OnPostUpdateStatus(int id, string status)
        {
            Console.WriteLine($"[CreateDispatch] OnPostUpdateStatus called for ID: {id}, Status: {status}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("UPDATE dispatches SET status = @Status WHERE id = @Id", 
                    new { Id = id, Status = status });

                Console.WriteLine($"[CreateDispatch] Updated dispatch status ID: {id} to {status}");
                SuccessMessage = "Dispatch status updated successfully";
                LoadDispatches();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateDispatch] Error updating status: {ex.Message}");
                ErrorMessage = "Failed to update dispatch status";
            }

            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            Console.WriteLine($"[CreateDispatch] OnPostDelete called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM dispatches WHERE id = @Id", new { Id = id });

                Console.WriteLine($"[CreateDispatch] Deleted dispatch ID: {id}");
                SuccessMessage = "Dispatch deleted successfully";
                LoadDispatches();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateDispatch] Error deleting dispatch: {ex.Message}");
                ErrorMessage = "Failed to delete dispatch";
            }

            return Page();
        }

        private string GenerateDispatchNumber(MySqlConnection connection)
        {
            var count = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM dispatches");
            return $"DSP-{DateTime.Now:yyyyMMdd}-{(count + 1):D4}";
        }

        private void LoadDispatches()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            var sql = "SELECT * FROM dispatches WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                sql += " AND status = @Status";
                parameters.Add("Status", FilterStatus);
            }

            sql += " ORDER BY created_at DESC LIMIT 100";

            Dispatches = connection.Query<Dispatch>(sql, parameters).ToList();
            Console.WriteLine($"[CreateDispatch] Loaded {Dispatches.Count} dispatches");
        }

        private void LoadStatistics()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            ActiveDispatches = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM dispatches WHERE status IN ('Pending', 'In Progress', 'Assigned')") ?? 0;
            CompletedToday = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM dispatches WHERE status = 'Completed' AND DATE(created_at) = CURDATE()") ?? 0;
            PendingDispatches = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM dispatches WHERE status = 'Pending'") ?? 0;
            
            Console.WriteLine($"[CreateDispatch] Statistics - Active: {ActiveDispatches}, Completed Today: {CompletedToday}, Pending: {PendingDispatches}");
        }
    }
}
