using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using System.ComponentModel.DataAnnotations;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    [IgnoreAntiforgeryToken]
    public class PrivateCompanyReceiptModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty] public int     CompanyId  { get; set; }
        [BindProperty] public int     WeredaId   { get; set; }
        [BindProperty] public int     VehicleId  { get; set; }
        [BindProperty] public int     DriverId   { get; set; }
        [BindProperty][Required] public DateTime Date { get; set; }
        [BindProperty][Required] public string   TimeString { get; set; } = "";
        [BindProperty][Required][Range(0.01,double.MaxValue)] public decimal Kilogram { get; set; }
        [BindProperty][Required][Range(0.01,double.MaxValue)] public decimal Price    { get; set; }
        [BindProperty] public string?       Notes     { get; set; }
        [BindProperty] public IFormFile?    ImageFile { get; set; }

        // Filters
        [BindProperty(SupportsGet = true)] public int?      FilterCompanyId  { get; set; }
        [BindProperty(SupportsGet = true)] public int?      FilterWeredaId   { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterStartDate  { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterEndDate    { get; set; }
        [BindProperty(SupportsGet = true)] public string?   FilterStatus     { get; set; }

        public decimal        DefaultPricePerKg { get; set; } = 1.4m;
        public List<dynamic>  Companies      { get; set; } = new();
        public List<dynamic>  Weredas        { get; set; } = new();
        public List<dynamic>  Vehicles       { get; set; } = new();
        public List<dynamic>  Drivers        { get; set; } = new();
        public List<dynamic>  RecentReceipts { get; set; } = new();
        public string         Message        { get; set; } = "";
        public bool           IsSuccess      { get; set; }

        public PrivateCompanyReceiptModel(IConfiguration cfg)
            => _connectionString = cfg.GetConnectionString("DefaultConnection") ?? "";

        // ── GET ──────────────────────────────────────────────────────────────
        public IActionResult OnGet()
        {
            if (!IsAuthorized()) return RedirectToPage("/Login");
            LoadData(); LoadDefaults();
            return Page();
        }

        // ── POST: Register new receipt ────────────────────────────────────────
        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAuthorized()) return RedirectToPage("/Login");
            if (!ModelState.IsValid) { LoadData(); LoadDefaults(); return Page(); }

            try
            {
                TimeSpan.TryParse(TimeString, out var receiptTime);
                using var conn = new MySqlConnection(_connectionString);

                // Upload image if provided
                string? imageUrl = null;
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await ImageFile.CopyToAsync(stream);
                    imageUrl = $"/uploads/{fileName}";
                }

                var companyName = conn.QueryFirstOrDefault<string>(
                    "SELECT company_name FROM private_cleaning_companies WHERE id=@Id", new { Id = CompanyId }) ?? "";
                var weredaName  = conn.QueryFirstOrDefault<string>(
                    "SELECT name FROM weredas WHERE id=@Id", new { Id = WeredaId }) ?? "";
                var plateNumber = conn.QueryFirstOrDefault<string>(
                    "SELECT plate_number FROM vehicles WHERE id=@Id", new { Id = VehicleId }) ?? "";
                var driverName  = conn.QueryFirstOrDefault<string>(
                    "SELECT name FROM users WHERE id=@Id", new { Id = DriverId }) ?? "";

                conn.Execute(@"
                    INSERT INTO private_company_receipts
                      (company_id, company_name, wereda_id, wereda_name,
                       vehicle_id, plate_number, driver_id, driver_name,
                       receipt_time, receipt_date, kilogram, price,
                       total_amount, notes, image_url, registered_by, status, registered_at)
                    VALUES
                      (@cid, @cn, @wid, @wn, @vid, @pl, @did, @dn,
                       @rt, @dt, @kg, @pr, @tot, @notes, @img, @regBy, 'Registered', NOW())",
                    new {
                        cid = CompanyId,  cn  = companyName,
                        wid = WeredaId,   wn  = weredaName,
                        vid = VehicleId,  pl  = plateNumber,
                        did = DriverId,   dn  = driverName,
                        rt  = receiptTime, dt = Date,
                        kg  = Kilogram,   pr  = Price,
                        tot = Kilogram * Price,
                        notes = Notes ?? "",
                        img   = imageUrl,
                        regBy = HttpContext.Session.GetString("UserName")
                    });

                Message = "Receipt registered successfully!";
                IsSuccess = true;
                CompanyId = WeredaId = VehicleId = DriverId = 0;
                TimeString = ""; Kilogram = 0; Price = 0; Notes = "";
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            LoadData(); LoadDefaults();
            return Page();
        }

        // ── POST: Mark as Paid ────────────────────────────────────────────────
        public IActionResult OnPostMarkPaid(int id)
        {
            if (!IsAuthorized()) return RedirectToPage("/Login");
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Execute(
                    "UPDATE private_company_receipts SET status='Paid', updated_at=NOW() WHERE id=@Id",
                    new { Id = id });
                Message = "Receipt marked as Paid."; IsSuccess = true;
            }
            catch (Exception ex) { Message = $"Error: {ex.Message}"; IsSuccess = false; }
            LoadData(); LoadDefaults(); return Page();
        }

        // ── POST: Delete (only allowed for non-Paid) ──────────────────────────
        public IActionResult OnPostDelete(int id)
        {
            if (!IsAuthorized()) return RedirectToPage("/Login");
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                // Prevent deleting Paid receipts
                var status = conn.QueryFirstOrDefault<string>(
                    "SELECT status FROM private_company_receipts WHERE id=@Id", new { Id = id });
                if (status?.ToLower() == "paid")
                {
                    Message = "Cannot delete a Paid receipt."; IsSuccess = false;
                }
                else
                {
                    conn.Execute("DELETE FROM private_company_receipts WHERE id=@Id", new { Id = id });
                    Message = "Receipt deleted."; IsSuccess = true;
                }
            }
            catch (Exception ex) { Message = $"Error: {ex.Message}"; IsSuccess = false; }
            LoadData(); LoadDefaults(); return Page();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private bool IsAuthorized()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
            return !string.IsNullOrEmpty(userName) &&
                   (userRole == "staff" || userRole == "manager" || userRole == "superadmin");
        }

        private void LoadDefaults()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                var val = conn.QueryFirstOrDefault<string>(
                    "SELECT setting_value FROM system_settings WHERE setting_key='DefaultPricePerKg'");
                if (val != null && decimal.TryParse(val, out var p)) DefaultPricePerKg = p;
                if (Price == 0) Price = DefaultPricePerKg;
            }
            catch { }
        }

        private void LoadData()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                Companies = conn.Query<dynamic>(
                    "SELECT id, company_name FROM private_cleaning_companies WHERE is_active=1 ORDER BY company_name").ToList();
                Weredas = conn.Query<dynamic>(
                    "SELECT id, name FROM weredas WHERE is_active=1 ORDER BY name").ToList();
                Vehicles = conn.Query<dynamic>(
                    "SELECT id, plate_number, model FROM vehicles ORDER BY plate_number").ToList();
                Drivers = conn.Query<dynamic>(
                    "SELECT id, name FROM users WHERE role='driver' AND is_active=1 ORDER BY name").ToList();

                var q = "SELECT * FROM private_company_receipts WHERE 1=1";
                var p = new DynamicParameters();
                if (FilterCompanyId > 0)       { q += " AND company_id=@CId"; p.Add("CId", FilterCompanyId); }
                if (FilterWeredaId  > 0)        { q += " AND wereda_id=@WId";  p.Add("WId", FilterWeredaId); }
                if (!string.IsNullOrEmpty(FilterStatus)) { q += " AND status=@St"; p.Add("St", FilterStatus); }
                if (FilterStartDate.HasValue)   { q += " AND receipt_date>=@S"; p.Add("S", FilterStartDate.Value); }
                if (FilterEndDate.HasValue)     { q += " AND receipt_date<=@E"; p.Add("E", FilterEndDate.Value); }
                q += " ORDER BY registered_at DESC LIMIT 100";

                RecentReceipts = conn.Query<dynamic>(q, p).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivateCompanyReceipt] LoadData error: {ex.Message}");
            }
        }
    }
}
