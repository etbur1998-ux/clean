using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.WeredaMahberat
{
    public class MonthlyReceiptModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public MonthlyReceipt? Receipt { get; set; }

        [BindProperty]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public string? FilterMonth { get; set; }

        [BindProperty]
        public int? FilterYear { get; set; }

        public IEnumerable<MonthlyReceipt>? Receipts { get; set; }

        public MonthlyReceiptModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[MonthlyReceipt] No UserName in session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"[MonthlyReceipt] OnGet called by UserName: {userName}");
            LoadReceipts();
            return Page();
        }

        public IActionResult OnPostAddReceipt()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (Receipt == null || string.IsNullOrEmpty(Receipt.ReceiptNumber))
            {
                ModelState.AddModelError(string.Empty, "Receipt Number is required");
                LoadReceipts();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var existingReceipt = connection.QueryFirstOrDefault<MonthlyReceipt>(
                    "SELECT id FROM monthly_receipts WHERE receipt_number = @ReceiptNumber",
                    new { Receipt.ReceiptNumber });

                if (existingReceipt != null)
                {
                    ModelState.AddModelError(string.Empty, "Receipt number already exists");
                    LoadReceipts();
                    return Page();
                }

                connection.Execute(
                    @"INSERT INTO monthly_receipts (receipt_number, month, year, total_amount, paid_amount, balance, status, source, created_at, updated_at)
                    VALUES (@ReceiptNumber, @Month, @Year, @TotalAmount, @PaidAmount, @Balance, @Status, @Source, NOW(), NOW())",
                    new
                    {
                        Receipt.ReceiptNumber,
                        Receipt.Month,
                        Receipt.Year,
                        Receipt.TotalAmount,
                        Receipt.PaidAmount,
                        Receipt.Balance,
                        Receipt.Status,
                        Receipt.Source
                    });

                Console.WriteLine($"[MonthlyReceipt] Added new receipt: {Receipt.ReceiptNumber}");
                TempData["SuccessMessage"] = "Receipt added successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonthlyReceipt] Add error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error adding receipt. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUpdateReceipt()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (Receipt == null || Receipt.Id <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid receipt ID");
                LoadReceipts();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                Receipt.Balance = Receipt.TotalAmount - Receipt.PaidAmount;

                var affectedRows = connection.Execute(
                    @"UPDATE monthly_receipts 
                    SET receipt_number = @ReceiptNumber, month = @Month, year = @Year, 
                        total_amount = @TotalAmount, paid_amount = @PaidAmount, balance = @Balance,
                        status = @Status, source = @Source, updated_at = NOW()
                    WHERE id = @Id",
                    new
                    {
                        Receipt.Id,
                        Receipt.ReceiptNumber,
                        Receipt.Month,
                        Receipt.Year,
                        Receipt.TotalAmount,
                        Receipt.PaidAmount,
                        Receipt.Balance,
                        Receipt.Status,
                        Receipt.Source
                    });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[MonthlyReceipt] Updated receipt ID: {Receipt.Id}");
                    TempData["SuccessMessage"] = "Receipt updated successfully";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Receipt not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonthlyReceipt] Update error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error updating receipt. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostApproveLevel1(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var affectedRows = connection.Execute(
                    "UPDATE monthly_receipts SET status = 'Level 1 Approved', updated_at = NOW() WHERE id = @Id AND status = 'Pending'",
                    new { Id = id });

                if (affectedRows > 0) TempData["SuccessMessage"] = "First approval (Level 1) completed successfully. Forwarded to Manager.";
                else TempData["ErrorMessage"] = "Receipt not found or already approved.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonthlyReceipt] Approve error: {ex.Message}");
                TempData["ErrorMessage"] = "Error approving receipt";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteReceipt(int id)
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
                    "DELETE FROM monthly_receipts WHERE id = @Id",
                    new { Id = id });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[MonthlyReceipt] Deleted receipt ID: {id}");
                    TempData["SuccessMessage"] = "Receipt deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Receipt not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonthlyReceipt] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting receipt";
            }

            return RedirectToPage();
        }

        private void LoadReceipts()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Load both manual monthly_receipts AND completed transport requests
                string sql = "SELECT * FROM monthly_receipts WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(FilterMonth))
                {
                    sql += " AND month = @Month";
                    parameters.Add("Month", FilterMonth);
                }
                if (FilterYear.HasValue && FilterYear > 0)
                {
                    sql += " AND year = @Year";
                    parameters.Add("Year", FilterYear.Value);
                }
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    sql += " AND (receipt_number LIKE @Search OR source LIKE @Search)";
                    parameters.Add("Search", $"%{SearchTerm}%");
                }
                sql += " ORDER BY year DESC, month DESC, created_at DESC";
                var manualReceipts = connection.Query<MonthlyReceipt>(sql, parameters).ToList();

                // Load paid transport requests and merge as MonthlyReceipt objects
                try
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    string trSql = @"
                        SELECT id, request_number, mahberat_user_id, mahberat_user_name,
                               pickup_location, destination, driver_name, vehicle_plate,
                               actual_kilogram, transport_cost, status,
                               paid_at, staff_action_at, updated_at, created_at
                        FROM transport_requests
                        WHERE status IN ('Paid','StaffApproved','ReceiptVerified')";
                    var trParams = new DynamicParameters();
                    if (userId.HasValue)
                    {
                        trSql += " AND mahberat_user_id = @UserId";
                        trParams.Add("UserId", userId.Value);
                    }
                    trSql += " ORDER BY COALESCE(paid_at, staff_action_at, updated_at, created_at) DESC";

                    var transportRows = connection.Query<dynamic>(trSql, trParams).ToList();
                    foreach (var tr in transportRows)
                    {
                        // Use best available date: paid_at > staff_action_at > updated_at > created_at
                        DateTime? completedAt = tr.paid_at ?? tr.staff_action_at ?? tr.updated_at ?? tr.created_at;
                        string statusLabel = (string)tr.status switch {
                            "Paid"           => "Paid",
                            "StaffApproved"  => "Approved",
                            "ReceiptVerified"=> "Verified",
                            _                => (string)tr.status
                        };
                        decimal cost = (decimal)(tr.transport_cost ?? 0m);
                        manualReceipts.Add(new MonthlyReceipt
                        {
                            Id            = (int)tr.id,
                            ReceiptNumber = (string)tr.request_number,
                            Month         = completedAt?.ToString("MMMM") ?? DateTime.Now.ToString("MMMM"),
                            Year          = completedAt?.Year ?? DateTime.Now.Year,
                            TotalAmount   = cost,
                            PaidAmount    = statusLabel == "Paid" ? cost : 0m,
                            Balance       = statusLabel == "Paid" ? 0m : cost,
                            Status        = statusLabel,
                            Source        = $"Transport | {tr.pickup_location} → {tr.destination} | Driver: {tr.driver_name ?? "-"} | {(tr.actual_kilogram != null ? tr.actual_kilogram + " KG" : "—")}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MonthlyReceipt] Transport rows load error: {ex.Message}");
                }

                Receipts = manualReceipts.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonthlyReceipt] Load error: {ex.Message}");
                Receipts = new List<MonthlyReceipt>();
            }
        }
    }
}
