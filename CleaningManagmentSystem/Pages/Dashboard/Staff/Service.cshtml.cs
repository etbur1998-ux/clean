using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class ServiceModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string ServiceName { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public decimal Price { get; set; }

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public List<Service> Services { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public ServiceModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            // If editing (Id provided), load that specific service
            if (Id > 0)
            {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var service = connection.QueryFirstOrDefault<Service>(
                    "SELECT id AS Id, name AS ServiceName, description AS Description, price AS Price FROM services WHERE id = @Id", 
                    new { Id });
                if (service != null)
                {
                    ServiceName = service.ServiceName;
                    Description = service.Description;
                    Price = service.Price;
                }
                    else
                    {
                        ErrorMessage = "Service not found.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Error loading service: " + ex.Message;
                }
            }

            LoadServices();
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
                    "INSERT INTO services (name, description, price) VALUES (@ServiceName, @Description, @Price)",
                    new { ServiceName, Description, Price });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadServices();
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
                    "UPDATE services SET name = @ServiceName, description = @Description, price = @Price WHERE id = @Id",
                    new { Id, ServiceName, Description, Price });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadServices();
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
                connection.Execute("DELETE FROM services WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadServices();
            return Page();
        }

        private void LoadServices()
        {
            using var connection = new MySqlConnection(_connectionString);
            Services = connection.Query<Service>(
                "SELECT id AS Id, name AS ServiceName, description AS Description, price AS Price FROM services ORDER BY id DESC"
            ).ToList();
        }
    }

    public class Service
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
    }
}