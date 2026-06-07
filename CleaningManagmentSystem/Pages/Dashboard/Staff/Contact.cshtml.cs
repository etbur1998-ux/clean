using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class ContactModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string SenderName { get; set; } = "";

        [BindProperty]
        public string SenderEmail { get; set; } = "";

        [BindProperty]
        public string Subject { get; set; } = "";

        [BindProperty]
        public string Message { get; set; } = "";

        [BindProperty]
        public DateTime SentDate { get; set; }

        [BindProperty]
        public bool IsRead { get; set; }

        public List<ContactMessage> Messages { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public ContactModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            LoadMessages();
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
                    "INSERT INTO contact (sender_name, sender_email, subject, message, sent_date, is_read) VALUES (@SenderName, @SenderEmail, @Subject, @Message, @SentDate, @IsRead)",
                    new { SenderName, SenderEmail, Subject, Message, SentDate = DateTime.Now, IsRead = false });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadMessages();
            return Page();
        }

        public IActionResult OnPostMarkAsRead(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("UPDATE contact SET is_read = 1 WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadMessages();
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
                connection.Execute("DELETE FROM contact WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            LoadMessages();
            return Page();
        }

        private void LoadMessages()
        {
            using var connection = new MySqlConnection(_connectionString);
            Messages = connection.Query<ContactMessage>("SELECT * FROM contact ORDER BY sent_date DESC").ToList();
        }
    }

    public class ContactMessage
    {
        public int Id { get; set; }
        public string SenderName { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
    }
}