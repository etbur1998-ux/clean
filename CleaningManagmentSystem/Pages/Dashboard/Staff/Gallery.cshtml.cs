using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class GalleryModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public string ImageUrl { get; set; } = "";

        [BindProperty]
        public string Category { get; set; } = "";

        [BindProperty]
        public DateTime UploadDate { get; set; }

        public List<GalleryItem> GalleryItems { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public GalleryModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadGallery();
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
                    "INSERT INTO gallery (title, description, image_url, category, upload_date) VALUES (@Title, @Description, @ImageUrl, @Category, @UploadDate)",
                    new { Title, Description, ImageUrl, Category, UploadDate });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadGallery();
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
                    "UPDATE gallery SET title = @Title, description = @Description, image_url = @ImageUrl, category = @Category, upload_date = @UploadDate WHERE id = @Id",
                    new { Id, Title, Description, ImageUrl, Category, UploadDate });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadGallery();
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
                connection.Execute("DELETE FROM gallery WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadGallery();
            return Page();
        }

        private void LoadGallery()
        {
            using var connection = new MySqlConnection(_connectionString);
            GalleryItems = connection.Query<GalleryItem>("SELECT * FROM gallery ORDER BY upload_date DESC").ToList();
        }
    }

    public class GalleryItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime UploadDate { get; set; }
    }
}