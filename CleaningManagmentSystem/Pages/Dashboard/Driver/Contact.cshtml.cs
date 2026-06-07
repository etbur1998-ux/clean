using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.Driver
{
    public class ContactModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public Contact? Contact { get; set; }

        [BindProperty]
        public Message? Message { get; set; }

        [BindProperty]
        public string? SearchTerm { get; set; }

        public IEnumerable<Contact>? Contacts { get; set; }

        public IEnumerable<Message>? Messages { get; set; }

        public ContactModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            LoadContacts();
            LoadMessages(userId.Value);
            return Page();
        }

        public IActionResult OnPostAddContact()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            if (Contact == null || string.IsNullOrEmpty(Contact.Name) || string.IsNullOrEmpty(Contact.Phone))
            {
                ModelState.AddModelError(string.Empty, "Name and Phone are required");
                LoadContacts();
                LoadMessages(userId.Value);
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                connection.Execute(
                    @"INSERT INTO contacts (driver_id, name, phone, email, company, notes, created_at)
                    VALUES (@DriverId, @Name, @Phone, @Email, @Company, @Notes, NOW())",
                    new
                    {
                        DriverId = userId,
                        Contact.Name,
                        Contact.Phone,
                        Contact.Email,
                        Contact.Company,
                        Contact.Notes
                    });

                TempData["SuccessMessage"] = "Contact added successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Add error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error adding contact. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUpdateContact()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            if (Contact == null || Contact.Id <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid contact ID");
                LoadContacts();
                LoadMessages(userId.Value);
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var affectedRows = connection.Execute(
                    @"UPDATE contacts 
                    SET name = @Name, phone = @Phone, email = @Email, 
                        company = @Company, notes = @Notes, updated_at = NOW()
                    WHERE id = @Id AND driver_id = @DriverId",
                    new
                    {
                        Contact.Name,
                        Contact.Phone,
                        Contact.Email,
                        Contact.Company,
                        Contact.Notes,
                        Id = Contact.Id,
                        DriverId = userId
                    });

                if (affectedRows > 0)
                {
                    TempData["SuccessMessage"] = "Contact updated successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Contact not found or unauthorized";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Update error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error updating contact. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteContact(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(
                    "DELETE FROM contacts WHERE id = @Id AND driver_id = @DriverId",
                    new { Id = id, DriverId = userId });
                TempData["SuccessMessage"] = "Contact deleted successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Contact] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting contact";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostSendMessage()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || userId <= 0 || role?.ToLower() != "driver")
            {
                return RedirectToPage("/Login");
            }

            if (Message == null || string.IsNullOrEmpty(Message.RecipientPhone) || string.IsNullOrEmpty(Message.Content))
            {
                ModelState.AddModelError(string.Empty, "Recipient and Message are required");
                LoadContacts();
                LoadMessages(userId.Value);
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                connection.Execute(
                    @"INSERT INTO messages (sender_id, recipient_phone, content, sent_at, status)
                    VALUES (@SenderId, @RecipientPhone, @Content, NOW(), 'sent')",
                    new
                    {
                        SenderId = userId,
                        Message.RecipientPhone,
                        Message.Content
                    });

                TempData["SuccessMessage"] = "Message sent successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Message] Send error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error sending message. Please try again.");
            }

            return RedirectToPage();
        }

        private void LoadContacts()
        {
            using var connection = new MySqlConnection(_connectionString);
            var allContacts = connection.Query<Contact>(
                "SELECT * FROM contacts WHERE driver_id = @DriverId ORDER BY name",
                new { DriverId = HttpContext.Session.GetInt32("UserId") });

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                Contacts = allContacts.Where(c =>
                    c.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Phone?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Email?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }
            else
            {
                Contacts = allContacts;
            }
        }

        private void LoadMessages(int userId)
        {
            using var connection = new MySqlConnection(_connectionString);
            Messages = connection.Query<Message>(
                @"SELECT * FROM messages 
                WHERE sender_id = @UserId OR recipient_phone IN 
                    (SELECT phone FROM contacts WHERE driver_id = @UserId)
                ORDER BY sent_at DESC LIMIT 50",
                new { UserId = userId });
        }
    }
}
