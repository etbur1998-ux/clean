using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    public class ApprovalsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ApprovalsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public List<dynamic> PendingSubmissions { get; set; } = new();
        public List<dynamic> HistorySubmissions { get; set; } = new();
        public List<dynamic> MonthlyReceipts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string HistorySearch { get; set; } = "";

        public decimal DefaultPricePerKg { get; set; } = 1.4m;

        public async Task OnGetAsync()
        {
            using var connection = new MySqlConnection(_connectionString);

            // Load default price
            try
            {
                var value = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");
                if (value != null && decimal.TryParse(value, out var price))
                    DefaultPricePerKg = price;
            }
            catch { }

            // ── Pending ──
            var pendingQuery = @"
                SELECT * FROM (
                    SELECT id, 'Mahberat' as receiptType,
                        wereda_name as weredaName, mahberat_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time, '%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at
                    FROM staff_receipts

                    UNION ALL

                    SELECT id, 'Outsource' as receiptType,
                        wereda_name as weredaName, company_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time, '%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at
                    FROM outsource_receipts
                ) as combined
                WHERE status = 'Pending'";

            if (!string.IsNullOrEmpty(SearchQuery))
                pendingQuery += " AND (weredaName LIKE @Search OR driverName LIKE @Search OR vehicleName LIKE @Search OR mahberatName LIKE @Search)";
            pendingQuery += " ORDER BY registered_at DESC";

            PendingSubmissions = (await connection.QueryAsync(pendingQuery,
                new { Search = "%" + SearchQuery + "%" })).ToList();

            // ── History (Approved / Rejected) ──
            var historyQuery = @"
                SELECT * FROM (
                    SELECT id, 'Mahberat' as receiptType,
                        wereda_name as weredaName, mahberat_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time, '%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at
                    FROM staff_receipts

                    UNION ALL

                    SELECT id, 'Outsource' as receiptType,
                        wereda_name as weredaName, company_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time, '%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at
                    FROM outsource_receipts
                ) as combined
                WHERE status != 'Pending'";

            if (!string.IsNullOrEmpty(HistorySearch))
                historyQuery += " AND (weredaName LIKE @HSearch OR driverName LIKE @HSearch OR vehicleName LIKE @HSearch OR mahberatName LIKE @HSearch)";
            historyQuery += " ORDER BY registered_at DESC LIMIT 200";

            HistorySubmissions = (await connection.QueryAsync(historyQuery,
                new { HSearch = "%" + HistorySearch + "%" })).ToList();

            // ── Monthly Receipts ──
            var monthlyQuery = @"
                SELECT id, receipt_number as receiptNumber, month, year, total_amount as totalAmount,
                       paid_amount as paidAmount, balance, status, source, created_at as createdAt
                FROM monthly_receipts
                WHERE status IN ('Level 1 Approved', 'Paid')
                ORDER BY created_at DESC LIMIT 100";
            MonthlyReceipts = (await connection.QueryAsync(monthlyQuery)).ToList();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id, string receiptType, decimal totalPrice)
        {
            using var connection = new MySqlConnection(_connectionString);
            var table = receiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";

            if (totalPrice > 0)
                await connection.ExecuteAsync(
                    $"UPDATE {table} SET status = 'Approved', price = @TotalPrice WHERE id = @Id",
                    new { Id = id, TotalPrice = totalPrice });
            else
                await connection.ExecuteAsync(
                    $"UPDATE {table} SET status = 'Approved' WHERE id = @Id",
                    new { Id = id });

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, string receiptType)
        {
            using var connection = new MySqlConnection(_connectionString);
            var table = receiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";
            await connection.ExecuteAsync($"UPDATE {table} SET status = 'Rejected' WHERE id = @Id", new { Id = id });
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveMonthlyAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.ExecuteAsync("UPDATE monthly_receipts SET status = 'Paid', updated_at = NOW() WHERE id = @Id", new { Id = id });
            return RedirectToPage(new { tab = "monthly" });
        }
    }
}
