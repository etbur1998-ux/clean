using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages
{
    public class BlogModel : PageModel
    {
        private readonly string _connectionString;

        public BlogModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<BlogPostItem> BlogPosts { get; set; } = new();
        public BlogPostItem? FeaturedPost { get; set; }

        public class BlogPostItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Content { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public bool IsPinned { get; set; }
            public string Priority { get; set; } = "Normal";
            public DateTime? CreatedAt { get; set; }
        }

        public void OnGet()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Fetch published posts targetting All (public)
                var allPosts = connection.Query<BlogPostItem>(
                    @"SELECT id as Id, title as Title, category as Category, content as Content, image_url as ImageUrl, 
                             is_pinned as IsPinned, priority as Priority, created_at as CreatedAt 
                      FROM posts 
                      WHERE status = 'Published' AND target_role = 'All' 
                      ORDER BY is_pinned DESC, created_at DESC").ToList();

                if (allPosts.Any())
                {
                    // The first pinned post (or latest if none are pinned) is featured
                    FeaturedPost = allPosts.FirstOrDefault(p => p.IsPinned) ?? allPosts.FirstOrDefault();
                    
                    if (FeaturedPost != null)
                    {
                        BlogPosts = allPosts.Where(p => p.Id != FeaturedPost.Id).ToList();
                    }
                    else
                    {
                        BlogPosts = allPosts;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Blog] Error loading posts: {ex.Message}");
            }
        }
    }
}
