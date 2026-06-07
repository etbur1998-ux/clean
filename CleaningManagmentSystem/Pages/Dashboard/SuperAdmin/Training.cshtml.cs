using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class TrainingModel : PageModel
    {
        private readonly string _connectionString;

        public List<TrainingItem> Trainings { get; set; } = new();
        public List<UserItem> UsersList { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public TrainingModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public class TrainingItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Trainer { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Location { get; set; } = "";
            public int Participants { get; set; }
            public string Status { get; set; } = "Scheduled";
            public string Materials { get; set; } = "";
            public int? AssignedToUserId { get; set; }
            public string AssignedToUserName { get; set; } = "General (All)";
            public DateTime CreatedAt { get; set; }
        }

        public class UserItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Role { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userName == null || userRole != "superadmin")
            {
                return RedirectToPage("/Login");
            }

            LoadData();
            return Page();
        }

        public IActionResult OnPostCreate(
            string title, string category, string trainer, string description, 
            DateTime startDate, DateTime endDate, string location, int participants, 
            string status, string materials, int? assignedToUserId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                string query = @"
                    INSERT INTO trainings 
                    (title, category, trainer, description, start_date, end_date, location, participants, status, materials, assigned_to_user_id, created_at)
                    VALUES 
                    (@Title, @Category, @Trainer, @Description, @StartDate, @EndDate, @Location, @Participants, @Status, @Materials, @AssignedToUserId, NOW())";
                
                connection.Execute(query, new {
                    Title = title,
                    Category = string.IsNullOrEmpty(category) ? "General" : category,
                    Trainer = trainer,
                    Description = description,
                    StartDate = startDate,
                    EndDate = endDate,
                    Location = location,
                    Participants = participants,
                    Status = string.IsNullOrEmpty(status) ? "Scheduled" : status,
                    Materials = materials,
                    AssignedToUserId = assignedToUserId
                });

                SuccessMessage = "Training program created successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error creating training: " + ex.Message;
            }

            LoadData();
            return Page();
        }

        public IActionResult OnPostUpdate(
            int id, string title, string category, string trainer, string description, 
            DateTime startDate, DateTime endDate, string location, int participants, 
            string status, string materials, int? assignedToUserId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                string query = @"
                    UPDATE trainings 
                    SET title = @Title, category = @Category, trainer = @Trainer, description = @Description, 
                        start_date = @StartDate, end_date = @EndDate, location = @Location, participants = @Participants, 
                        status = @Status, materials = @Materials, assigned_to_user_id = @AssignedToUserId 
                    WHERE id = @Id";

                connection.Execute(query, new {
                    Id = id,
                    Title = title,
                    Category = string.IsNullOrEmpty(category) ? "General" : category,
                    Trainer = trainer,
                    Description = description,
                    StartDate = startDate,
                    EndDate = endDate,
                    Location = location,
                    Participants = participants,
                    Status = string.IsNullOrEmpty(status) ? "Scheduled" : status,
                    Materials = materials,
                    AssignedToUserId = assignedToUserId
                });

                SuccessMessage = "Training program updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating training: " + ex.Message;
            }

            LoadData();
            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM trainings WHERE id = @Id", new { Id = id });
                SuccessMessage = "Training program deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting training: " + ex.Message;
            }

            LoadData();
            return Page();
        }

        private void LoadData()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Fetch all trainings with assigned username if applicable
                Trainings = connection.Query<TrainingItem>(@"
                    SELECT t.id as Id, t.title as Title, t.category as Category, t.trainer as Trainer, 
                           t.description as Description, t.start_date as StartDate, t.end_date as EndDate, 
                           t.location as Location, t.participants as Participants, t.status as Status, 
                           t.materials as Materials, t.assigned_to_user_id as AssignedToUserId, 
                           u.name as AssignedToUserName 
                    FROM trainings t
                    LEFT JOIN users u ON t.assigned_to_user_id = u.id
                    ORDER BY t.start_date DESC").ToList();

                // Load all eligible users (staff, cleaner, drivers, etc.) to allow assignments
                UsersList = connection.Query<UserItem>(@"
                    SELECT id as Id, name as Name, role as Role 
                    FROM users 
                    WHERE role IN ('staff', 'cleaner', 'driver', 'manager', 'wereda_mahberat') 
                    ORDER BY name ASC").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading database data: " + ex.Message;
            }
        }
    }
}
