using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace CleaningManagmentSystem.Controllers
{
    [Route("api/mobile")]
    [ApiController]
    public class MobileApiController : ControllerBase
    {
        private readonly string _connectionString;

        public MobileApiController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            using var connection = CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<UserResponse>(
                @"SELECT id, name, role, phone 
                  FROM users 
                  WHERE email = @Email AND password = @Password AND is_active = TRUE",
                new { Email = request.Username, Password = request.Password });

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials or inactive account" });

            // Normalize role to the exact casing the Flutter app expects
            user.Role = NormalizeRole(user.Role);

            // If the user is a driver, fetch their assigned vehicle
            if (user.Role.ToLower() == "driver")
            {
                var vehicle = await connection.QueryFirstOrDefaultAsync(
                    "SELECT id, plate_number FROM vehicles WHERE driver_id = @Id", new { Id = user.Id });
                if (vehicle != null)
                {
                    user.VehicleId   = vehicle.id;
                    user.VehicleName = vehicle.plate_number;
                }
            }

            return Ok(user);
        }

        /// <summary>
        /// Maps any DB role string to the canonical value the Flutter app expects.
        /// Flutter checks role.toLowerCase() for: driver, outsource, privatecompanyrep, manager
        /// </summary>
        private static string NormalizeRole(string raw)
        {
            return (raw ?? "").Trim().ToLower() switch
            {
                "driver"                            => "driver",
                "outsource"                         => "outsource",
                "privatecompanyrep"                 => "PrivateCompanyRep",
                "private_company_rep"               => "PrivateCompanyRep",
                "private company rep"               => "PrivateCompanyRep",
                "manager"                           => "manager",
                "superadmin"                        => "superadmin",
                "super admin"                       => "superadmin",
                "super_admin"                       => "superadmin",
                "staff"                             => "staff",
                "wereda_mahberat"                   => "WeredaMahberat",
                "weredamahberat"                    => "WeredaMahberat",
                "wereda mahberat"                   => "WeredaMahberat",
                "dispatchofficer"                   => "DispatchOfficer",
                "dispatch_officer"                  => "DispatchOfficer",
                "dispatch officer"                  => "DispatchOfficer",
                _                                   => raw  // keep original for unknown roles
            };
        }

        [HttpGet("weredas")]
        public async Task<IActionResult> GetWeredas()
        {
            using var connection = CreateConnection();
            var weredas = await connection.QueryAsync(
                "SELECT id, name FROM weredas WHERE is_active = TRUE ORDER BY name ASC");
            return Ok(weredas);
        }

        [HttpGet("mahberats")]
        public async Task<IActionResult> GetMahberats()
        {
            using var connection = CreateConnection();
            var mahberats = await connection.QueryAsync(
                "SELECT id, name FROM mahberats WHERE is_active = TRUE ORDER BY name ASC");
            return Ok(mahberats);
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            using var connection = CreateConnection();
            var companies = await connection.QueryAsync(
                "SELECT id, company_name as name FROM outsource_companies WHERE status = 'Active' ORDER BY company_name ASC");
            return Ok(companies);
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            using var connection = CreateConnection();
            var vehicles = await connection.QueryAsync(
                "SELECT id, CONCAT(model, ' - ', plate_number) as name FROM vehicles WHERE status = 'Available' ORDER BY plate_number ASC");
            return Ok(vehicles);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReceipt([FromBody] ReceiptSubmission request)
        {
            using var connection = CreateConnection();
            
            var isOutsource = request.ReceiptType == "Outsource";
            
            var weredaName = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT name FROM weredas WHERE id = @Id", new { Id = request.WeredaId });
                
            string entityName = null;
            if (isOutsource) {
                entityName = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT company_name FROM outsource_companies WHERE id = @Id", new { Id = request.MahberatId });
            } else {
                entityName = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM mahberats WHERE id = @Id", new { Id = request.MahberatId });
            }
                
            var vehicleName = request.VehicleId.HasValue 
                ? await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT plate_number FROM vehicles WHERE id = @Id", new { Id = request.VehicleId }) 
                : null;
            var driverName = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT name FROM users WHERE id = @Id", new { Id = request.UserId });

            if (isOutsource)
            {
                await connection.ExecuteAsync(
                      @"INSERT INTO outsource_receipts 
                      (wereda_id, wereda_name, company_id, company_name, vehicle_id, plate_number, 
                       driver_id, driver_name, receipt_time, receipt_date, kilogram, price, registered_by, status, notes, image_url)
                      VALUES 
                      (@WeredaId, @WeredaName, @CompanyId, @CompanyName, @VehicleId, @PlateNumber,
                       @DriverId, @DriverName, @Time, @Date, @Kilogram, @Price, 'MobileApp', 'Pending', @Notes, @ImageUrl)",
                    new 
                    { 
                        WeredaId = request.WeredaId, WeredaName = weredaName,
                        CompanyId = request.MahberatId, CompanyName = entityName,
                        VehicleId = request.VehicleId, PlateNumber = vehicleName,
                        DriverId = request.UserId, DriverName = driverName,
                        Time = request.Time, Date = request.Date,
                        Kilogram = request.Kilogram, Price = request.Total,
                        Notes = request.Notes, ImageUrl = request.ImageUrl
                    });
            }
            else
            {
                await connection.ExecuteAsync(
                      @"INSERT INTO staff_receipts 
                      (wereda_id, wereda_name, mahberat_id, mahberat_name, vehicle_id, plate_number, 
                       driver_id, driver_name, receipt_time, receipt_date, kilogram, price, registered_by, status, notes, image_url, latitude, longitude)
                      VALUES 
                      (@WeredaId, @WeredaName, @MahberatId, @MahberatName, @VehicleId, @PlateNumber,
                       @DriverId, @DriverName, @Time, @Date, @Kilogram, @Price, 'MobileApp', 'Pending', @Notes, @ImageUrl, @Latitude, @Longitude)",
                    new 
                    { 
                        WeredaId = request.WeredaId, WeredaName = weredaName,
                        MahberatId = request.MahberatId, MahberatName = entityName,
                        VehicleId = request.VehicleId, PlateNumber = vehicleName,
                        DriverId = request.UserId, DriverName = driverName,
                        Time = request.Time, Date = request.Date,
                        Kilogram = request.Kilogram, Price = request.Total,
                        Notes = request.Notes, ImageUrl = request.ImageUrl,
                        Latitude = request.Latitude, Longitude = request.Longitude
                    });
            }

            return Ok(new { success = true });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetHistory(int userId)
        {
            using var connection = CreateConnection();
            var history = await connection.QueryAsync(
                @"SELECT * FROM (
                    SELECT 
                        id, 
                        wereda_name as weredaName, 
                        mahberat_name as mahberatName, 
                        kilogram, 
                        price as total, 
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date, 
                        TIME_FORMAT(receipt_time, '%H:%i') as time, 
                        status,
                        notes,
                        image_url as imageUrl,
                        registered_at
                    FROM staff_receipts 
                    WHERE driver_id = @UserId 
                    
                    UNION ALL
                    
                    SELECT 
                        id, 
                        wereda_name as weredaName, 
                        company_name as mahberatName, 
                        kilogram, 
                        price as total, 
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date, 
                        TIME_FORMAT(receipt_time, '%H:%i') as time, 
                        status,
                        notes,
                        image_url as imageUrl,
                        registered_at
                    FROM outsource_receipts 
                    WHERE driver_id = @UserId 
                ) as combined
                ORDER BY registered_at DESC",
                new { UserId = userId });
            return Ok(history);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingSubmissions()
        {
            using var connection = CreateConnection();
            var pending = await connection.QueryAsync(
                @"SELECT * FROM (
                    SELECT 
                        id, 
                        'Mahberat' as receiptType,
                        wereda_name as weredaName, 
                        mahberat_name as mahberatName, 
                        driver_name as driverName,
                        plate_number as vehicleName,
                        kilogram, 
                        price as total, 
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date, 
                        TIME_FORMAT(receipt_time, '%H:%i') as time, 
                        status,
                        notes,
                        image_url as imageUrl,
                        registered_at
                    FROM staff_receipts 
                    WHERE status = 'Pending' 
                    
                    UNION ALL
                    
                    SELECT 
                        id, 
                        'Outsource' as receiptType,
                        wereda_name as weredaName, 
                        company_name as mahberatName, 
                        driver_name as driverName,
                        plate_number as vehicleName,
                        kilogram, 
                        price as total, 
                        DATE_FORMAT(receipt_date, '%Y-%m-%d') as date, 
                        TIME_FORMAT(receipt_time, '%H:%i') as time, 
                        status,
                        notes,
                        image_url as imageUrl,
                        registered_at
                    FROM outsource_receipts 
                    WHERE status = 'Pending' 
                ) as combined
                ORDER BY registered_at DESC");
            return Ok(pending);
        }

        [HttpPost("submissions/{id}/status")]
        public async Task<IActionResult> UpdateSubmissionStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            using var connection = CreateConnection();
            var table = request.ReceiptType == "Outsource" ? "outsource_receipts" : "staff_receipts";
            var result = await connection.ExecuteAsync(
                $"UPDATE {table} SET status = @Status WHERE id = @Id",
                new { Status = request.Status, Id = id });

            if (result > 0)
                return Ok(new { success = true });
            return BadRequest(new { message = "Update failed or record not found" });
        }

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/{uniqueFileName}";
            return Ok(new { url = imageUrl });
        }

        [HttpGet("notifications/{userId}")]
        public async Task<IActionResult> GetNotifications(int userId)
        {
            // Returns transport notifications for the user
            using var connection = CreateConnection();
            try
            {
                var notifs = await connection.QueryAsync(
                    @"SELECT id, transport_request_id, request_number, title, body, notification_type, is_read, created_at
                      FROM transport_notifications WHERE recipient_user_id = @UserId
                      ORDER BY created_at DESC LIMIT 50",
                    new { UserId = userId });
                return Ok(notifs);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            using var connection = CreateConnection();
            try
            {
                await connection.ExecuteAsync(
                    "UPDATE transport_notifications SET is_read = 1 WHERE id = @Id", new { Id = id });
            }
            catch { }
            return Ok(new { success = true });
        }

        [HttpGet("reports/{userId}")]
        public async Task<IActionResult> GetReportData(int userId, [FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            using var connection = CreateConnection();
            var start = startDate ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
            var end   = endDate   ?? DateTime.Now.ToString("yyyy-MM-dd");

            // Combine staff_receipts + outsource_receipts for all mobile roles
            var history = await connection.QueryAsync(
                @"SELECT kilogram, price as total, receipt_date as date, status
                  FROM staff_receipts
                  WHERE driver_id = @UserId AND receipt_date BETWEEN @Start AND @End
                  UNION ALL
                  SELECT kilogram, price as total, receipt_date as date, status
                  FROM outsource_receipts
                  WHERE driver_id = @UserId AND receipt_date BETWEEN @Start AND @End
                  UNION ALL
                  SELECT kilogram, price as total, receipt_date as date, status
                  FROM private_company_receipts
                  WHERE driver_id = @UserId AND receipt_date BETWEEN @Start AND @End
                  ORDER BY date ASC",
                new { UserId = userId, Start = start, End = end });

            return Ok(new { data = history, startDate = start, endDate = end });
        }

        // ── Private Company Rep: get their company info ──────────────
        [HttpGet("private-company/{userId}")]
        public async Task<IActionResult> GetPrivateCompanyInfo(int userId)
        {
            using var connection = CreateConnection();

            var userInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, name, email, role FROM users WHERE id = @Id",
                new { Id = userId });

            if (userInfo == null)
                return NotFound(new { message = "User not found." });

            Console.WriteLine($"[PrivateCompany] Looking up company for user {userId} ({userInfo.email})");

            dynamic? company = null;

            // 1. Match by rep_user_id
            company = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT p.id, p.company_name, p.phone, p.address,
                         COALESCE(p.services_offered, '') AS services_provided
                  FROM private_cleaning_companies p
                  WHERE p.rep_user_id = @Uid AND COALESCE(p.is_active, 1) = 1
                  LIMIT 1",
                new { Uid = userId });

            if (company != null)
            {
                Console.WriteLine($"[PrivateCompany] Found via rep_user_id: {company.company_name}");
                return Ok(company);
            }

            // 2. Match by email
            company = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT p.id, p.company_name, p.phone, p.address,
                         COALESCE(p.services_offered, '') AS services_provided
                  FROM private_cleaning_companies p
                  WHERE p.email = @Email AND COALESCE(p.is_active, 1) = 1
                  LIMIT 1",
                new { Email = (string)userInfo.email });

            if (company != null)
            {
                Console.WriteLine($"[PrivateCompany] Found via email: {company.company_name}. Auto-linking.");
                try { await connection.ExecuteAsync(
                    "UPDATE private_cleaning_companies SET rep_user_id=@Uid WHERE id=@Id",
                    new { Uid = userId, Id = (int)company.id }); }
                catch { }
                return Ok(company);
            }

            // 3. Match by contact_person name
            company = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT p.id, p.company_name, p.phone, p.address,
                         COALESCE(p.services_offered, '') AS services_provided
                  FROM private_cleaning_companies p
                  WHERE p.contact_person = @Name AND COALESCE(p.is_active, 1) = 1
                  LIMIT 1",
                new { Name = (string)userInfo.name });

            if (company != null)
            {
                Console.WriteLine($"[PrivateCompany] Found via contact_person: {company.company_name}. Auto-linking.");
                try { await connection.ExecuteAsync(
                    "UPDATE private_cleaning_companies SET rep_user_id=@Uid WHERE id=@Id",
                    new { Uid = userId, Id = (int)company.id }); }
                catch { }
                return Ok(company);
            }

            // 4. Auto-create a placeholder company linked to this user
            Console.WriteLine($"[PrivateCompany] Auto-creating company for user {userId}...");
            try
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO private_cleaning_companies
                        (company_name, contact_person, phone, email,
                         license_number, address, services_offered,
                         status, is_active, rep_user_id, created_at, updated_at)
                      VALUES
                        (@cn, @cp, @ph, @em, '', '', '',
                         'Active', 1, @uid, NOW(), NOW())",
                    new {
                        cn  = (string)userInfo.name + "'s Company",
                        cp  = (string)userInfo.name,
                        ph  = "",
                        em  = (string)userInfo.email,
                        uid = userId
                    });

                var newId = await connection.QueryFirstAsync<long>("SELECT LAST_INSERT_ID()");
                Console.WriteLine($"[PrivateCompany] Auto-created company ID {newId} for user {userId}.");

                company = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT p.id, p.company_name, p.phone, p.address,
                             COALESCE(p.services_offered, '') AS services_provided
                      FROM private_cleaning_companies p WHERE p.id = @Id",
                    new { Id = newId });

                return Ok(company);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivateCompany] Auto-create failed: {ex.Message}");
                return StatusCode(500, new {
                    message = $"Could not auto-create company. Error: {ex.Message}. " +
                              $"Ask admin to link your account (User ID: {userId}) to a company at Private Cleaning Companies page."
                });
            }
        }

        // ── Private Company Rep: submit receipt ──────────────────────
        [HttpPost("submit-private-receipt")]
        public async Task<IActionResult> SubmitPrivateReceipt([FromBody] PrivateReceiptSubmission req)
        {
            using var connection = CreateConnection();

            // Resolve company info — try by rep_user_id first, then by email match
            var company = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT p.id, p.company_name 
                  FROM private_cleaning_companies p
                  WHERE p.rep_user_id = @Uid AND COALESCE(p.is_active, 1) = 1
                  LIMIT 1",
                new { Uid = req.UserId });

            if (company == null)
            {
                company = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT p.id, p.company_name
                      FROM private_cleaning_companies p
                      JOIN users u ON u.email = p.email
                      WHERE u.id = @Uid AND COALESCE(p.is_active, 1) = 1
                      LIMIT 1",
                    new { Uid = req.UserId });

                // Auto-fix rep_user_id so next call is fast
                if (company != null)
                {
                    try
                    {
                        await connection.ExecuteAsync(
                            "UPDATE private_cleaning_companies SET rep_user_id = @Uid WHERE id = @Id",
                            new { Uid = req.UserId, Id = (int)company.id });
                    }
                    catch { }
                }
            }

            var companyId   = (int?)company?.id   ?? 0;
            var companyName = (string?)company?.company_name ?? req.CompanyName ?? "";

            var weredaName = req.WeredaId > 0
                ? await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM weredas WHERE id = @Id", new { Id = req.WeredaId })
                : null;

            var vehiclePlate = req.VehicleId.HasValue
                ? await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT plate_number FROM vehicles WHERE id = @Id", new { Id = req.VehicleId })
                : null;

            var repName = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT name FROM users WHERE id = @Id", new { Id = req.UserId }) ?? "";

            TimeSpan receiptTime = TimeSpan.TryParse(req.Time, out var t) ? t : TimeSpan.Zero;
            DateTime receiptDate = DateTime.TryParse(req.Date, out var d) ? d : DateTime.Today;
            decimal  total       = req.Kilogram * req.Price;

            await connection.ExecuteAsync(
                @"INSERT INTO private_company_receipts
                    (company_id, company_name, wereda_id, wereda_name,
                     vehicle_id, plate_number, driver_id, driver_name,
                     receipt_time, receipt_date, kilogram, price, total_amount,
                     notes, registered_by, status, registered_at)
                  VALUES
                    (@CompanyId, @CompanyName, @WeredaId, @WeredaName,
                     @VehicleId, @PlateNumber, @DriverId, @DriverName,
                     @ReceiptTime, @ReceiptDate, @Kilogram, @Price, @Total,
                     @Notes, @RegisteredBy, 'Registered', NOW())",
                new {
                    CompanyId    = companyId,
                    CompanyName  = companyName,
                    WeredaId     = req.WeredaId,
                    WeredaName   = weredaName ?? "",
                    VehicleId    = req.VehicleId,
                    PlateNumber  = vehiclePlate ?? "",
                    DriverId     = req.UserId,
                    DriverName   = repName,
                    ReceiptTime  = receiptTime,
                    ReceiptDate  = receiptDate,
                    Kilogram     = req.Kilogram,
                    Price        = req.Price,
                    Total        = total,
                    Notes        = req.Notes ?? "",
                    RegisteredBy = repName
                });

            return Ok(new { success = true, message = "Receipt submitted successfully.", total });
        }

        // ── Private Company Rep: get their receipt history ────────────
        [HttpGet("private-receipts/{userId}")]
        public async Task<IActionResult> GetPrivateReceipts(int userId)
        {
            using var connection = CreateConnection();
            var receipts = await connection.QueryAsync<dynamic>(
                @"SELECT id, company_name, wereda_name, plate_number, driver_name,
                         DATE_FORMAT(receipt_date, '%Y-%m-%d') AS receipt_date,
                         TIME_FORMAT(receipt_time, '%H:%i')    AS receipt_time,
                         kilogram, price, total_amount, status, registered_at
                  FROM private_company_receipts
                  WHERE driver_id = @UserId
                  ORDER BY registered_at DESC LIMIT 100",
                new { UserId = userId });
            return Ok(receipts);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? VehicleId { get; set; }
        public string? VehicleName { get; set; }
    }

    public class ReceiptSubmission
    {
        public string ReceiptType { get; set; } = "Mahberat";
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public int WeredaId { get; set; }
        public int MahberatId { get; set; }
        public int? VehicleId { get; set; }
        public decimal Kilogram { get; set; }
        public decimal Rate { get; set; }
        public decimal Total { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Notes { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class StatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
        public string ReceiptType { get; set; } = "Mahberat";
    }

    public class PrivateReceiptSubmission
    {
        public int     UserId     { get; set; }
        public string? CompanyName{ get; set; }
        public int     WeredaId   { get; set; }
        public int?    VehicleId  { get; set; }
        public decimal Kilogram   { get; set; }
        public decimal Price      { get; set; }
        public string  Date       { get; set; } = "";
        public string  Time       { get; set; } = "";
        public string? Notes      { get; set; }
        public string? ImageUrl   { get; set; }
    }
}
