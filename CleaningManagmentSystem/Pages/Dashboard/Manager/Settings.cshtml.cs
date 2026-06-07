using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Services;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    public class SettingsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly EmailService _emailService;

        public SettingsModel(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        // ── Pricing ──
        [BindProperty]
        public decimal DefaultPricePerKg { get; set; } = 1.4m;

        // ── Transport Defaults ──
        [BindProperty]
        public decimal DefaultTransportRatePerKg { get; set; } = 2.5m;
        [BindProperty]
        public decimal DefaultKg { get; set; } = 100m;

        // ── Create User ──
        [BindProperty]
        public string NewUserName { get; set; } = "";
        [BindProperty]
        public string NewUserEmail { get; set; } = "";
        [BindProperty]
        public string NewUserRole { get; set; } = "staff";
        [BindProperty]
        public string NewUserPhone { get; set; } = "";

        // ── Reset Password ──
        [BindProperty]
        public string ResetEmail { get; set; } = "";
        [BindProperty]
        public string NewPassword { get; set; } = "";
        [BindProperty]
        public string ConfirmNewPassword { get; set; } = "";

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await EnsureSettingsTableExistsAsync(connection);

            var value = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");
            if (value != null && decimal.TryParse(value, out var price))
                DefaultPricePerKg = price;

            var rateVal = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultTransportRatePerKg'");
            if (rateVal != null && decimal.TryParse(rateVal, out var rate))
                DefaultTransportRatePerKg = rate;

            var kgVal = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultKg'");
            if (kgVal != null && decimal.TryParse(kgVal, out var kg))
                DefaultKg = kg;
        }

        // ── Save Pricing ──
        public async Task<IActionResult> OnPostSavePricingAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await EnsureSettingsTableExistsAsync(connection);

            var exists = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");

            if (exists > 0)
                await connection.ExecuteAsync(
                    "UPDATE system_settings SET setting_value = @Value WHERE setting_key = 'DefaultPricePerKg'",
                    new { Value = DefaultPricePerKg.ToString() });
            else
                await connection.ExecuteAsync(
                    "INSERT INTO system_settings (setting_key, setting_value) VALUES ('DefaultPricePerKg', @Value)",
                    new { Value = DefaultPricePerKg.ToString() });

            TempData["SuccessMessage"] = "Pricing settings saved successfully.";
            return RedirectToPage();
        }

        // ── Save Transport Defaults ──
        public async Task<IActionResult> OnPostSaveTransportDefaultsAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await EnsureSettingsTableExistsAsync(connection);

            foreach (var (key, val) in new[]
            {
                ("DefaultTransportRatePerKg", DefaultTransportRatePerKg.ToString()),
                ("DefaultKg",                 DefaultKg.ToString()),
            })
            {
                var exists = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM system_settings WHERE setting_key = @Key", new { Key = key });
                if (exists > 0)
                    await connection.ExecuteAsync(
                        "UPDATE system_settings SET setting_value = @Val WHERE setting_key = @Key",
                        new { Val = val, Key = key });
                else
                    await connection.ExecuteAsync(
                        "INSERT INTO system_settings (setting_key, setting_value) VALUES (@Key, @Val)",
                        new { Key = key, Val = val });
            }

            TempData["SuccessMessage"] = "Transport defaults saved successfully.";
            return RedirectToPage();
        }

        // ── Create User Account (auto-generate password & email it) ──
        public async Task<IActionResult> OnPostCreateUserAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUserName) || string.IsNullOrWhiteSpace(NewUserEmail))
            {
                TempData["ErrorMessage"] = "Please fill in all required fields (Name and Email).";
                return RedirectToPage();
            }

            using var connection = new MySqlConnection(_connectionString);

            var existing = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM users WHERE email = @Email", new { Email = NewUserEmail });

            if (existing > 0)
            {
                TempData["ErrorMessage"] = "A user with this email already exists.";
                return RedirectToPage();
            }

            // Generate a random default password
            var defaultPassword = GeneratePassword();

            await connection.ExecuteAsync(
                @"INSERT INTO users (name, email, password, role, phone, is_active, created_at)
                  VALUES (@Name, @Email, @Password, @Role, @Phone, 1, NOW())",
                new { Name = NewUserName, Email = NewUserEmail, Password = defaultPassword, Role = NewUserRole, Phone = NewUserPhone });

            // Send welcome email with credentials
            var emailBody = $@"
                <html><body style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #0d6efd;'>Welcome to Yeka Cleaning Management System</h2>
                    <p>Hello <strong>{NewUserName}</strong>,</p>
                    <p>Your account has been created. Here are your login credentials:</p>
                    <div style='background:#f8f9fa; border-left:4px solid #0d6efd; padding:16px; border-radius:6px; margin:16px 0;'>
                        <p style='margin:4px 0;'><strong>Email:</strong> {NewUserEmail}</p>
                        <p style='margin:4px 0;'><strong>Password:</strong> <span style='font-size:1.2em; color:#d63384;'>{defaultPassword}</span></p>
                        <p style='margin:4px 0;'><strong>Role:</strong> {NewUserRole}</p>
                    </div>
                    <p>Please log in and change your password after your first login.</p>
                    <p style='color:#999; font-size:12px;'>— Yeka Cleaning System</p>
                </body></html>";

            var (emailSent, emailError) = await _emailService.SendEmailAsync(
                NewUserEmail,
                "Your Yeka Cleaning Account Credentials",
                emailBody);

            if (emailSent)
                TempData["SuccessMessage"] = $"Account for '{NewUserName}' created successfully. Login credentials have been sent to {NewUserEmail}.";
            else
                TempData["SuccessMessage"] = $"Account for '{NewUserName}' created. Note: Email could not be sent ({emailError}). Default password: {defaultPassword}";

            return RedirectToPage();
        }

        // ── Reset Password ──
        public async Task<IActionResult> OnPostResetPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(ResetEmail) || string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["ErrorMessage"] = "Please enter the user email and new password.";
                return RedirectToPage();
            }

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match. Please try again.";
                return RedirectToPage();
            }

            using var connection = new MySqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, name FROM users WHERE email = @Email AND is_active = 1", new { Email = ResetEmail });

            if (user == null)
            {
                TempData["ErrorMessage"] = "No active user found with that email address.";
                return RedirectToPage();
            }

            await connection.ExecuteAsync(
                "UPDATE users SET password = @Password, updated_at = NOW() WHERE email = @Email",
                new { Password = NewPassword, Email = ResetEmail });

            TempData["SuccessMessage"] = $"Password for '{user.name}' has been reset successfully.";
            return RedirectToPage();
        }

        private static string GeneratePassword(int length = 10)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789@#!";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task EnsureSettingsTableExistsAsync(MySqlConnection connection)
        {
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS system_settings (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    setting_key VARCHAR(100) UNIQUE NOT NULL,
                    setting_value TEXT,
                    updated_at DATETIME DEFAULT NOW() ON UPDATE NOW()
                )");
        }
    }
}
