using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class PostDetailsModel : PageModel
    {
        private readonly string _connectionString;

        public PostDetailsModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public PostDetailItem Post { get; set; } = new();
        public TrainingDetailItem? LinkedTraining { get; set; }
        public List<TrainingListItem> TrainingList { get; set; } = new();

        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public class PostDetailItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Content { get; set; } = "";
            public string Status { get; set; } = "";
            public int? TrainingId { get; set; }
            public bool IsPinned { get; set; }
            public string Priority { get; set; } = "Normal";
            public string TargetRole { get; set; } = "All";
            public DateTime? CreatedAt { get; set; }
            public DateTime? PublishedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class TrainingDetailItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Trainer { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Location { get; set; } = "";
            public int Participants { get; set; }
            public string Status { get; set; } = "";
            public string Materials { get; set; } = "";
        }

        public class TrainingListItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
        }

        public IActionResult OnGet(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            LoadData(id);

            if (Post.Id == 0)
            {
                return RedirectToPage("./Post");
            }

            return Page();
        }

        private void LoadData(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                // Fetch the post
                var post = connection.QueryFirstOrDefault<PostDetailItem>(
                    "SELECT id as Id, title as Title, category as Category, content as Content, status as Status, training_id as TrainingId, is_pinned as IsPinned, priority as Priority, target_role as TargetRole, created_at as CreatedAt, published_at as PublishedAt, updated_at as UpdatedAt FROM posts WHERE id = @Id",
                    new { Id = id });

                if (post != null)
                {
                    Post = post;

                    // Fetch linked training if any
                    if (Post.TrainingId.HasValue)
                    {
                        LinkedTraining = connection.QueryFirstOrDefault<TrainingDetailItem>(
                            "SELECT id as Id, title as Title, trainer as Trainer, description as Description, start_date as StartDate, end_date as EndDate, location as Location, participants as Participants, status as Status, materials as Materials FROM trainings WHERE id = @Id",
                            new { Id = Post.TrainingId.Value });
                    }
                }

                // Load all trainings for dropdown in edit panel
                TrainingList = connection.Query<TrainingListItem>("SELECT id, title FROM trainings").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading post details: " + ex.Message;
            }
        }

        public IActionResult OnPostUpdateAttributes(int id, string category, string priority, string targetRole, bool isPinned, int? trainingId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                string query = @"UPDATE posts 
                               SET category = @Category, priority = @Priority, target_role = @TargetRole, is_pinned = @IsPinned, training_id = @TrainingId, updated_at = NOW()
                               WHERE id = @Id";

                connection.Execute(query, new {
                    Id = id,
                    Category = category,
                    Priority = priority,
                    TargetRole = targetRole,
                    IsPinned = isPinned ? 1 : 0,
                    TrainingId = trainingId
                });

                SuccessMessage = "Post attributes updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to update attributes: " + ex.Message;
            }

            LoadData(id);
            return Page();
        }

        public IActionResult OnPostUpdateContent(int id, string title, string content)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                string query = @"UPDATE posts 
                               SET title = @Title, content = @Content, updated_at = NOW()
                               WHERE id = @Id";

                connection.Execute(query, new {
                    Id = id,
                    Title = title,
                    Content = content
                });

                SuccessMessage = "Post content updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to update content: " + ex.Message;
            }

            LoadData(id);
            return Page();
        }

        public IActionResult OnPostChangeStatus(int id, string status)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                string query = "UPDATE posts SET status = @Status, published_at = IF(@Status = 'Published', NOW(), published_at), updated_at = NOW() WHERE id = @Id";
                connection.Execute(query, new { Id = id, Status = status });

                SuccessMessage = $"Post status changed to {status}!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to change status: " + ex.Message;
            }

            LoadData(id);
            return Page();
        }
    }
}
