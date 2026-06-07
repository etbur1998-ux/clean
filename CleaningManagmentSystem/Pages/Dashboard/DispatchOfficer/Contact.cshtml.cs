using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    public class ContactModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public List<ContactMessage> Messages { get; set; } = new();

        [BindProperty]
        public ContactMessage NewMessage { get; set; } = new();

        [BindProperty]
        public int EditId { get; set; }

        [BindProperty]
        public ContactMessage EditMessage { get; set; } = new();

        [BindProperty]
        public string FilterStatus { get; set; } = "";

        [BindProperty]
        public string SearchTerm { get; set; } = "";

        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public int TotalContacts { get; set; }
        public int TotalMessages { get; set; }
        public int TotalCalls { get; set; }

        public ContactModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[Contact] OnGet called");

            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole");

            Console.WriteLine($"[Contact] Session - UserId: {userId}, UserName: {userName}, Role: {role}");

            if (userId == null || userId == 0)
            {
                Console.WriteLine("[Contact] User not logged in, redirecting to Login");
                return RedirectToPage("/Login");
            }

            if (role?.ToLower() != "dispatch_officer")
            {
                Console.WriteLine($"[Contact] User role {role} not authorized for this page");
                return RedirectToPage("/Login");
            }

            try
            {
                LoadMessages();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Error loading data: {ex.Message}");
                ErrorMessage = "Failed to load contact data";
            }

            return Page();
        }

        public IActionResult OnPostCreate()
        {
            Console.WriteLine("[Contact] OnPostCreate called");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(NewMessage.Name) || string.IsNullOrEmpty(NewMessage.Message))
            {
                ErrorMessage = "Name and Message are required";
                OnGet();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"INSERT INTO contact_messages (name, email, subject, message, phone, status, created_at) 
                           VALUES (@Name, @Email, @Subject, @Message, @Phone, @Status, @CreatedAt)";
                
                NewMessage.Status = "New";
                NewMessage.CreatedAt = DateTime.Now;

                connection.Execute(sql, new
                {
                    NewMessage.Name,
                    NewMessage.Email,
                    NewMessage.Subject,
                    NewMessage.Message,
                    NewMessage.Phone,
                    NewMessage.Status,
                    NewMessage.CreatedAt
                });

                Console.WriteLine($"[Contact] Created new message from: {NewMessage.Name}");
                SuccessMessage = "Message sent successfully";
                NewMessage = new ContactMessage();
                LoadMessages();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Error creating message: {ex.Message}");
                ErrorMessage = "Failed to send message";
            }

            return Page();
        }

        public IActionResult OnPostReply()
        {
            Console.WriteLine($"[Contact] OnPostReply called for ID: {EditId}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"UPDATE contact_messages SET reply = @Reply, status = 'Replied', replied_at = @RepliedAt 
                           WHERE id = @Id";
                
                connection.Execute(sql, new
                {
                    Id = EditId,
                    EditMessage.Reply,
                    RepliedAt = DateTime.Now
                });

                Console.WriteLine($"[Contact] Replied to message ID: {EditId}");
                SuccessMessage = "Reply sent successfully";
                LoadMessages();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Error sending reply: {ex.Message}");
                ErrorMessage = "Failed to send reply";
            }

            return Page();
        }

        public IActionResult OnPostUpdateStatus(int id, string status)
        {
            Console.WriteLine($"[Contact] OnPostUpdateStatus called for ID: {id}, Status: {status}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("UPDATE contact_messages SET status = @Status WHERE id = @Id", 
                    new { Id = id, Status = status });

                Console.WriteLine($"[Contact] Updated message status ID: {id} to {status}");
                SuccessMessage = "Message status updated successfully";
                LoadMessages();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Error updating status: {ex.Message}");
                ErrorMessage = "Failed to update message status";
            }

            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            Console.WriteLine($"[Contact] OnPostDelete called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM contact_messages WHERE id = @Id", new { Id = id });

                Console.WriteLine($"[Contact] Deleted message ID: {id}");
                SuccessMessage = "Message deleted successfully";
                LoadMessages();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Error deleting message: {ex.Message}");
                ErrorMessage = "Failed to delete message";
            }

            return Page();
        }

        private void LoadMessages()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            var sql = "SELECT * FROM contact_messages WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                sql += " AND status = @Status";
                parameters.Add("Status", FilterStatus);
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                sql += " AND (name LIKE @Search OR email LIKE @Search OR subject LIKE @Search OR message LIKE @Search)";
                parameters.Add("Search", $"%{SearchTerm}%");
            }

            sql += " ORDER BY created_at DESC";

            Messages = connection.Query<ContactMessage>(sql, parameters).ToList();
            Console.WriteLine($"[Contact] Loaded {Messages.Count} messages");
        }

        private void LoadStatistics()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            TotalContacts = connection.QueryFirstOrDefault<int?>("SELECT COUNT(DISTINCT name) FROM contact_messages") ?? 0;
            TotalMessages = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM contact_messages WHERE status != 'Archived'") ?? 0;
            TotalCalls = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM contact_messages WHERE subject LIKE '%call%' OR phone IS NOT NULL") ?? 0;
            
            Console.WriteLine($"[Contact] Statistics - Contacts: {TotalContacts}, Messages: {TotalMessages}, Calls: {TotalCalls}");
        }
    }
}
