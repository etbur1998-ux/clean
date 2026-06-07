using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.WeredaMahberat
{
    public class CapitalModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public CapitalTransaction? Transaction { get; set; }

        [BindProperty]
        public string? FilterCategory { get; set; }

        [BindProperty]
        public string? FilterType { get; set; }

        [BindProperty]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty]
        public DateTime? FilterEndDate { get; set; }

        public IEnumerable<CapitalTransaction>? Transactions { get; set; }

        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public decimal CurrentBalance { get; set; }

        public CapitalModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[Capital] No UserName in session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"[Capital] OnGet called by UserName: {userName}");
            LoadTransactions();
            return Page();
        }

        public IActionResult OnPostAddTransaction()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (Transaction == null || string.IsNullOrEmpty(Transaction.TransactionType) || Transaction.Amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Transaction type and valid amount are required");
                LoadTransactions();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var lastTransaction = connection.QueryFirstOrDefault<CapitalTransaction>(
                    "SELECT balance FROM capital_transactions ORDER BY id DESC LIMIT 1");

                Transaction.Balance = lastTransaction?.Balance ?? 0;
                if (Transaction.TransactionType.ToLower() == "income")
                {
                    Transaction.Balance += Transaction.Amount;
                }
                else
                {
                    Transaction.Balance -= Transaction.Amount;
                }

                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                connection.Execute(
                    @"INSERT INTO capital_transactions (transaction_type, description, amount, balance, category, reference, notes, created_by, transaction_date, created_at)
                    VALUES (@TransactionType, @Description, @Amount, @Balance, @Category, @Reference, @Notes, @CreatedBy, @TransactionDate, NOW())",
                    new
                    {
                        Transaction.TransactionType,
                        Transaction.Description,
                        Transaction.Amount,
                        Transaction.Balance,
                        Transaction.Category,
                        Transaction.Reference,
                        Transaction.Notes,
                        CreatedBy = userId,
                        TransactionDate = Transaction.TransactionDate == default ? DateTime.Now : Transaction.TransactionDate
                    });

                Console.WriteLine($"[Capital] Added transaction: {Transaction.TransactionType} - {Transaction.Amount}");
                TempData["SuccessMessage"] = "Transaction added successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Add error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error adding transaction. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteTransaction(int id)
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
                    "DELETE FROM capital_transactions WHERE id = @Id",
                    new { Id = id });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[Capital] Deleted transaction ID: {id}");
                    TempData["SuccessMessage"] = "Transaction deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Transaction not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting transaction";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostRecalculateBalance()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var transactions = connection.Query<CapitalTransaction>(
                    "SELECT * FROM capital_transactions ORDER BY id").ToList();

                decimal runningBalance = 0;
                foreach (var tx in transactions)
                {
                    if (tx.TransactionType.ToLower() == "income")
                    {
                        runningBalance += tx.Amount;
                    }
                    else
                    {
                        runningBalance -= tx.Amount;
                    }

                    connection.Execute(
                        "UPDATE capital_transactions SET balance = @Balance WHERE id = @Id",
                        new { Balance = runningBalance, Id = tx.Id });
                }

                Console.WriteLine($"[Capital] Recalculated balance for {transactions.Count} transactions");
                TempData["SuccessMessage"] = "Balance recalculated successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Recalculate error: {ex.Message}");
                TempData["ErrorMessage"] = "Error recalculating balance";
            }

            return RedirectToPage();
        }

        private void LoadTransactions()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                string sql = "SELECT * FROM capital_transactions WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(FilterCategory))
                {
                    sql += " AND category = @Category";
                    parameters.Add("Category", FilterCategory);
                }

                if (!string.IsNullOrEmpty(FilterType))
                {
                    sql += " AND transaction_type = @Type";
                    parameters.Add("Type", FilterType);
                }

                if (FilterStartDate.HasValue)
                {
                    sql += " AND transaction_date >= @StartDate";
                    parameters.Add("StartDate", FilterStartDate.Value);
                }

                if (FilterEndDate.HasValue)
                {
                    sql += " AND transaction_date <= @EndDate";
                    parameters.Add("EndDate", FilterEndDate.Value);
                }

                sql += " ORDER BY transaction_date DESC, id DESC";

                var records = connection.Query<CapitalTransaction>(sql, parameters).ToList();

                Transactions = records;
                TotalIncome = records.Where(t => t.TransactionType?.ToLower() == "income").Sum(t => t.Amount);
                TotalExpense = records.Where(t => t.TransactionType?.ToLower() == "expense").Sum(t => t.Amount);
                CurrentBalance = TotalIncome - TotalExpense;

                Console.WriteLine($"[Capital] Loaded {records.Count} transactions, Balance: {CurrentBalance}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Capital] Load error: {ex.Message}");
                Transactions = new List<CapitalTransaction>();
            }
        }
    }
}
