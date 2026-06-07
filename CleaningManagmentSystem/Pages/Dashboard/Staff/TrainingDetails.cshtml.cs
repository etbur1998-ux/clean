using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class TrainingDetailsModel : PageModel
    {
        private readonly string _connectionString;

        public TrainingItem Training { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public TrainingDetailsModel(IConfiguration configuration)
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
            public string AssignedToUserName { get; set; } = "";
        }

        public IActionResult OnGet(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            LoadTraining(id);
            return Page();
        }

        public IActionResult OnPostComplete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string updateQuery = "UPDATE trainings SET status = 'Completed' WHERE id = @Id";
                connection.Execute(updateQuery, new { Id = id });
                SuccessMessage = "Congratulations! You have successfully marked this training program as Completed!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error completing training: " + ex.Message;
            }

            LoadTraining(id);
            return Page();
        }

        private void LoadTraining(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                string query = @"
                    SELECT t.id as Id, t.title as Title, t.category as Category, t.trainer as Trainer, 
                           t.description as Description, t.start_date as StartDate, t.end_date as EndDate, 
                           t.location as Location, t.participants as Participants, t.status as Status, 
                           t.materials as Materials, t.assigned_to_user_id as AssignedToUserId, 
                           u.name as AssignedToUserName 
                    FROM trainings t
                    LEFT JOIN users u ON t.assigned_to_user_id = u.id
                    WHERE t.id = @Id";

                var item = connection.QueryFirstOrDefault<TrainingItem>(query, new { Id = id });
                if (item != null)
                {
                    Training = item;
                }
                else
                {
                    ErrorMessage = "The requested training record could not be found.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database fetch error: " + ex.Message;
            }
        }
    }
}
