using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    [IgnoreAntiforgeryToken]
    public class MahberatsModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Name { get; set; } = "";

        [BindProperty]
        public int WeredaId { get; set; }

        [BindProperty]
        public string WeredaName { get; set; } = "";

        [BindProperty]
        public string ContactPerson { get; set; } = "";

        [BindProperty]
        public string Phone { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Address { get; set; } = "";

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<Mahberat> Mahberats { get; set; } = new();
        public List<Wereda> Weredas { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public MahberatsModel(IConfiguration configuration)
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
            LoadMahberats();
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
                // Get wereda name if wereda_id provided
                if (WeredaId > 0)
                {
                    var wereda = connection.QueryFirstOrDefault<Wereda>("SELECT name FROM weredas WHERE id = @WeredaId", new { WeredaId });
                    if (wereda != null)
                    {
                        WeredaName = wereda.Name;
                    }
                }

                connection.Execute(
                    "INSERT INTO mahberats (name, wereda_id, wereda_name, contact_person, phone, email, address, is_active, created_at) VALUES (@Name, @WeredaId, @WeredaName, @ContactPerson, @Phone, @Email, @Address, @IsActive, NOW())",
                    new { Name, WeredaId, WeredaName, ContactPerson, Phone, Email, Address, IsActive });
                SuccessMessage = "Mahberat added successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding mahberat: " + ex.Message;
            }

            LoadMahberats();
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
                // Get wereda name if wereda_id provided
                if (WeredaId > 0)
                {
                    var wereda = connection.QueryFirstOrDefault<Wereda>("SELECT name FROM weredas WHERE id = @WeredaId", new { WeredaId });
                    if (wereda != null)
                    {
                        WeredaName = wereda.Name;
                    }
                }

                connection.Execute(
                    "UPDATE mahberats SET name = @Name, wereda_id = @WeredaId, wereda_name = @WeredaName, contact_person = @ContactPerson, phone = @Phone, email = @Email, address = @Address, is_active = @IsActive, updated_at = NOW() WHERE id = @Id",
                    new { Id, Name, WeredaId, WeredaName, ContactPerson, Phone, Email, Address, IsActive });
                SuccessMessage = "Mahberat updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating mahberat: " + ex.Message;
            }

            LoadMahberats();
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
                connection.Execute("DELETE FROM mahberats WHERE id = @Id", new { Id = id });
                SuccessMessage = "Mahberat deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting mahberat: " + ex.Message;
            }

            LoadMahberats();
            return RedirectToPage();
        }

        private void LoadWeredas()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                Weredas = connection.Query<Wereda>("SELECT * FROM weredas WHERE is_active = 1 ORDER BY name ASC").ToList();
            }
            catch { }
        }

        private void LoadMahberats()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                Mahberats = connection.Query<Mahberat>(@"
                    SELECT 
                        id AS Id, 
                        name AS Name, 
                        wereda_id AS WeredaId, 
                        wereda_name AS WeredaName, 
                        contact_person AS ContactPerson, 
                        phone AS Phone, 
                        email AS Email, 
                        address AS Address, 
                        is_active AS IsActive, 
                        created_at AS CreatedAt, 
                        updated_at AS UpdatedAt 
                    FROM mahberats 
                    ORDER BY name ASC").ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading mahberats: " + ex.Message;
            }
        }
    }
}