using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.WeredaMahberat
{
    public class ContactModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public ContactMessage? ContactMessage { get; set; }

        [BindProperty]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public string? FilterStatus { get; set; }

        public IEnumerable<ContactMessage>? Messages { get; set; }

        public int UnreadCount { get; set; }

        public WeredaContactInfo? ContactInfo { get; set; }

        public ContactModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[Contact] No UserName in session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"[Contact] OnGet called by UserName: {userName}");
            LoadMessages();
            LoadContactInfo();
            return Page();
        }

        public IActionResult OnPostSendMessage()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (ContactMessage == null || string.IsNullOrEmpty(ContactMessage.Name) || string.IsNullOrEmpty(ContactMessage.Message))
            {
                ModelState.AddModelError(string.Empty, "Name and Message are required");
                LoadMessages();
                LoadContactInfo();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                connection.Execute(
                    @"INSERT INTO contact_messages (name, email, subject, message, phone, status, created_at)
                    VALUES (@Name, @Email, @Subject, @Message, @Phone, 'sent', NOW())",
                    new
                    {
                        ContactMessage.Name,
                        ContactMessage.Email,
                        ContactMessage.Subject,
                        ContactMessage.Message,
                        ContactMessage.Phone
                    });

                Console.WriteLine($"[Contact] Sent message from: {ContactMessage.Name}");
                TempData["SuccessMessage"] = "Message sent successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Send error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error sending message. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostReply(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (ContactMessage == null || string.IsNullOrEmpty(ContactMessage.Reply))
            {
                ModelState.AddModelError(string.Empty, "Reply message is required");
                LoadMessages();
                LoadContactInfo();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var affectedRows = connection.Execute(
                    @"UPDATE contact_messages 
                    SET reply = @Reply, status = 'replied', replied_at = NOW()
                    WHERE id = @Id",
                    new
                    {
                        Id = id,
                        ContactMessage.Reply
                    });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[Contact] Replied to message ID: {id}");
                    TempData["SuccessMessage"] = "Reply sent successfully";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Message not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Reply error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error sending reply. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostMarkAsRead(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var affectedRows = connection.Execute(
                    "UPDATE contact_messages SET status = 'read' WHERE id = @Id AND status = 'sent'",
                    new { Id = id });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[Contact] Marked message ID: {id} as read");
                    TempData["SuccessMessage"] = "Message marked as read";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Mark as read error: {ex.Message}");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteMessage(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var affectedRows = connection.Execute(
                    "DELETE FROM contact_messages WHERE id = @Id",
                    new { Id = id });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[Contact] Deleted message ID: {id}");
                    TempData["SuccessMessage"] = "Message deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Message not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting message";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostSaveContactInfo()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (ContactInfo == null)
            {
                ModelState.AddModelError(string.Empty, "Contact information is required");
                LoadMessages();
                LoadContactInfo();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var existing = connection.QueryFirstOrDefault<WeredaContactInfo>(
                    "SELECT id FROM wereda_contact_info WHERE id = 1");

                if (existing != null)
                {
                    connection.Execute(
                        @"UPDATE wereda_contact_info 
                        SET phone = @Phone, email = @Email, address = @Address, 
                            office_hours = @OfficeHours, updated_at = NOW()
                        WHERE id = 1",
                        new
                        {
                            ContactInfo.Phone,
                            ContactInfo.Email,
                            ContactInfo.Address,
                            ContactInfo.OfficeHours
                        });
                }
                else
                {
                    connection.Execute(
                        @"INSERT INTO wereda_contact_info (phone, email, address, office_hours, created_at)
                        VALUES (@Phone, @Email, @Address, @OfficeHours, NOW())",
                        new
                        {
                            ContactInfo.Phone,
                            ContactInfo.Email,
                            ContactInfo.Address,
                            ContactInfo.OfficeHours
                        });
                }

                Console.WriteLine("[Contact] Saved contact information");
                TempData["SuccessMessage"] = "Contact information saved successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Save info error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error saving contact information. Please try again.");
            }

            return RedirectToPage();
        }

        private void LoadMessages()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                string sql = "SELECT * FROM contact_messages WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(FilterStatus))
                {
                    sql += " AND status = @Status";
                    parameters.Add("Status", FilterStatus);
                }

                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    sql += " AND (name LIKE @Search OR message LIKE @Search OR email LIKE @Search)";
                    parameters.Add("Search", $"%{SearchTerm}%");
                }

                sql += " ORDER BY created_at DESC";

                var messages = connection.Query<ContactMessage>(sql, parameters).ToList();

                Messages = messages;
                UnreadCount = messages.Count(m => m.Status?.ToLower() == "sent");

                Console.WriteLine($"[Contact] Loaded {messages.Count} messages, Unread: {UnreadCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Load error: {ex.Message}");
                Messages = new List<ContactMessage>();
            }
        }

        private void LoadContactInfo()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                ContactInfo = connection.QueryFirstOrDefault<WeredaContactInfo>(
                    "SELECT * FROM wereda_contact_info WHERE id = 1");

                if (ContactInfo == null)
                {
                    ContactInfo = new WeredaContactInfo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Load info error: {ex.Message}");
                ContactInfo = new WeredaContactInfo();
            }
        }
    }

    public class WeredaContactInfo
    {
        public int Id { get; set; }
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string OfficeHours { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}