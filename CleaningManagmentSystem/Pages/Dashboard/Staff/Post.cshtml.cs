using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class PostModel : PageModel
    {
        private readonly string _connectionString;

        public List<PostItem> Posts { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public PostModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            LoadPosts();
            return Page();
        }

        private void LoadPosts()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                // Load only published posts targeted to All or Staff, ordered by pinned first
                Posts = connection.Query<PostItem>(
                    @"SELECT id as Id, title as Title, category as Category, content as Content, status as Status, 
                             training_id as TrainingId, is_pinned as IsPinned, priority as Priority, created_at as CreatedAt 
                      FROM posts 
                      WHERE status = 'Published' AND (target_role = 'All' OR target_role = 'staff') 
                      ORDER BY is_pinned DESC, created_at DESC").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading posts: " + ex.Message;
            }
        }
    }

    public class PostItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Content { get; set; } = "";
        public string Status { get; set; } = "";
        public int? TrainingId { get; set; }
        public bool IsPinned { get; set; }
        public string Priority { get; set; } = "Normal";
        public DateTime? CreatedAt { get; set; }
    }
}
