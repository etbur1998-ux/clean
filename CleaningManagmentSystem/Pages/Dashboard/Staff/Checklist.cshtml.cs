using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class ChecklistModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string ChecklistName { get; set; } = "";

        [BindProperty]
        public string TaskDescription { get; set; } = "";

        [BindProperty]
        public bool IsCompleted { get; set; }

        [BindProperty]
        public DateTime DueDate { get; set; }

        [BindProperty]
        public string AssignedTo { get; set; } = "";

        public List<Checklist> Checklists { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public ChecklistModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadChecklists();
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
                    "INSERT INTO checklists (checklist_name, task_description, is_completed, due_date, assigned_to) VALUES (@ChecklistName, @TaskDescription, @IsCompleted, @DueDate, @AssignedTo)",
                    new { ChecklistName, TaskDescription, IsCompleted, DueDate, AssignedTo });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadChecklists();
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
                    "UPDATE checklists SET checklist_name = @ChecklistName, task_description = @TaskDescription, is_completed = @IsCompleted, due_date = @DueDate, assigned_to = @AssignedTo WHERE id = @Id",
                    new { Id, ChecklistName, TaskDescription, IsCompleted, DueDate, AssignedTo });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadChecklists();
            return Page();
        }

        public IActionResult OnPostToggle(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("UPDATE checklists SET is_completed = NOT is_completed WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadChecklists();
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
                connection.Execute("DELETE FROM checklists WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadChecklists();
            return Page();
        }

        private void LoadChecklists()
        {
            using var connection = new MySqlConnection(_connectionString);
            Checklists = connection.Query<Checklist>("SELECT * FROM checklists ORDER BY due_date ASC").ToList();
        }
    }

    public class Checklist
    {
        public int Id { get; set; }
        public string ChecklistName { get; set; } = "";
        public string TaskDescription { get; set; } = "";
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
        public string AssignedTo { get; set; } = "";
    }
}