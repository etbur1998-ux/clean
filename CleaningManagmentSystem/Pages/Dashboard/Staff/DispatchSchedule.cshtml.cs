using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class DispatchScheduleModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string ScheduleName { get; set; } = "";

        [BindProperty]
        public DateTime DispatchDate { get; set; }

        [BindProperty]
        public string TimeSlot { get; set; } = "";

        [BindProperty]
        public string AssignedDriver { get; set; } = "";

        [BindProperty]
        public string AssignedVehicle { get; set; } = "";

        [BindProperty]
        public string Status { get; set; } = "Scheduled";

        [BindProperty]
        public string Route { get; set; } = "";

        public List<DispatchSchedule> Schedules { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public DispatchScheduleModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadSchedules();
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
                    "INSERT INTO dispatch_schedules (schedule_name, dispatch_date, time_slot, assigned_driver, assigned_vehicle, status, route) VALUES (@ScheduleName, @DispatchDate, @TimeSlot, @AssignedDriver, @AssignedVehicle, @Status, @Route)",
                    new { ScheduleName, DispatchDate, TimeSlot, AssignedDriver, AssignedVehicle, Status, Route });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadSchedules();
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
                    "UPDATE dispatch_schedules SET schedule_name = @ScheduleName, dispatch_date = @DispatchDate, time_slot = @TimeSlot, assigned_driver = @AssignedDriver, assigned_vehicle = @AssignedVehicle, status = @Status, route = @Route WHERE id = @Id",
                    new { Id, ScheduleName, DispatchDate, TimeSlot, AssignedDriver, AssignedVehicle, Status, Route });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadSchedules();
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
                connection.Execute("DELETE FROM dispatch_schedules WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadSchedules();
            return Page();
        }

        private void LoadSchedules()
        {
            using var connection = new MySqlConnection(_connectionString);
            Schedules = connection.Query<DispatchSchedule>("SELECT * FROM dispatch_schedules ORDER BY dispatch_date ASC").ToList();
        }
    }

    public class DispatchSchedule
    {
        public int Id { get; set; }
        public string ScheduleName { get; set; } = "";
        public DateTime DispatchDate { get; set; }
        public string TimeSlot { get; set; } = "";
        public string AssignedDriver { get; set; } = "";
        public string AssignedVehicle { get; set; } = "";
        public string Status { get; set; } = "Scheduled";
        public string Route { get; set; } = "";
    }
}