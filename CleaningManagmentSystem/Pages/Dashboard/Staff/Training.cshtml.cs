using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class TrainingModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string TrainingName { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public DateTime StartDate { get; set; }

        [BindProperty]
        public DateTime EndDate { get; set; }

        [BindProperty]
        public string Trainer { get; set; } = "";

        [BindProperty]
        public string Location { get; set; } = "";

        [BindProperty]
        public string Status { get; set; } = "Scheduled";

        [BindProperty]
        public int? AssignedToUserId { get; set; }

        [BindProperty]
        public string Materials { get; set; } = "";

        public List<Training> Trainings { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public TrainingModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public class UserListItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
        
        public List<UserListItem> StaffList { get; set; } = new();

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadTrainings(userId, userRole);

            if (userRole == "superadmin" || userRole == "manager")
            {
                using var connection = new MySqlConnection(_connectionString);
                StaffList = connection.Query<UserListItem>("SELECT id, name FROM users WHERE role = 'staff'").ToList();
            }

            return Page();
        }

        public IActionResult OnPostAdd()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "INSERT INTO trainings (title, description, start_date, end_date, trainer, location, status, materials, assigned_to_user_id) VALUES (@TrainingName, @Description, @StartDate, @EndDate, @Trainer, @Location, @Status, @Materials, @AssignedToUserId)",
                    new { TrainingName, Description, StartDate, EndDate, Trainer, Location, Status, Materials, AssignedToUserId });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadTrainings(userId, userRole);
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "UPDATE trainings SET title = @TrainingName, description = @Description, start_date = @StartDate, end_date = @EndDate, trainer = @Trainer, location = @Location, status = @Status WHERE id = @Id",
                    new { Id, TrainingName, Description, StartDate, EndDate, Trainer, Location, Status });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadTrainings(userId, userRole);
            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM trainings WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadTrainings(userId, userRole);
            return Page();
        }

        private void LoadTrainings(int? userId, string? userRole)
        {
            using var connection = new MySqlConnection(_connectionString);
            if (userRole == "staff")
            {
                Trainings = connection.Query<Training>(
                    "SELECT id, title as TrainingName, category as Category, description, start_date as StartDate, end_date as EndDate, trainer, location, status, materials as Materials, participants as Participants, assigned_to_user_id as AssignedToUserId FROM trainings WHERE assigned_to_user_id = @UserId OR assigned_to_user_id IS NULL ORDER BY start_date DESC",
                    new { UserId = userId }).ToList();
            }
            else
            {
                Trainings = connection.Query<Training>(
                    "SELECT id, title as TrainingName, category as Category, description, start_date as StartDate, end_date as EndDate, trainer, location, status, materials as Materials, participants as Participants, assigned_to_user_id as AssignedToUserId FROM trainings ORDER BY start_date DESC").ToList();
            }
        }
    }

    public class Training
    {
        public int Id { get; set; }
        public string TrainingName { get; set; } = "";
        public string Category { get; set; } = "General";
        public string Description { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Trainer { get; set; } = "";
        public string Location { get; set; } = "";
        public string Status { get; set; } = "Scheduled";
        public string Materials { get; set; } = "";
        public int Participants { get; set; }
        public int? AssignedToUserId { get; set; }
    }
}