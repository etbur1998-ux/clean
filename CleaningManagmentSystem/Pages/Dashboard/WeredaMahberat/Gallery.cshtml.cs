using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.WeredaMahberat
{
    public class GalleryModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public Gallery? GalleryItem { get; set; }

        [BindProperty]
        public string? FilterCategory { get; set; }

        [BindProperty]
        public string? SearchTerm { get; set; }

        public IEnumerable<Gallery>? GalleryItems { get; set; }

        public IEnumerable<string>? Categories { get; set; }

        public GalleryModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[Gallery] No UserName in session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"[Gallery] OnGet called by UserName: {userName}");
            LoadGallery();
            LoadCategories();
            return Page();
        }

        public IActionResult OnPostAddItem()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (GalleryItem == null || string.IsNullOrEmpty(GalleryItem.Title))
            {
                ModelState.AddModelError(string.Empty, "Title is required");
                LoadGallery();
                LoadCategories();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                connection.Execute(
                    @"INSERT INTO gallery (title, description, image_url, category, views, is_active, created_at)
                    VALUES (@Title, @Description, @ImageUrl, @Category, 0, @IsActive, NOW())",
                    new
                    {
                        GalleryItem.Title,
                        GalleryItem.Description,
                        GalleryItem.ImageUrl,
                        GalleryItem.Category,
                        IsActive = true
                    });

                Console.WriteLine($"[Gallery] Added gallery item: {GalleryItem.Title}");
                TempData["SuccessMessage"] = "Gallery item added successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Add error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error adding gallery item. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUpdateItem()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (GalleryItem == null || GalleryItem.Id <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid gallery item ID");
                LoadGallery();
                LoadCategories();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var affectedRows = connection.Execute(
                    @"UPDATE gallery 
                    SET title = @Title, description = @Description, image_url = @ImageUrl, category = @Category
                    WHERE id = @Id",
                    new
                    {
                        GalleryItem.Id,
                        GalleryItem.Title,
                        GalleryItem.Description,
                        GalleryItem.ImageUrl,
                        GalleryItem.Category
                    });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[Gallery] Updated gallery item ID: {GalleryItem.Id}");
                    TempData["SuccessMessage"] = "Gallery item updated successfully";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Gallery item not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Update error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error updating gallery item. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteItem(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var affectedRows = connection.Execute(
                    "DELETE FROM gallery WHERE id = @Id",
                    new { Id = id });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[Gallery] Deleted gallery item ID: {id}");
                    TempData["SuccessMessage"] = "Gallery item deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gallery item not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting gallery item";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostToggleActive(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var item = connection.QueryFirstOrDefault<Gallery>(
                    "SELECT is_active FROM gallery WHERE id = @Id",
                    new { Id = id });

                if (item != null)
                {
                    var newStatus = !item.IsActive;
                    connection.Execute(
                        "UPDATE gallery SET is_active = @IsActive WHERE id = @Id",
                        new { IsActive = newStatus, Id = id });

                    Console.WriteLine($"[Gallery] Toggled active status for ID: {id} to {newStatus}");
                    TempData["SuccessMessage"] = $"Item {(newStatus ? "activated" : "deactivated")} successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gallery item not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Toggle error: {ex.Message}");
                TempData["ErrorMessage"] = "Error toggling item status";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostIncrementViews(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "UPDATE gallery SET views = views + 1 WHERE id = @Id",
                    new { Id = id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Increment views error: {ex.Message}");
            }

            return RedirectToPage();
        }

        private void LoadGallery()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                string sql = "SELECT * FROM gallery WHERE 1=1";
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

                GalleryItems = connection.Query<Gallery>(sql, parameters).ToList();
                Console.WriteLine($"[Gallery] Loaded {GalleryItems.Count()} items");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Load error: {ex.Message}");
                GalleryItems = new List<Gallery>();
            }
        }

        private void LoadCategories()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                Categories = connection.Query<string>(
                    "SELECT DISTINCT category FROM gallery WHERE category IS NOT NULL AND category != '' ORDER BY category");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gallery] Load categories error: {ex.Message}");
                Categories = new List<string>();
            }
        }
    }
}
