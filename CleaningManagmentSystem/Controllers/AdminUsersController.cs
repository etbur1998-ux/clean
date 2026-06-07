using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Controllers
{
    /// <summary>
    /// Handles all user CRUD for the SuperAdmin Users page.
    /// Route: /admin/users/*
    /// </summary>
    [Route("admin/users")]
    public class AdminUsersController : Controller
    {
        private readonly string _cs;

        public AdminUsersController(IConfiguration cfg)
            => _cs = cfg.GetConnectionString("DefaultConnection") ?? "";

        private IActionResult RequireAdmin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return Redirect("/Login");
            return null!;
        }

        // ── POST /admin/users/create ────────────────────────────────────────
        [HttpPost("create")]
        public IActionResult Create(
            [FromForm] string Name,
            [FromForm] string Email,
            [FromForm] string Password,
            [FromForm] string Role,
            [FromForm] string? Phone,
            [FromForm] string? Address,
            [FromForm] bool IsActive = true)
        {
            var check = RequireAdmin(); if (check != null) return check;

            Console.WriteLine($"[AdminUsers.Create] name={Name} email={Email} role='{Role}' active={IsActive}");

            if (string.IsNullOrWhiteSpace(Name))     return Redirect("/Dashboard/SuperAdmin/Users?err=Name+is+required");
            if (string.IsNullOrWhiteSpace(Email))    return Redirect("/Dashboard/SuperAdmin/Users?err=Email+is+required");
            if (string.IsNullOrWhiteSpace(Password)) return Redirect("/Dashboard/SuperAdmin/Users?err=Password+is+required");
            if (string.IsNullOrWhiteSpace(Role))     return Redirect("/Dashboard/SuperAdmin/Users?err=Please+select+a+Role");

            var normalizedRole = NormalizeRole(Role);

            try
            {
                using var db = new MySqlConnection(_cs);
                // Check duplicate email
                var exists = db.QueryFirstOrDefault<int>(
                    "SELECT COUNT(*) FROM users WHERE email=@e", new { e = Email });
                if (exists > 0)
                    return Redirect($"/Dashboard/SuperAdmin/Users?err=Email+{Uri.EscapeDataString(Email)}+already+registered");

                var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                db.Execute(
                    @"INSERT INTO users (name,email,password,role,phone,address,is_active,created_by,created_at,updated_at)
                      VALUES (@n,@e,@pw,@r,@ph,@ad,@act,@cb,NOW(),NOW())",
                    new { n = Name, e = Email, pw = Password, r = normalizedRole,
                          ph = Phone ?? "", ad = Address ?? "",
                          act = IsActive, cb = adminId });

                return Redirect($"/Dashboard/SuperAdmin/Users?ok={Uri.EscapeDataString($"User '{Name}' created with role '{normalizedRole}'")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminUsers.Create] ERROR: {ex.Message}");
                var msg = ex.Message.Contains("Duplicate") ? $"Email '{Email}' already exists" : ex.Message;
                return Redirect($"/Dashboard/SuperAdmin/Users?err={Uri.EscapeDataString(msg)}");
            }
        }

        // ── POST /admin/users/create-private ───────────────────────────────
        [HttpPost("create-private")]
        public IActionResult CreatePrivate(
            [FromForm] string Name,
            [FromForm] string Email,
            [FromForm] string Password,
            [FromForm] string? Phone,
            [FromForm] string? Address,
            [FromForm] string CompanyName,
            [FromForm] string? LicenseNumber,
            [FromForm] string? CompanyAddress,
            [FromForm] string? ServicesOffered)
        {
            var check = RequireAdmin(); if (check != null) return check;

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(CompanyName))
                return Redirect("/Dashboard/SuperAdmin/Users?err=Name+Email+Password+and+CompanyName+are+required");

            try
            {
                using var db = new MySqlConnection(_cs);
                var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;

                db.Execute(
                    @"INSERT INTO users (name,email,password,role,phone,address,is_active,created_by,created_at,updated_at)
                      VALUES (@n,@e,@pw,'PrivateCompanyRep',@ph,@ad,1,@cb,NOW(),NOW())",
                    new { n = Name, e = Email, pw = Password,
                          ph = Phone ?? "", ad = Address ?? "", cb = adminId });

                var newId = (int)db.QueryFirst<long>("SELECT LAST_INSERT_ID()");

                db.Execute(
                    @"INSERT INTO private_cleaning_companies
                        (company_name,license_number,contact_person,phone,email,
                         address,services_offered,status,is_active,rep_user_id,created_at,updated_at)
                      VALUES
                        (@cn,@ln,@cp,@ph,@em,@ca,@so,'Active',1,@rid,NOW(),NOW())",
                    new { cn = CompanyName, ln = LicenseNumber ?? "", cp = Name,
                          ph = Phone ?? "", em = Email,
                          ca = CompanyAddress ?? "", so = ServicesOffered ?? "",
                          rid = newId });

                return Redirect($"/Dashboard/SuperAdmin/Users?ok={Uri.EscapeDataString($"Private rep '{Name}' ({CompanyName}) registered")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminUsers.CreatePrivate] ERROR: {ex.Message}");
                var msg = ex.Message.Contains("Duplicate") ? $"Email '{Email}' already exists" : ex.Message;
                return Redirect($"/Dashboard/SuperAdmin/Users?err={Uri.EscapeDataString(msg)}");
            }
        }

        // ── POST /admin/users/update ────────────────────────────────────────
        [HttpPost("update")]
        public IActionResult Update(
            [FromForm] int UserId,
            [FromForm] string Name,
            [FromForm] string Email,
            [FromForm] string Role,
            [FromForm] string? Phone,
            [FromForm] string? Address,
            [FromForm] string? Password,
            [FromForm] bool IsActive = true)
        {
            var check = RequireAdmin(); if (check != null) return check;

            var r = NormalizeRole(Role);
            try
            {
                using var db = new MySqlConnection(_cs);
                if (string.IsNullOrWhiteSpace(Password))
                {
                    db.Execute(
                        @"UPDATE users SET name=@n,email=@e,role=@r,phone=@ph,
                                 address=@ad,is_active=@act,updated_at=NOW()
                          WHERE id=@id",
                        new { n = Name, e = Email, r, ph = Phone ?? "",
                              ad = Address ?? "", act = IsActive, id = UserId });
                }
                else
                {
                    db.Execute(
                        @"UPDATE users SET name=@n,email=@e,role=@r,phone=@ph,
                                 address=@ad,is_active=@act,password=@pw,updated_at=NOW()
                          WHERE id=@id",
                        new { n = Name, e = Email, r, ph = Phone ?? "",
                              ad = Address ?? "", act = IsActive, pw = Password, id = UserId });
                }
                return Redirect($"/Dashboard/SuperAdmin/Users?ok={Uri.EscapeDataString($"User updated with role '{r}'")}");
            }
            catch (Exception ex)
            {
                return Redirect($"/Dashboard/SuperAdmin/Users?err={Uri.EscapeDataString(ex.Message)}");
            }
        }

        // ── POST /admin/users/toggle ────────────────────────────────────────
        [HttpPost("toggle")]
        public IActionResult Toggle([FromForm] int UserId)
        {
            var check = RequireAdmin(); if (check != null) return check;
            using var db = new MySqlConnection(_cs);
            db.Execute("UPDATE users SET is_active=NOT is_active,updated_at=NOW() WHERE id=@id", new { id = UserId });
            return Redirect("/Dashboard/SuperAdmin/Users?ok=Status+updated");
        }

        // ── POST /admin/users/deactivate ────────────────────────────────────
        [HttpPost("deactivate")]
        public IActionResult Deactivate([FromForm] int UserId)
        {
            var check = RequireAdmin(); if (check != null) return check;
            using var db = new MySqlConnection(_cs);
            db.Execute("UPDATE users SET is_active=0,updated_at=NOW() WHERE id=@id", new { id = UserId });
            return Redirect("/Dashboard/SuperAdmin/Users?ok=User+deactivated");
        }

        // ── POST /admin/users/reset-password ───────────────────────────────
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromForm] int UserId, [FromForm] string Password)
        {
            var check = RequireAdmin(); if (check != null) return check;
            if (string.IsNullOrWhiteSpace(Password))
                return Redirect("/Dashboard/SuperAdmin/Users?err=Password+cannot+be+empty");
            using var db = new MySqlConnection(_cs);
            db.Execute("UPDATE users SET password=@pw,updated_at=NOW() WHERE id=@id",
                new { pw = Password, id = UserId });
            return Redirect("/Dashboard/SuperAdmin/Users?ok=Password+reset");
        }

        // ── Role normalizer ─────────────────────────────────────────────────
        public static string NormalizeRole(string raw) => (raw ?? "").Trim().ToLower() switch
        {
            "driver"            => "driver",
            "outsource"         => "outsource",
            "privatecompanyrep" => "PrivateCompanyRep",
            "manager"           => "manager",
            "superadmin"        => "superadmin",
            "super admin"       => "superadmin",
            "super_admin"       => "superadmin",
            "staff"             => "staff",
            "wereda_mahberat"   => "wereda_mahberat",
            "weredamahberat"    => "wereda_mahberat",
            "dispatchofficer"   => "DispatchOfficer",
            "dispatch_officer"  => "DispatchOfficer",
            _                   => (raw ?? "").Trim()
        };
    }
}
