using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Services;

namespace CleaningManagmentSystem.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly string _connectionString;
    private readonly EmailService _emailService;

    [BindProperty]
    public string Email { get; set; } = "";

    public string Message { get; set; } = "";
    public bool IsSuccess { get; set; }

    public ForgotPasswordModel(IConfiguration configuration, EmailService emailService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _emailService = emailService;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrEmpty(Email))
        {
            Message = "Please enter your email address.";
            return Page();
        }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                var user = connection.QueryFirstOrDefault<dynamic>(
                    "SELECT id, name FROM users WHERE email = @Email AND is_active = 1",
                    new { Email });
                
                if (user == null)
                {
                    Message = "If this email exists, a reset link will be sent.";
                    IsSuccess = true;
                    return Page();
                }
                
                var resetToken = Guid.NewGuid().ToString("N") + DateTime.Now.Ticks.ToString("X");
                connection.Execute(
                    "UPDATE users SET reset_token = @Token, reset_expires = @Expires WHERE email = @Email",
                    new { Token = resetToken, Expires = DateTime.Now.AddHours(1), Email });
                
                var emailSent = await _emailService.SendPasswordResetEmailAsync(Email, resetToken);
                
                if (emailSent)
                {
                    Message = "If this email exists, a password reset link has been sent.";
                    IsSuccess = true;
                    Console.WriteLine($"[ForgotPassword] Reset email sent to {Email}");
                }
                else
                {
                    Message = "Could not send reset email. Please try again later or contact support.";
                    IsSuccess = false;
                    Console.WriteLine($"[ForgotPassword] Failed to send reset email to {Email}");
                }
            }
        catch (Exception ex)
        {
            Console.WriteLine($"[ForgotPassword] Error: {ex.Message}");
            Console.WriteLine($"[ForgotPassword] Stack: {ex.StackTrace}");
            Message = $"Error: {ex.Message}. Please try again.";
        }

        return Page();
    }
}