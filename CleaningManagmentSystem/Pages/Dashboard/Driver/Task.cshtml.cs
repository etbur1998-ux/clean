using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.Driver
{
    public class TaskModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public DriverTask? Task { get; set; }

        [BindProperty]
        public int? TaskId { get; set; }

        [BindProperty]
        public string? Status { get; set; }

        [BindProperty]
        public string? Notes { get; set; }

        [BindProperty]
        public string? FilterStatus { get; set; }

        public IEnumerable<DriverTask>? Tasks { get; set; }

        public TaskModel(IConfiguration configuration)
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

            LoadDriverTasks(userId.Value);
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

            if (!TaskId.HasValue || string.IsNullOrEmpty(Status))
            {
                ModelState.AddModelError(string.Empty, "Task ID and Status are required");
                LoadDriverTasks(userId.Value);
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Verify task belongs to this driver
                var task = connection.QueryFirstOrDefault<DriverTask>(
                    "SELECT * FROM delivery_tasks WHERE id = @TaskId AND driver_id = @DriverId",
                    new { TaskId = TaskId, DriverId = userId });

                if (task == null)
                {
                    ModelState.AddModelError(string.Empty, "Task not found or unauthorized");
                    LoadDriverTasks(userId.Value);
                    return Page();
                }

                // Update task status
                var affectedRows = connection.Execute(
                    @"UPDATE delivery_tasks 
                    SET status = @Status, notes = @Notes, updated_at = NOW() 
                    WHERE id = @TaskId AND driver_id = @DriverId",
                    new
                    {
                        Status,
                        Notes,
                        TaskId = TaskId,
                        DriverId = userId
                    });

                if (affectedRows > 0)
                {
                    // Also update task history
                    connection.Execute(
                        @"INSERT INTO task_history (task_id, status, notes, created_at)
                        VALUES (@TaskId, @Status, @Notes, NOW())",
                        new
                        {
                            TaskId = TaskId,
                            Status,
                            Notes
                        });

                    TempData["SuccessMessage"] = "Task status updated successfully";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Task] Update error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error updating task. Please try again.");
            }

            LoadDriverTasks(userId.Value);
            return Page();
        }

        public IActionResult OnPostComplete(int taskId)
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
                    @"UPDATE delivery_tasks 
                    SET status = 'completed', updated_at = NOW() 
                    WHERE id = @TaskId AND driver_id = @DriverId",
                    new { TaskId = taskId, DriverId = userId });

                TempData["SuccessMessage"] = "Task marked as completed";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Task] Complete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error completing task";
            }

            return RedirectToPage();
        }

        private void LoadDriverTasks(int driverId)
        {
            using var connection = new MySqlConnection(_connectionString);

            var query = @"SELECT * FROM delivery_tasks 
                         WHERE driver_id = @DriverId";

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                query += " AND status = @FilterStatus";
            }

            query += " ORDER BY created_at DESC";

            Tasks = connection.Query<DriverTask>(query, new { DriverId = driverId, FilterStatus });
        }
    }
}
