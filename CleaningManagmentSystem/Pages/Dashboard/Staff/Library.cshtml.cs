using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class LibraryModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Author { get; set; } = "";

        [BindProperty]
        public string DocumentType { get; set; } = "";

        [BindProperty]
        public string FileUrl { get; set; } = "";

        [BindProperty]
        public DateTime PublishedDate { get; set; }

        public List<LibraryItem> LibraryItems { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public LibraryModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadLibrary();
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
                    "INSERT INTO library (title, author, document_type, file_url, published_date) VALUES (@Title, @Author, @DocumentType, @FileUrl, @PublishedDate)",
                    new { Title, Author, DocumentType, FileUrl, PublishedDate });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadLibrary();
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
                    "UPDATE library SET title = @Title, author = @Author, document_type = @DocumentType, file_url = @FileUrl, published_date = @PublishedDate WHERE id = @Id",
                    new { Id, Title, Author, DocumentType, FileUrl, PublishedDate });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadLibrary();
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
                connection.Execute("DELETE FROM library WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadLibrary();
            return Page();
        }

        private void LoadLibrary()
        {
            using var connection = new MySqlConnection(_connectionString);
            LibraryItems = connection.Query<LibraryItem>("SELECT * FROM library ORDER BY published_date DESC").ToList();
        }
    }

    public class LibraryItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public DateTime PublishedDate { get; set; }
    }
}