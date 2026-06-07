using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages
{
    public class BlogPostModel : PageModel
    {
        private readonly string _connectionString;

        public BlogPostModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public PostItem Post { get; set; } = new();
        public List<PostItem> RecentPosts { get; set; } = new();

        public class PostItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Content { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public DateTime? CreatedAt { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var post = connection.QueryFirstOrDefault<PostItem>(
                    @"SELECT id as Id, title as Title, category as Category, content as Content, image_url as ImageUrl, created_at as CreatedAt 
                      FROM posts 
                      WHERE id = @Id AND status = 'Published' AND target_role = 'All'",
                    new { Id = id });

                if (post == null)
                {
                    return RedirectToPage("/Blog");
                }

                Post = post;

                // Load 3 recent public posts for the sidebar/bottom widgets
                RecentPosts = connection.Query<PostItem>(
                    @"SELECT id as Id, title as Title, category as Category, content as Content, image_url as ImageUrl, created_at as CreatedAt 
                      FROM posts 
                      WHERE id != @Id AND status = 'Published' AND target_role = 'All' 
                      ORDER BY created_at DESC LIMIT 3",
                    new { Id = id }).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BlogPost] Error: {ex.Message}");
                return RedirectToPage("/Blog");
            }
        }
    }
}
