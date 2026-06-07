using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    [IgnoreAntiforgeryToken]
    public class WeredasModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Name { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public string Subcity { get; set; } = "";

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<Wereda> Weredas { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public WeredasModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "manager")
            {
                return RedirectToPage("/Login");
            }

            LoadWeredas();
            return Page();
        }

        public IActionResult OnPostAdd()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "INSERT INTO weredas (name, description, subcity, is_active, created_at) VALUES (@Name, @Description, @Subcity, @IsActive, NOW())",
                    new { Name, Description, Subcity, IsActive });
                SuccessMessage = "Wereda added successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding wereda: " + ex.Message;
            }

            LoadWeredas();
            return RedirectToPage();
        }

        public IActionResult OnPostUpdate()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "UPDATE weredas SET name = @Name, description = @Description, subcity = @Subcity, is_active = @IsActive, updated_at = NOW() WHERE id = @Id",
                    new { Id, Name, Description, Subcity, IsActive });
                SuccessMessage = "Wereda updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating wereda: " + ex.Message;
            }

            LoadWeredas();
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM weredas WHERE id = @Id", new { Id = id });
                SuccessMessage = "Wereda deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting wereda: " + ex.Message;
            }

            LoadWeredas();
            return RedirectToPage();
        }

        private void LoadWeredas()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                Weredas = connection.Query<Wereda>(@"
                    SELECT 
                        id AS Id,
                        name AS Name,
                        description AS Description,
                        subcity AS Subcity,
                        is_active AS IsActive,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM weredas 
                    ORDER BY name ASC").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading weredas: " + ex.Message;
            }
        }
    }
}