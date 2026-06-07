using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.WeredaMahberat
{
    public class ReceiptApprovalsModel : PageModel
    {
        private readonly string _connectionString;

        public ReceiptApprovalsModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        // Receipts waiting for Mahberat Level-1 approval (mahberat_approved IS NULL)
        public List<dynamic> PendingReceipts { get; set; } = new();
        // Receipts already actioned by Mahberat
        public List<dynamic> ActionedReceipts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; } = "";

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            LoadData(userName);
            return Page();
        }

        private void LoadData(string userName)
        {
            using var connection = new MySqlConnection(_connectionString);

            var searchFilter = string.IsNullOrEmpty(SearchQuery)
                ? ""
                : " AND (weredaName LIKE @Search OR driverName LIKE @Search OR vehicleName LIKE @Search)";
            var param = new { Search = "%" + SearchQuery + "%" };

            // Pending Level-1: mahberat_approved IS NULL and status = Pending
            var pendingQ = $@"
                SELECT * FROM (
                    SELECT id, 'Mahberat' as receiptType,
                        wereda_name as weredaName, mahberat_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time,'%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at,
                        mahberat_approved, mahberat_approved_by, mahberat_notes
                    FROM staff_receipts
                    WHERE status = 'Pending' AND (mahberat_approved IS NULL)

                    UNION ALL

                    SELECT id, 'Outsource' as receiptType,
                        wereda_name as weredaName, company_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time,'%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at,
                        mahberat_approved, mahberat_approved_by, mahberat_notes
                    FROM outsource_receipts
                    WHERE status = 'Pending' AND (mahberat_approved IS NULL)
                ) as combined
                WHERE 1=1 {searchFilter}
                ORDER BY registered_at DESC";

            PendingReceipts = (connection.Query(pendingQ, param)).ToList();

            // Already actioned by Mahberat
            var actionedQ = $@"
                SELECT * FROM (
                    SELECT id, 'Mahberat' as receiptType,
                        wereda_name as weredaName, mahberat_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time,'%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at,
                        mahberat_approved, mahberat_approved_by, mahberat_notes
                    FROM staff_receipts
                    WHERE mahberat_approved IS NOT NULL

                    UNION ALL

                    SELECT id, 'Outsource' as receiptType,
                        wereda_name as weredaName, company_name as mahberatName,
                        driver_name as driverName, plate_number as vehicleName,
                        kilogram, price as total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') as date,
                        TIME_FORMAT(receipt_time,'%H:%i') as time,
                        status, notes, image_url as imageUrl, registered_at,
                        mahberat_approved, mahberat_approved_by, mahberat_notes
                    FROM outsource_receipts
                    WHERE mahberat_approved IS NOT NULL
                ) as combined
                WHERE 1=1 {searchFilter}
                ORDER BY registered_at DESC LIMIT 100";

            ActionedReceipts = (connection.Query(actionedQ, param)).ToList();
        }

        // Mahberat approves — sets mahberat_approved = 1, receipt stays Pending for Staff
        public IActionResult OnPostApprove(int id, string receiptType, string notes)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Login");

            using var connection = new MySqlConnection(_connectionString);
            var table = receiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";
            connection.Execute(
                $@"UPDATE {table} 
                   SET mahberat_approved = 1, 
                       mahberat_approved_by = @By, 
                       mahberat_approved_at = NOW(),
                       mahberat_notes = @Notes
                   WHERE id = @Id",
                new { By = userName, Notes = notes ?? "", Id = id });

            LoadData(userName);
            return RedirectToPage();
        }

        // Mahberat rejects — sets mahberat_approved = 0 AND status = MahberatRejected
        public IActionResult OnPostReject(int id, string receiptType, string notes)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Login");

            using var connection = new MySqlConnection(_connectionString);
            var table = receiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";
            connection.Execute(
                $@"UPDATE {table} 
                   SET mahberat_approved = 0, 
                       mahberat_approved_by = @By, 
                       mahberat_approved_at = NOW(),
                       mahberat_notes = @Notes,
                       status = 'MahberatRejected'
                   WHERE id = @Id",
                new { By = userName, Notes = notes ?? "", Id = id });

            LoadData(userName);
            return RedirectToPage();
        }
    }
}
