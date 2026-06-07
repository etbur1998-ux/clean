using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages;

public class ResetPasswordModel : PageModel
{
    private readonly string _connectionString;

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Token { get; set; } = "";

    [BindProperty]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    public string ConfirmPassword { get; set; } = "";

    public string Message { get; set; } = "";
    public bool IsSuccess { get; set; }

    public ResetPasswordModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public IActionResult OnGet()
    {
        Email = Request.Query["email"].ToString();
        Token = Request.Query["token"].ToString();

        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Token))
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
        {
            Message = "Please enter and confirm your new password.";
            return Page();
        }

        if (NewPassword != ConfirmPassword)
        {
            Message = "Passwords do not match.";
            return Page();
        }

        if (NewPassword.Length < 6)
        {
            Message = "Password must be at least 6 characters.";
            return Page();
        }

        try
        {
            using var connection = new MySqlConnection(_connectionString);

            var user = connection.QueryFirstOrDefault<dynamic>(
                "SELECT id FROM users WHERE email = @Email AND reset_token = @Token AND reset_expires > NOW()",
                new { Email, Token });

            if (user == null)
            {
                Message = "Invalid or expired reset link. Please request a new one.";
                return Page();
            }

            connection.Execute(
                "UPDATE users SET password = @Password, reset_token = NULL, reset_expires = NULL, updated_at = NOW() WHERE email = @Email",
                new { Password = NewPassword, Email });

            IsSuccess = true;
            Message = "Password reset successful! You can now login with your new password.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ResetPassword] Error: {ex.Message}");
            Message = "An error occurred. Please try again.";
        }

        return Page();
    }
}