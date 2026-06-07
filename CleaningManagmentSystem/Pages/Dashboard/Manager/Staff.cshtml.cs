using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    public class StaffModel : PageModel
    {
        private readonly string _connectionString;

        public StaffModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public List<UserModel> StaffList { get; set; } = new();

        public void OnGet()
        {
            using var connection = new MySqlConnection(_connectionString);
            StaffList = connection.Query<UserModel>(
                "SELECT id, name, email, role, phone, address, is_active FROM users WHERE role != 'superadmin' ORDER BY id DESC"
            ).ToList();
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Execute("UPDATE users SET is_active = NOT is_active WHERE id = @Id", new { Id = id });
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateStaff(int id, string name, string phone, string newPassword)
        {
            using var connection = new MySqlConnection(_connectionString);
            if (!string.IsNullOrEmpty(newPassword))
            {
                connection.Execute("UPDATE users SET name = @Name, phone = @Phone, password = @Password WHERE id = @Id", 
                    new { Name = name, Phone = phone, Password = newPassword, Id = id });
            }
            else
            {
                connection.Execute("UPDATE users SET name = @Name, phone = @Phone WHERE id = @Id", 
                    new { Name = name, Phone = phone, Id = id });
            }
            return RedirectToPage();
        }

        public class UserModel
        {
            public int id { get; set; }
            public string name { get; set; } = "";
            public string email { get; set; } = "";
            public string role { get; set; } = "";
            public string phone { get; set; } = "";
            public string address { get; set; } = "";
            public bool is_active { get; set; }
        }
    }
}
