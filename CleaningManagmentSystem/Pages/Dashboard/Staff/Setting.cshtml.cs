using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class SettingModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int UserId { get; set; }

        [BindProperty]
        public string Name { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Phone { get; set; } = "";

        [BindProperty]
        public string CurrentPassword { get; set; } = "";

        [BindProperty]
        public string NewPassword { get; set; } = "";

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

        [BindProperty]
        public bool EmailNotifications { get; set; } = true;

        [BindProperty]
        public bool SmsNotifications { get; set; } = true;

        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public SettingModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            UserId = userId.Value;
            LoadUserSettings();
            return Page();
        }

        public IActionResult OnPostUpdateProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "UPDATE users SET name = @Name, phone = @Phone WHERE id = @UserId",
                    new { Name, Phone, UserId = userId });
                
                HttpContext.Session.SetString("UserName", Name);
                SuccessMessage = "Profile updated successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadUserSettings();
            return Page();
        }

        public IActionResult OnPostChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var user = connection.QueryFirstOrDefault<User>(
                    "SELECT id, password FROM users WHERE id = @UserId",
                    new { UserId = userId });

                if (user == null)
                {
                    ErrorMessage = "User not found";
                    LoadUserSettings();
                    return Page();
                }

                if (user.Password != CurrentPassword)
                {
                    ErrorMessage = "Current password is incorrect";
                    LoadUserSettings();
                    return Page();
                }

                if (NewPassword != ConfirmPassword)
                {
                    ErrorMessage = "New passwords do not match";
                    LoadUserSettings();
                    return Page();
                }

                connection.Execute(
                    "UPDATE users SET password = @NewPassword WHERE id = @UserId",
                    new { NewPassword, UserId = userId });

                SuccessMessage = "Password changed successfully";
                CurrentPassword = "";
                NewPassword = "";
                ConfirmPassword = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadUserSettings();
            return Page();
        }

        public IActionResult OnPostUpdateNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "UPDATE users SET email_notifications = @EmailNotifications, sms_notifications = @SmsNotifications WHERE id = @UserId",
                    new { EmailNotifications, SmsNotifications, UserId = userId });

                SuccessMessage = "Notification preferences updated successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadUserSettings();
            return Page();
        }

        private void LoadUserSettings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return;

            using var connection = new MySqlConnection(_connectionString);
            var user = connection.QueryFirstOrDefault<User>(
                "SELECT id, name, email, phone, email_notifications, sms_notifications FROM users WHERE id = @UserId",
                new { UserId = userId });

            if (user != null)
            {
                UserId = user.Id;
                Name = user.Name;
                Email = user.Email;
                Phone = user.Phone ?? "";
                EmailNotifications = user.EmailNotifications;
                SmsNotifications = user.SmsNotifications;
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Phone { get; set; }
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = true;
    }
}