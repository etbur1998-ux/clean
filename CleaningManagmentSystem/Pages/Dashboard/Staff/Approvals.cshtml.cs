using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class ApprovalsModel : PageModel
    {
        private readonly string _connectionString;

        public ApprovalsModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public List<dynamic> PendingSubmissions  { get; set; } = new();
        public List<dynamic> HistorySubmissions  { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; } = "";

        public decimal DefaultPricePerKg { get; set; } = 1.4m;

        public int     TotalPending  { get; set; }
        public int     TotalApproved { get; set; }
        public int     TotalRejected { get; set; }
        public decimal TotalKgToday  { get; set; }

        // ── GET ───────────────────────────────────────────────────
        public async Task OnGetAsync()
        {
            using var conn = new MySqlConnection(_connectionString);

            try
            {
                var val = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");
                if (val != null && decimal.TryParse(val, out var p)) DefaultPricePerKg = p;
            }
            catch { }

            var searchWhere = string.IsNullOrEmpty(SearchQuery) ? "" :
                " AND (weredaName LIKE @Search OR driverName LIKE @Search " +
                "OR vehicleName LIKE @Search OR mahberatName LIKE @Search)";
            var param = new { Search = "%" + SearchQuery + "%" };

            var pendingQ = $@"
                SELECT * FROM (
                    SELECT id, 'Mahberat' AS receiptType,
                        wereda_name AS weredaName, mahberat_name AS mahberatName,
                        driver_name AS driverName, plate_number  AS vehicleName,
                        kilogram, price,
                        ROUND(kilogram * price, 2) AS total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') AS date,
                        TIME_FORMAT(receipt_time,'%H:%i')    AS time,
                        status, notes, image_url AS imageUrl, registered_at,
                        mahberat_approved, mahberat_approved_by,
                        mahberat_approved_at, mahberat_notes,
                        IFNULL(transport_request_id, 0) AS transport_request_id
                    FROM staff_receipts
                    WHERE status = 'Pending' AND mahberat_approved = 1
                    UNION ALL
                    SELECT id, 'Outsource' AS receiptType,
                        wereda_name AS weredaName, company_name AS mahberatName,
                        driver_name AS driverName, plate_number AS vehicleName,
                        kilogram, price,
                        ROUND(kilogram * price, 2) AS total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') AS date,
                        TIME_FORMAT(receipt_time,'%H:%i')    AS time,
                        status, notes, image_url AS imageUrl, registered_at,
                        mahberat_approved, mahberat_approved_by,
                        mahberat_approved_at, mahberat_notes,
                        NULL AS transport_request_id
                    FROM outsource_receipts
                    WHERE status = 'Pending' AND mahberat_approved = 1
                ) AS combined
                WHERE 1=1 {searchWhere}
                ORDER BY registered_at DESC";

            PendingSubmissions = (await conn.QueryAsync(pendingQ, param)).ToList();

            var histQ = $@"
                SELECT * FROM (
                    SELECT id, 'Mahberat' AS receiptType,
                        wereda_name AS weredaName, mahberat_name AS mahberatName,
                        driver_name AS driverName, plate_number  AS vehicleName,
                        kilogram, price,
                        ROUND(kilogram * price, 2) AS total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') AS date,
                        TIME_FORMAT(receipt_time,'%H:%i')    AS time,
                        status, notes, image_url AS imageUrl, registered_at,
                        mahberat_approved, mahberat_notes,
                        IFNULL(transport_request_id, 0) AS transport_request_id
                    FROM staff_receipts
                    WHERE status != 'Pending'
                    UNION ALL
                    SELECT id, 'Outsource' AS receiptType,
                        wereda_name AS weredaName, company_name AS mahberatName,
                        driver_name AS driverName, plate_number AS vehicleName,
                        kilogram, price,
                        ROUND(kilogram * price, 2) AS total,
                        DATE_FORMAT(receipt_date,'%Y-%m-%d') AS date,
                        TIME_FORMAT(receipt_time,'%H:%i')    AS time,
                        status, notes, image_url AS imageUrl, registered_at,
                        mahberat_approved, mahberat_notes,
                        NULL AS transport_request_id
                    FROM outsource_receipts
                    WHERE status != 'Pending'
                ) AS combined
                WHERE 1=1 {searchWhere}
                ORDER BY registered_at DESC LIMIT 200";

            HistorySubmissions = (await conn.QueryAsync(histQ, param)).ToList();

            TotalPending  = PendingSubmissions.Count;
            TotalApproved = HistorySubmissions.Count(x =>
                (string?)x.status is "Approved" or "Billed" or "Paid");
            TotalRejected = HistorySubmissions.Count(x =>
                (string?)x.status is "Rejected" or "MahberatRejected");
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            TotalKgToday = HistorySubmissions
                .Where(x => (string?)x.date == today)
                .Sum(x => (decimal)(x.kilogram ?? 0));
        }

        // ── APPROVE → redirect to RegisterReceipt for final billing ──
        public async Task<IActionResult> OnPostApproveAsync(
            int id, string receiptType, decimal pricePerKg, decimal totalPrice)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Staff";
            using var conn = new MySqlConnection(_connectionString);

            // Load default rate
            decimal defaultRate = 1.4m;
            try
            {
                var val = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");
                if (val != null && decimal.TryParse(val, out var p)) defaultRate = p;
            }
            catch { }

            decimal effectiveRate = pricePerKg > 0 ? pricePerKg
                                  : totalPrice > 0  ? totalPrice
                                  : defaultRate;

            var table = receiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";

            // Mark receipt Approved
            try
            {
                await conn.ExecuteAsync(
                    $"UPDATE {table} SET status='Approved', price=@Rate, approved_by=@By, approved_at=NOW() WHERE id=@Id",
                    new { Rate = effectiveRate, By = userName, Id = id });
            }
            catch
            {
                await conn.ExecuteAsync(
                    $"UPDATE {table} SET status='Approved', price=@Rate WHERE id=@Id",
                    new { Rate = effectiveRate, Id = id });
            }

            // Advance transport_request to ReceiptVerified
            if (receiptType == "Mahberat")
            {
                try
                {
                    var trId = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT transport_request_id FROM staff_receipts WHERE id=@Id",
                        new { Id = id });

                    if (trId.HasValue && trId.Value > 0)
                    {
                        var staffId = HttpContext.Session.GetInt32("UserId") ?? 0;
                        await conn.ExecuteAsync(
                            @"UPDATE transport_requests
                              SET status='ReceiptVerified',
                                  mahberat_verified_at=COALESCE(mahberat_verified_at,NOW()),
                                  staff_id=@StaffId, staff_name=@StaffName, staff_action_at=NOW()
                              WHERE id=@Id
                                AND status IN ('ReceiptSubmitted','MahberatVerified','ReceiptVerified')",
                            new { StaffId = staffId, StaffName = userName, Id = trId.Value });
                    }
                }
                catch { }
            }

            // Redirect to RegisterReceipt pre-filled for final billing
            return RedirectToPage("/Dashboard/Staff/RegisterReceipt",
                new { fromApproval = "true", approvedReceiptId = id, approvedType = receiptType });
        }

        // ── REJECT ──────────────────────────────────────────────────
        public async Task<IActionResult> OnPostRejectAsync(int id, string receiptType, string? notes)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Staff";
            using var conn = new MySqlConnection(_connectionString);

            var table = receiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";
            try
            {
                await conn.ExecuteAsync(
                    $"UPDATE {table} SET status='Rejected', rejected_by=@By, rejected_at=NOW(), reject_notes=@Notes WHERE id=@Id",
                    new { By = userName, Notes = notes ?? "", Id = id });
            }
            catch
            {
                await conn.ExecuteAsync(
                    $"UPDATE {table} SET status='Rejected' WHERE id=@Id",
                    new { Id = id });
            }

            if (receiptType == "Mahberat")
            {
                try
                {
                    var trId = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT transport_request_id FROM staff_receipts WHERE id=@Id",
                        new { Id = id });

                    if (trId.HasValue && trId.Value > 0)
                    {
                        var driverId = await conn.QueryFirstOrDefaultAsync<int?>(
                            "SELECT driver_id FROM transport_requests WHERE id=@Id",
                            new { Id = trId.Value });

                        await conn.ExecuteAsync(
                            "UPDATE transport_requests SET status='ReceiptSubmitted' WHERE id=@Id AND status='ReceiptSubmitted'",
                            new { Id = trId.Value });

                        if (driverId.HasValue)
                            await conn.ExecuteAsync(
                                @"INSERT INTO transport_notifications
                                  (recipient_user_id, transport_request_id, request_number, title, body, notification_type)
                                  VALUES (@Uid,@TrId,
                                    (SELECT request_number FROM transport_requests WHERE id=@TrId),
                                    'Receipt Rejected by Staff', @Body, 'Warning')",
                                new {
                                    Uid  = driverId.Value,
                                    TrId = trId.Value,
                                    Body = $"Staff rejected your receipt. Reason: {notes ?? "No reason given"}. Please resubmit."
                                });
                    }
                }
                catch { }
            }

            return RedirectToPage();
        }
    }
}
