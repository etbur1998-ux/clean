using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    public class GalleryModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public List<Gallery> Galleries { get; set; } = new();

        [BindProperty]
        public Gallery NewGallery { get; set; } = new();

        [BindProperty]
        public int EditId { get; set; }

        [BindProperty]
        public Gallery EditGallery { get; set; } = new();

        [BindProperty]
        public string FilterCategory { get; set; } = "";

        [BindProperty]
        public string SearchTerm { get; set; } = "";

        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public int TotalPhotos { get; set; }
        public int TotalAlbums { get; set; }
        public int TotalEvents { get; set; }

        public GalleryModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[Gallery] OnGet called");

            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole");

            Console.WriteLine($"[Gallery] Session - UserId: {userId}, UserName: {userName}, Role: {role}");

            if (userId == null || userId == 0)
            {
                Console.WriteLine("[Gallery] User not logged in, redirecting to Login");
                return RedirectToPage("/Login");
            }

            if (role?.ToLower() != "dispatch_officer")
            {
                Console.WriteLine($"[Gallery] User role {role} not authorized for this page");
                return RedirectToPage("/Login");
            }

            try
            {
                LoadGalleries();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Error loading data: {ex.Message}");
                ErrorMessage = "Failed to load gallery data";
            }

            return Page();
        }

        public IActionResult OnPostCreate()
        {
            Console.WriteLine("[Gallery] OnPostCreate called");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(NewGallery.Title))
            {
                ErrorMessage = "Title is required";
                OnGet();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"INSERT INTO galleries (title, description, image_url, category, views, is_active, created_at) 
                           VALUES (@Title, @Description, @ImageUrl, @Category, @Views, @IsActive, @CreatedAt)";
                
                NewGallery.Views = 0;
                NewGallery.IsActive = true;
                NewGallery.CreatedAt = DateTime.Now;

                connection.Execute(sql, new
                {
                    NewGallery.Title,
                    NewGallery.Description,
                    NewGallery.ImageUrl,
                    NewGallery.Category,
                    NewGallery.Views,
                    NewGallery.IsActive,
                    NewGallery.CreatedAt
                });

                Console.WriteLine($"[Gallery] Created new gallery item: {NewGallery.Title}");
                SuccessMessage = "Gallery item created successfully";
                NewGallery = new Gallery();
                LoadGalleries();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Error creating gallery: {ex.Message}");
                ErrorMessage = "Failed to create gallery item";
            }

            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            Console.WriteLine($"[Gallery] OnPostUpdate called for ID: {EditId}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"UPDATE galleries SET title = @Title, description = @Description, 
                           image_url = @ImageUrl, category = @Category, is_active = @IsActive 
                           WHERE id = @Id";
                
                connection.Execute(sql, new
                {
                    Id = EditId,
                    EditGallery.Title,
                    EditGallery.Description,
                    EditGallery.ImageUrl,
                    EditGallery.Category,
                    EditGallery.IsActive
                });

                Console.WriteLine($"[Gallery] Updated gallery ID: {EditId}");
                SuccessMessage = "Gallery item updated successfully";
                LoadGalleries();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Error updating gallery: {ex.Message}");
                ErrorMessage = "Failed to update gallery item";
            }

            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            Console.WriteLine($"[Gallery] OnPostDelete called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM galleries WHERE id = @Id", new { Id = id });

                Console.WriteLine($"[Gallery] Deleted gallery ID: {id}");
                SuccessMessage = "Gallery item deleted successfully";
                LoadGalleries();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Error deleting gallery: {ex.Message}");
                ErrorMessage = "Failed to delete gallery item";
            }

            return Page();
        }

        public IActionResult OnPostToggleActive(int id)
        {
            Console.WriteLine($"[Gallery] OnPostToggleActive called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var gallery = connection.QueryFirstOrDefault<Gallery>("SELECT * FROM galleries WHERE id = @Id", new { Id = id });
                
                if (gallery != null)
                {
                    connection.Execute("UPDATE galleries SET is_active = @IsActive WHERE id = @Id", 
                        new { Id = id, IsActive = !gallery.IsActive });
                    
                    Console.WriteLine($"[Gallery] Toggled gallery ID: {id} to {(!gallery.IsActive)}");
                    SuccessMessage = "Gallery status updated successfully";
                }

                LoadGalleries();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Error toggling gallery: {ex.Message}");
                ErrorMessage = "Failed to update gallery status";
            }

            return Page();
        }

        private void LoadGalleries()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            var sql = "SELECT * FROM galleries WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(FilterCategory))
            {
                sql += " AND category = @Category";
                parameters.Add("Category", FilterCategory);
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                sql += " AND (title LIKE @Search OR description LIKE @Search)";
                parameters.Add("Search", $"%{SearchTerm}%");
            }

            sql += " ORDER BY created_at DESC";

            Galleries = connection.Query<Gallery>(sql, parameters).ToList();
            Console.WriteLine($"[Gallery] Loaded {Galleries.Count} gallery items");
        }

        private void LoadStatistics()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            TotalPhotos = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM galleries WHERE is_active = 1") ?? 0;
            TotalAlbums = connection.QueryFirstOrDefault<int?>("SELECT COUNT(DISTINCT category) FROM galleries WHERE category IS NOT NULL") ?? 0;
            TotalEvents = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM galleries WHERE category = 'Event' AND is_active = 1") ?? 0;
            
            Console.WriteLine($"[Gallery] Statistics - Photos: {TotalPhotos}, Albums: {TotalAlbums}, Events: {TotalEvents}");
        }
    }
}
