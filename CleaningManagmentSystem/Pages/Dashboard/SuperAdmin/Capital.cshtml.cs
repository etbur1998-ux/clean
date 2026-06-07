using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class CapitalModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string TransactionType { get; set; } = "Income";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public decimal Amount { get; set; }

        [BindProperty]
        public string Category { get; set; } = "";

        [BindProperty]
        public string Reference { get; set; } = "";

        [BindProperty]
        public string Notes { get; set; } = "";

        [BindProperty]
        public DateTime? TransactionDate { get; set; }

        [BindProperty]
        public string SearchQuery { get; set; } = "";

        public List<CapitalTransaction> Transactions { get; set; } = new();
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalCapital { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public CapitalModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[Capital] OnGet called");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[Capital] No session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            LoadTransactions();
            CalculateTotals();
            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine($"[Capital] OnPost called with action");
            
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
                    CreateTransaction(connection);
                    SuccessMessage = "Transaction created!";
                }
                else if (action == "update")
                {
                    UpdateTransaction(connection);
                    SuccessMessage = "Transaction updated!";
                }
                else if (action == "delete")
                {
                    DeleteTransaction(connection);
                    SuccessMessage = "Transaction deleted!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Database error: {ex.Message}");
                ErrorMessage = "Database error occurred.";
            }

            LoadTransactions();
            CalculateTotals();
            return Page();
        }

        private void LoadTransactions()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = "SELECT * FROM capital_transactions ORDER BY id DESC";
                
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = @"SELECT * FROM capital_transactions 
                              WHERE description LIKE @Search OR category LIKE @Search OR reference LIKE @Search 
                              ORDER BY id DESC";
                    Transactions = connection.Query<CapitalTransaction>(query, new { Search = $"%{SearchQuery}%" }).ToList();
                }
                else
                {
                    Transactions = connection.Query<CapitalTransaction>(query).ToList();
                }
                
                Console.WriteLine($"[Capital] Loaded {Transactions.Count} transactions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Load error: {ex.Message}");
                ErrorMessage = "Error loading transactions.";
            }
        }

        private void CalculateTotals()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                TotalIncome = connection.QuerySingleOrDefault<decimal?>("SELECT COALESCE(SUM(amount), 0) FROM capital_transactions WHERE transaction_type = 'Income'") ?? 0m;
                TotalExpense = connection.QuerySingleOrDefault<decimal?>("SELECT COALESCE(SUM(amount), 0) FROM capital_transactions WHERE transaction_type = 'Expense'") ?? 0m;
                TotalCapital = TotalIncome - TotalExpense;
                
                Console.WriteLine($"[Capital] Totals - Income: {TotalIncome}, Expense: {TotalExpense}, Capital: {TotalCapital}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Calculate error: {ex.Message}");
            }
        }

        private void CreateTransaction(MySqlConnection connection)
        {
            string query = @"INSERT INTO capital_transactions 
                           (transaction_type, description, amount, category, reference, notes, transaction_date, created_at) 
                           VALUES (@TransactionType, @Description, @Amount, @Category, @Reference, @Notes, @TransactionDate, NOW())";
            
            connection.Execute(query, new { 
                TransactionType, 
                Description, 
                Amount, 
                Category, 
                Reference, 
                Notes, 
                TransactionDate 
            });
            
            Console.WriteLine($"[Capital] Created: {TransactionType} - {Amount}");
        }

        private void UpdateTransaction(MySqlConnection connection)
        {
            string query = @"UPDATE capital_transactions 
                           SET transaction_type = @TransactionType, description = @Description, amount = @Amount,
                               category = @Category, reference = @Reference, notes = @Notes, transaction_date = @TransactionDate,
                               updated_at = NOW()
                           WHERE id = @Id";
            
            connection.Execute(query, new { 
                TransactionType, 
                Description, 
                Amount, 
                Category, 
                Reference, 
                Notes, 
                TransactionDate, 
                Id 
            });
            
            Console.WriteLine($"[Capital] Updated ID: {Id}");
        }

        private void DeleteTransaction(MySqlConnection connection)
        {
            string query = "DELETE FROM capital_transactions WHERE id = @Id";
            connection.Execute(query, new { Id });
            Console.WriteLine($"[Capital] Deleted ID: {Id}");
        }
    }

    public class CapitalTransaction
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string Category { get; set; } = "";
        public string Reference { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime? TransactionDate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}