using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class PostModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty] public int      Id          { get; set; }
        [BindProperty] public string   Title       { get; set; } = "";
        [BindProperty] public string   Category    { get; set; } = "";
        [BindProperty] public string   PostContent { get; set; } = "";
        [BindProperty] public string   Status      { get; set; } = "Draft";
        [BindProperty] public string   SearchQuery { get; set; } = "";
        [BindProperty] public int?     TrainingId  { get; set; }
        [BindProperty] public bool     IsPinned    { get; set; }
        [BindProperty] public string   Priority    { get; set; } = "Normal";
        [BindProperty] public string   TargetRole  { get; set; } = "All";
        [BindProperty] public string?  ImageUrl    { get; set; }
        [BindProperty] public IFormFile? ImageFile { get; set; }

        [BindProperty(SupportsGet = true)] public string FilterCategory { get; set; } = "";

        public List<PostItem>          Posts        { get; set; } = new();
        public List<TrainingListItem>  TrainingList { get; set; } = new();
        public string ErrorMessage   { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public class TrainingListItem
        {
            public int    Id    { get; set; }
            public string Title { get; set; } = "";
        }

        public PostModel(IConfiguration configuration)
            => _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

        // ── GET ──────────────────────────────────────────────────────────────
        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToPage("/Login");
            LoadPosts();
            LoadTrainings();
            return Page();
        }

        // ── POST ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToPage("/Login");

            var action = Request.Form["action"].ToString();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                switch (action)
                {
                    case "create":
                        await CreatePostAsync(connection);
                        SuccessMessage = $"Post '{Title}' created! " +
                            (Status == "Published" ? "✅ Now visible on /Blog." : "📝 Saved as Draft.");
                        break;
                    case "update":
                        await UpdatePostAsync(connection);
                        SuccessMessage = "Post updated!";
                        break;
                    case "delete":
                        connection.Execute(
                            "UPDATE posts SET status='Deleted', updated_at=NOW() WHERE id=@Id",
                            new { Id });
                        SuccessMessage = "Post deleted.";
                        break;
                    case "publish":
                        connection.Execute(
                            "UPDATE posts SET status='Published', target_role='All', updated_at=NOW() WHERE id=@Id",
                            new { Id });
                        SuccessMessage = "✅ Post published — now visible at /Blog.";
                        break;
                    case "unpublish":
                        connection.Execute(
                            "UPDATE posts SET status='Draft', updated_at=NOW() WHERE id=@Id",
                            new { Id });
                        SuccessMessage = "Post moved back to Draft.";
                        break;
                    default:
                        ErrorMessage = $"Unknown action: {action}";
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }

            LoadPosts();
            LoadTrainings();
            return Page();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private async Task<string?> UploadImageAsync()
        {
            if (ImageFile == null || ImageFile.Length == 0) return null;
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await ImageFile.CopyToAsync(stream);
            return $"/uploads/posts/{fileName}";
        }

        private async Task CreatePostAsync(MySqlConnection connection)
        {
            var uploaded   = await UploadImageAsync();
            var finalImg   = !string.IsNullOrEmpty(uploaded) ? uploaded : ImageUrl;
            var adminId    = HttpContext.Session.GetInt32("UserId") ?? 0;
            var adminName  = HttpContext.Session.GetString("UserName") ?? "";

            connection.Execute(@"
                INSERT INTO posts
                  (title, category, content, status, image_url,
                   training_id, is_pinned, priority, target_role,
                   author, author_id, created_at, updated_at)
                VALUES
                  (@Title, @Category, @PostContent, @Status, @ImageUrl,
                   @TrainingId, @IsPinned, @Priority, @TargetRole,
                   @Author, @AuthorId, NOW(), NOW())",
                new {
                    Title, Category, PostContent, Status,
                    ImageUrl   = finalImg,
                    TrainingId,
                    IsPinned   = IsPinned ? 1 : 0,
                    Priority, TargetRole,
                    Author     = adminName,
                    AuthorId   = adminId
                });
        }

        private async Task UpdatePostAsync(MySqlConnection connection)
        {
            var uploaded = await UploadImageAsync();
            string? finalImg = uploaded;
            if (string.IsNullOrEmpty(finalImg))
            {
                // Use URL field if filled, else keep existing
                finalImg = !string.IsNullOrEmpty(ImageUrl)
                    ? ImageUrl
                    : connection.QueryFirstOrDefault<string>(
                          "SELECT image_url FROM posts WHERE id=@Id", new { Id });
            }

            connection.Execute(@"
                UPDATE posts SET
                  title=@Title, category=@Category, content=@PostContent,
                  status=@Status, image_url=@ImageUrl,
                  training_id=@TrainingId, is_pinned=@IsPinned,
                  priority=@Priority, target_role=@TargetRole, updated_at=NOW()
                WHERE id=@Id",
                new {
                    Title, Category, PostContent, Status,
                    ImageUrl   = finalImg,
                    TrainingId,
                    IsPinned   = IsPinned ? 1 : 0,
                    Priority, TargetRole, Id
                });
        }

        private void LoadTrainings()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                TrainingList = connection
                    .Query<TrainingListItem>("SELECT id, title FROM trainings ORDER BY title")
                    .ToList();
            }
            catch { }
        }

        private void LoadPosts()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var q = "SELECT * FROM posts WHERE status != 'Deleted'";
                var p = new DynamicParameters();
                if (!string.IsNullOrEmpty(FilterCategory))
                { q += " AND category=@C"; p.Add("C", FilterCategory); }
                if (!string.IsNullOrEmpty(SearchQuery))
                { q += " AND (title LIKE @S OR content LIKE @S)"; p.Add("S", $"%{SearchQuery}%"); }
                q += " ORDER BY is_pinned DESC, id DESC";
                Posts = connection.Query<PostItem>(q, p).ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading posts: {ex.Message}";
            }
        }
    }

    public class PostItem
    {
        public int       Id          { get; set; }
        public string    Title       { get; set; } = "";
        public string    Category    { get; set; } = "";
        public string    Content     { get; set; } = "";
        public string    Status      { get; set; } = "";
        public string?   ImageUrl    { get; set; }
        public int?      TrainingId  { get; set; }
        public bool      IsPinned    { get; set; }
        public string    Priority    { get; set; } = "Normal";
        public string    TargetRole  { get; set; } = "All";
        public string    Author      { get; set; } = "";
        public DateTime? CreatedAt   { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
