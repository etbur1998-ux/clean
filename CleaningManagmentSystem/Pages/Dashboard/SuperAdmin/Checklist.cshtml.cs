using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class ChecklistModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public string AssignedTo { get; set; } = "";

        [BindProperty]
        public string Category { get; set; } = "";

        [BindProperty]
        public string Priority { get; set; } = "Medium";

        [BindProperty]
        public string Status { get; set; } = "Pending";

        [BindProperty]
        public DateTime? DueDate { get; set; }

        [BindProperty]
        public string SearchQuery { get; set; } = "";

        public List<ChecklistItem> ChecklistItems { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public ChecklistModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[Checklist] OnGet called");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[Checklist] No session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            LoadChecklist();
            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine($"[Checklist] OnPost called with action");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            var action = Request.Form["action"];
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                if (action == "create")
                {
                    CreateChecklist(connection);
                    SuccessMessage = "Checklist item created!";
                }
                else if (action == "update")
                {
                    UpdateChecklist(connection);
                    SuccessMessage = "Checklist item updated!";
                }
                else if (action == "delete")
                {
                    DeleteChecklist(connection);
                    SuccessMessage = "Checklist item deleted!";
                }
                else if (action == "toggle")
                {
                    ToggleStatus(connection);
                    SuccessMessage = "Status updated!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Checklist] Database error: {ex.Message}");
                ErrorMessage = "Database error occurred.";
            }

            LoadChecklist();
            return Page();
        }

        private void LoadChecklist()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = "SELECT * FROM checklist ORDER BY id DESC";
                
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = @"SELECT * FROM checklist 
                              WHERE title LIKE @Search OR description LIKE @Search 
                              ORDER BY id DESC";
                    ChecklistItems = connection.Query<ChecklistItem>(query, new { Search = $"%{SearchQuery}%" }).ToList();
                }
                else
                {
                    ChecklistItems = connection.Query<ChecklistItem>(query).ToList();
                }
                
                Console.WriteLine($"[Checklist] Loaded {ChecklistItems.Count} items");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Checklist] Load error: {ex.Message}");
                ErrorMessage = "Error loading checklist.";
            }
        }

        private void CreateChecklist(MySqlConnection connection)
        {
            string query = @"INSERT INTO checklist (title, description, assigned_to, category, priority, status, due_date, created_at) 
                           VALUES (@Title, @Description, @AssignedTo, @Category, @Priority, @Status, @DueDate, NOW())";
            
            connection.Execute(query, new { 
                Title, 
                Description, 
                AssignedTo, 
                Category, 
                Priority, 
                Status, 
                DueDate 
            });
            
            Console.WriteLine($"[Checklist] Created: {Title}");
        }

        private void UpdateChecklist(MySqlConnection connection)
        {
            string query = @"UPDATE checklist 
                           SET title = @Title, description = @Description, assigned_to = @AssignedTo,
                               category = @Category, priority = @Priority, status = @Status,
                               due_date = @DueDate, updated_at = NOW()
                           WHERE id = @Id";
            
            connection.Execute(query, new { 
                Title, 
                Description, 
                AssignedTo, 
                Category, 
                Priority, 
                Status, 
                DueDate, 
                Id 
            });
            
            Console.WriteLine($"[Checklist] Updated ID: {Id}");
        }

        private void DeleteChecklist(MySqlConnection connection)
        {
            string query = "DELETE FROM checklist WHERE id = @Id";
            connection.Execute(query, new { Id });
            Console.WriteLine($"[Checklist] Deleted ID: {Id}");
        }

        private void ToggleStatus(MySqlConnection connection)
        {
            string newStatus = Status == "Completed" ? "Pending" : "Completed";
            string query = "UPDATE checklist SET status = @Status, updated_at = NOW() WHERE id = @Id";
            connection.Execute(query, new { Status = newStatus, Id });
            Console.WriteLine($"[Checklist] Toggled status for ID: {Id}");
        }
    }

    public class ChecklistItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string AssignedTo { get; set; } = "";
        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}