using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class RegisterReceiptModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string ReceiptNumber { get; set; } = "";

        [BindProperty]
        public string ClientName { get; set; } = "";

        [BindProperty]
        public string ServiceType { get; set; } = "";

        [BindProperty]
        public decimal Amount { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; } = "Cash";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public DateTime? ReceiptDate { get; set; }

        [BindProperty]
        public string SearchQuery { get; set; } = "";

        public List<Receipt> Receipts { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public RegisterReceiptModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[RegisterReceipt] OnGet called");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[RegisterReceipt] No session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            LoadReceipts();
            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine($"[RegisterReceipt] OnPost called with action");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            var action = Request.Form["action"];
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                if (action == "create")
                {
                    ReceiptNumber = GenerateReceiptNumber(connection);
                    CreateReceipt(connection);
                    SuccessMessage = "Receipt created!";
                }
                else if (action == "update")
                {
                    UpdateReceipt(connection);
                    SuccessMessage = "Receipt updated!";
                }
                else if (action == "delete")
                {
                    DeleteReceipt(connection);
                    SuccessMessage = "Receipt deleted!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegisterReceipt] Database error: {ex.Message}");
                ErrorMessage = "Database error occurred.";
            }

            LoadReceipts();
            return Page();
        }

        private string GenerateReceiptNumber(MySqlConnection connection)
        {
            var count = connection.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM receipts") + 1;
            return $"REC-2026-{count:D3}";
        }

        private void LoadReceipts()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = "SELECT * FROM receipts ORDER BY id DESC";
                
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = @"SELECT * FROM receipts 
                              WHERE receipt_number LIKE @Search OR client_name LIKE @Search 
                              ORDER BY id DESC";
                    Receipts = connection.Query<Receipt>(query, new { Search = $"%{SearchQuery}%" }).ToList();
                }
                else
                {
                    Receipts = connection.Query<Receipt>(query).ToList();
                }
                
                Console.WriteLine($"[RegisterReceipt] Loaded {Receipts.Count} receipts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegisterReceipt] Load error: {ex.Message}");
                ErrorMessage = "Error loading receipts.";
            }
        }

        private void CreateReceipt(MySqlConnection connection)
        {
            string query = @"INSERT INTO receipts (receipt_number, client_name, service_type, amount, payment_method, description, receipt_date, created_at) 
                           VALUES (@ReceiptNumber, @ClientName, @ServiceType, @Amount, @PaymentMethod, @Description, @ReceiptDate, NOW())";
            
            connection.Execute(query, new { 
                ReceiptNumber, 
                ClientName, 
                ServiceType, 
                Amount, 
                PaymentMethod, 
                Description, 
                ReceiptDate 
            });
            
            Console.WriteLine($"[RegisterReceipt] Created: {ReceiptNumber}");
        }

        private void UpdateReceipt(MySqlConnection connection)
        {
            string query = @"UPDATE receipts 
                           SET client_name = @ClientName, service_type = @ServiceType, amount = @Amount,
                               payment_method = @PaymentMethod, description = @Description, receipt_date = @ReceiptDate,
                               updated_at = NOW()
                           WHERE id = @Id";
            
            connection.Execute(query, new { 
                ClientName, 
                ServiceType, 
                Amount, 
                PaymentMethod, 
                Description, 
                ReceiptDate, 
                Id 
            });
            
            Console.WriteLine($"[RegisterReceipt] Updated ID: {Id}");
        }

        private void DeleteReceipt(MySqlConnection connection)
        {
            string query = "DELETE FROM receipts WHERE id = @Id";
            connection.Execute(query, new { Id });
            Console.WriteLine($"[RegisterReceipt] Deleted ID: {Id}");
        }
    }

    public class Receipt
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? ReceiptDate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}