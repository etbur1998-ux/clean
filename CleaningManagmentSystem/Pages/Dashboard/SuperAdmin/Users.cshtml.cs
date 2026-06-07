using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using UserModel = CleaningManagmentSystem.Models.User;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class UsersModel : PageModel
    {
        private readonly string _cs;

        public List<UserModel> UsersList  { get; set; } = new();
        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage   { get; set; } = "";
        public string SearchQuery    { get; set; } = "";

        public static readonly HashSet<string> MobileRoles = new(StringComparer.OrdinalIgnoreCase)
            { "driver", "outsource", "PrivateCompanyRep", "manager" };

        public UsersModel(IConfiguration cfg)
            => _cs = cfg.GetConnectionString("DefaultConnection") ?? "";

        // GET only — all mutations go through /admin/users/* controller actions
        public IActionResult OnGet(
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? ok = null,
            [FromQuery] string? err = null)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToPage("/Login");

            SearchQuery    = searchQuery ?? "";
            SuccessMessage = ok  ?? "";
            ErrorMessage   = err ?? "";

            LoadUsers();
            return Page();
        }

        private void LoadUsers()
        {
            try
            {
                using var db = new MySqlConnection(_cs);
                UsersList = string.IsNullOrEmpty(SearchQuery)
                    ? db.Query<UserModel>("SELECT * FROM users ORDER BY id DESC").ToList()
                    : db.Query<UserModel>(
                        @"SELECT * FROM users
                          WHERE name LIKE @s OR email LIKE @s OR phone LIKE @s OR role LIKE @s
                          ORDER BY id DESC",
                        new { s = $"%{SearchQuery}%" }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Users.LoadUsers] {ex.Message}");
                ErrorMessage = "Error loading users.";
            }
        }
    }
}
