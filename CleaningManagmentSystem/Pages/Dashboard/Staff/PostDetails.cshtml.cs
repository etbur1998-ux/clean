using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
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

        public IActionResult OnGet(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Fetch the post - ensure it is published and targeted to staff or all
                var post = connection.QueryFirstOrDefault<PostDetailItem>(
                    @"SELECT id as Id, title as Title, category as Category, content as Content, status as Status, 
                             training_id as TrainingId, is_pinned as IsPinned, priority as Priority, target_role as TargetRole, 
                             created_at as CreatedAt 
                      FROM posts 
                      WHERE id = @Id AND status = 'Published' AND (target_role = 'All' OR target_role = 'staff')",
                    new { Id = id });

                if (post == null)
                {
                    return RedirectToPage("./Post");
                }

                Post = post;

                // Load linked training details if any
                if (Post.TrainingId.HasValue)
                {
                    LinkedTraining = connection.QueryFirstOrDefault<TrainingDetailItem>(
                        @"SELECT id as Id, title as Title, trainer as Trainer, description as Description, 
                                 start_date as StartDate, end_date as EndDate, location as Location, 
                                 participants as Participants, status as Status, materials as Materials 
                          FROM trainings 
                          WHERE id = @Id",
                        new { Id = Post.TrainingId.Value });
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading post details: " + ex.Message;
                return Page();
            }
        }
    }
}
