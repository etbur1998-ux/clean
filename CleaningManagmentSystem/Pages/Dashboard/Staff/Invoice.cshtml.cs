using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class InvoiceModel : PageModel
    {
        private readonly string _connectionString;

        public List<dynamic> Receipts { get; set; } = new();
        public string ReceiptType { get; set; } = "";
        public decimal TotalKilogram { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPreview { get; set; }
        public string Ids { get; set; } = "";

        public InvoiceModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<IActionResult> OnGetAsync(string ids, string type)
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (string.IsNullOrEmpty(ids) || string.IsNullOrEmpty(type))
            {
                return RedirectToPage("/Dashboard/Staff/Index");
            }

            ReceiptType = type;
            var table = type == "Outsource" ? "outsource_receipts" : "staff_receipts";
            var entityNameCol = type == "Outsource" ? "company_name" : "mahberat_name";

            var idArray = ids.Split(',').Select(id => int.TryParse(id, out var parsed) ? parsed : 0).Where(id => id > 0).ToList();
            
            if (!idArray.Any())
            {
                return RedirectToPage("/Dashboard/Staff/Index");
            }

            var query = $@"
                SELECT id, wereda_name, {entityNameCol} as entity_name, plate_number, driver_name,
                       receipt_date, receipt_time, kilogram, price,
                       (kilogram * price) AS total_price, status
                FROM {table}
                WHERE id IN ({string.Join(",", idArray)})";

            using var connection = new MySqlConnection(_connectionString);
            Receipts = (await connection.QueryAsync(query)).ToList();

            if (Receipts.Any())
            {
                TotalKilogram = Receipts.Sum(r => (decimal)r.kilogram);
                TotalAmount = Receipts.Sum(r => (decimal)r.total_price);
                IsPreview = Receipts.Any(r => r.status == "Approved" || r.status == "Registered");
                Ids = ids;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmBillAsync(string ids, string type)
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName) || userRole?.ToLower() != "staff")
                return RedirectToPage("/Login");

            if (string.IsNullOrEmpty(ids) || string.IsNullOrEmpty(type))
            {
                return RedirectToPage("/Dashboard/Staff/Index");
            }

            var table = type == "Outsource" ? "outsource_receipts" : "staff_receipts";
            var idArray = ids.Split(',').Select(id => int.TryParse(id, out var parsed) ? parsed : 0).Where(id => id > 0).ToList();

            if (idArray.Any())
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.ExecuteAsync($"UPDATE {table} SET status = 'Billed' WHERE id IN ({string.Join(",", idArray)})");
            }

            // Redirect back to the invoice page to show the final printed version
            return RedirectToPage("/Dashboard/Staff/Invoice", new { ids = ids, type = type });
        }
    }
}
