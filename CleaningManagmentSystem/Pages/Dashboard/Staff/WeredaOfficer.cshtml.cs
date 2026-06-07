using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class WeredaOfficerModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string FullName { get; set; } = "";

        [BindProperty]
        public string Phone { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Wereda { get; set; } = "";

        [BindProperty]
        public string Subcity { get; set; } = "";

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<WeredaOfficer> Officers { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public WeredaOfficerModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadOfficers();
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
                    "INSERT INTO wereda_officers (full_name, phone, email, wereda, subcity, is_active) VALUES (@FullName, @Phone, @Email, @Wereda, @Subcity, @IsActive)",
                    new { FullName, Phone, Email, Wereda, Subcity, IsActive });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadOfficers();
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
                    "UPDATE wereda_officers SET full_name = @FullName, phone = @Phone, email = @Email, wereda = @Wereda, subcity = @Subcity, is_active = @IsActive WHERE id = @Id",
                    new { Id, FullName, Phone, Email, Wereda, Subcity, IsActive });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadOfficers();
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
                connection.Execute("DELETE FROM wereda_officers WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadOfficers();
            return Page();
        }

        private void LoadOfficers()
        {
            using var connection = new MySqlConnection(_connectionString);
            Officers = connection.Query<WeredaOfficer>("SELECT * FROM wereda_officers ORDER BY full_name ASC").ToList();
        }
    }

    public class WeredaOfficer
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Wereda { get; set; } = "";
        public string Subcity { get; set; } = "";
        public bool IsActive { get; set; }
    }
}