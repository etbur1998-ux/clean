using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class PrivateCleaningCompanyModel : PageModel
    {
        private readonly string _cs;

        public List<PrivateCompanyRow> Companies     { get; set; } = new();
        public List<AvailableUser>     AvailableUsers { get; set; } = new();
        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage   { get; set; } = "";

        public PrivateCleaningCompanyModel(IConfiguration cfg)
            => _cs = cfg.GetConnectionString("DefaultConnection") ?? "";

        // ── GET ──────────────────────────────────────────────────────────────
        public IActionResult OnGet(
            [FromQuery] string? ok  = null,
            [FromQuery] string? err = null)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToPage("/Login");

            SuccessMessage = ok  ?? "";
            ErrorMessage   = err ?? "";
            LoadData();
            return Page();
        }

        // ── POST ─────────────────────────────────────────────────────────────
        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToPage("/Login");

            string F(string k) => Request.Form[k].ToString().Trim();

            var action = F("action");
            int.TryParse(F("Id"), out int id);

            try
            {
                using var db = new MySqlConnection(_cs);

                switch (action)
                {
                    // ── Create company ────────────────────────────────────
                    case "create":
                        var cn = F("CompanyName");
                        if (string.IsNullOrWhiteSpace(cn))
                            throw new Exception("Company Name is required.");

                        int? repUserId = null;
                        if (int.TryParse(F("RepUserId"), out int uidVal) && uidVal > 0)
                        {
                            repUserId = uidVal;
                        }

                        db.Execute(
                            @"INSERT INTO private_cleaning_companies
                                (company_name, contact_person, phone, email,
                                 license_number, status, services_offered, address,
                                 is_active, rep_user_id, created_at, updated_at)
                              VALUES
                                (@cn, @cp, @ph, @em, @ln, @st, @sv, @ad, 1, @repUserId, NOW(), NOW())",
                            new {
                                cn, cp = F("ContactPerson"), ph = F("Phone"),
                                em = F("Email"), ln = F("LicenseNumber"),
                                st = string.IsNullOrEmpty(F("Status")) ? "Active" : F("Status"),
                                sv = F("ServicesOffered"), ad = F("Address"),
                                repUserId
                            });

                        if (repUserId.HasValue)
                        {
                            db.Execute(
                                "UPDATE users SET role='PrivateCompanyRep', updated_at=NOW() WHERE id=@uid",
                                new { uid = repUserId.Value });
                        }

                        SuccessMessage = $"Company '{cn}' created successfully!";
                        break;

                    // ── Update company ────────────────────────────────────
                    case "update":
                        int? updateRepUserId = null;
                        if (int.TryParse(F("RepUserId"), out int uidValUpdate) && uidValUpdate > 0)
                        {
                            updateRepUserId = uidValUpdate;
                        }

                        var existingRepUserId = db.QueryFirstOrDefault<int?>(
                            "SELECT rep_user_id FROM private_cleaning_companies WHERE id=@id",
                            new { id });

                        var finalRepUserId = updateRepUserId ?? existingRepUserId;

                        if (finalRepUserId.HasValue && finalRepUserId != existingRepUserId)
                        {
                            db.Execute(
                                "UPDATE users SET role='PrivateCompanyRep', updated_at=NOW() WHERE id=@uid",
                                new { uid = finalRepUserId.Value });
                        }

                        db.Execute(
                            @"UPDATE private_cleaning_companies SET
                                company_name=@cn, contact_person=@cp, phone=@ph,
                                email=@em, license_number=@ln, status=@st,
                                services_offered=@sv, address=@ad, rep_user_id=@repUserId, updated_at=NOW()
                              WHERE id=@id",
                            new {
                                cn = F("CompanyName"), cp = F("ContactPerson"),
                                ph = F("Phone"), em = F("Email"),
                                ln = F("LicenseNumber"), st = F("Status"),
                                sv = F("ServicesOffered"), ad = F("Address"),
                                repUserId = finalRepUserId, id
                            });

                        SuccessMessage = "Company updated.";
                        break;

                    // ── Toggle status ─────────────────────────────────────
                    case "toggle":
                        db.Execute(
                            @"UPDATE private_cleaning_companies
                              SET status = CASE WHEN status='Active' THEN 'Inactive' ELSE 'Active' END,
                                  is_active = CASE WHEN status='Active' THEN 0 ELSE 1 END,
                                  updated_at = NOW()
                              WHERE id=@id", new { id });
                        SuccessMessage = "Status toggled.";
                        break;

                    // ── Delete ────────────────────────────────────────────
                    case "delete":
                        db.Execute(
                            "UPDATE private_cleaning_companies SET status='Inactive', is_active=0, updated_at=NOW() WHERE id=@id",
                            new { id });
                        SuccessMessage = "Company deactivated.";
                        break;

                    // ── Link existing registered user to company ──────────
                    case "link_user":
                        int.TryParse(F("UserId"), out int userId);
                        if (userId == 0) throw new Exception("Please select a user.");

                        var userInfo = db.QueryFirstOrDefault<dynamic>(
                            "SELECT id, name, email, role FROM users WHERE id=@uid AND is_active=1",
                            new { uid = userId });
                        if (userInfo == null)
                            throw new Exception("Selected user not found or inactive.");

                        var alreadyLinkedCompanyId = db.QueryFirstOrDefault<int?>(
                            "SELECT id FROM private_cleaning_companies WHERE rep_user_id=@uid AND id != @id",
                            new { uid = userId, id });

                        if (alreadyLinkedCompanyId.HasValue)
                        {
                            db.Execute(
                                "UPDATE private_cleaning_companies SET rep_user_id=NULL, updated_at=NOW() WHERE id=@oldId",
                                new { oldId = alreadyLinkedCompanyId.Value });
                            Console.WriteLine($"[PrivateCompany] Unlinked user {userId} from company {alreadyLinkedCompanyId.Value} before re-linking.");
                        }

                        db.Execute(
                            "UPDATE users SET role='PrivateCompanyRep', updated_at=NOW() WHERE id=@uid",
                            new { uid = userId });

                        db.Execute(
                            "UPDATE private_cleaning_companies SET rep_user_id=@uid, updated_at=NOW() WHERE id=@id",
                            new { uid = userId, id });

                        var linkedUserName = (string)userInfo.name;
                        var linkedEmail    = (string)userInfo.email;
                        if (alreadyLinkedCompanyId.HasValue)
                            SuccessMessage = $"User '{linkedUserName}' ({linkedEmail}) moved to this company (unlinked from previous company).";
                        else
                            SuccessMessage = $"User '{linkedUserName}' ({linkedEmail}) linked as PrivateCompanyRep for this company.";
                        break;

                    // ── Unlink user from company ──────────────────────────
                    case "unlink_user":
                        db.Execute(
                            "UPDATE private_cleaning_companies SET rep_user_id=NULL, updated_at=NOW() WHERE id=@id",
                            new { id });
                        SuccessMessage = "User unlinked from company.";
                        break;

                    // ── Register new user and link to company ────────────
                    case "register_and_link_user":
                        var newUserName = F("NewUserName");
                        var newUserEmail = F("NewUserEmail");
                        var newUserPassword = F("NewUserPassword");
                        var newUserPhone = F("NewUserPhone");
                        var newUserAddress = F("NewUserAddress");

                        if (string.IsNullOrWhiteSpace(newUserName)) throw new Exception("Representative Name is required.");
                        if (string.IsNullOrWhiteSpace(newUserEmail)) throw new Exception("Representative Email is required.");
                        if (string.IsNullOrWhiteSpace(newUserPassword)) throw new Exception("Representative Password is required.");

                        // Check duplicate email
                        var emailExists = db.QueryFirstOrDefault<int>(
                            "SELECT COUNT(*) FROM users WHERE email=@e", new { e = newUserEmail });
                        if (emailExists > 0)
                            throw new Exception($"Email '{newUserEmail}' is already registered.");

                        var adminUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                        // Insert new user as PrivateCompanyRep
                        db.Execute(
                            @"INSERT INTO users (name, email, password, role, phone, address, is_active, created_by, created_at, updated_at)
                              VALUES (@n, @e, @pw, 'PrivateCompanyRep', @ph, @ad, 1, @cb, NOW(), NOW())",
                            new { n = newUserName, e = newUserEmail, pw = newUserPassword,
                                  ph = newUserPhone ?? "", ad = newUserAddress ?? "", cb = adminUserId });

                        var linkedNewId = db.QueryFirst<int>("SELECT LAST_INSERT_ID()");

                        // Link to company
                        db.Execute(
                            "UPDATE private_cleaning_companies SET rep_user_id=@uid, updated_at=NOW() WHERE id=@id",
                            new { uid = linkedNewId, id });

                        SuccessMessage = $"Representative '{newUserName}' registered and linked to this company successfully.";
                        break;

                    default:
                        ErrorMessage = $"Unknown action: '{action}'";
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivateCompany] ERROR: {ex.Message}");
                ErrorMessage = ex.Message.Contains("Duplicate entry")
                    ? "That email is already registered."
                    : ex.Message;
            }

            LoadData();
            return Page();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                using var db = new MySqlConnection(_cs);

                // Load companies with linked rep user info
                Companies = db.Query<PrivateCompanyRow>(
                    @"SELECT p.id,
                             p.company_name AS CompanyName,
                             p.license_number AS LicenseNumber,
                             p.contact_person AS ContactPerson,
                             p.phone,
                             p.email,
                             p.address,
                             p.services_offered AS ServicesOffered,
                             p.status,
                             u.id   AS RepUserId,
                             u.name AS RepUserName,
                             u.email AS RepUserEmail,
                             u.phone AS RepUserPhone,
                             u.is_active AS RepUserActive
                      FROM private_cleaning_companies p
                      LEFT JOIN users u ON u.id = p.rep_user_id
                      ORDER BY p.id DESC").ToList();

                // Load users available to be linked as private reps:
                // All users regardless of current role — admin can assign any user
                AvailableUsers = db.Query<AvailableUser>(
                    @"SELECT u.id, u.name, u.email, u.role, u.phone,
                             COALESCE(p.company_name,'') AS LinkedCompany
                      FROM users u
                      LEFT JOIN private_cleaning_companies p ON p.rep_user_id = u.id
                      WHERE u.is_active = 1
                      ORDER BY u.name ASC").ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivateCompany.LoadData] {ex.Message}");
                ErrorMessage = "Error loading data.";
            }
        }
    }

    // ── View Models ────────────────────────────────────────────────────────────
    public class PrivateCompanyRow
    {
        public int     Id              { get; set; }
        public string  CompanyName     { get; set; } = "";
        public string  ContactPerson   { get; set; } = "";
        public string  Phone           { get; set; } = "";
        public string  Email           { get; set; } = "";
        public string  LicenseNumber   { get; set; } = "";
        public string  Status          { get; set; } = "Active";
        public string  ServicesOffered { get; set; } = "";
        public string  Address         { get; set; } = "";
        public int?    RepUserId       { get; set; }
        public string? RepUserName     { get; set; }
        public string? RepUserEmail    { get; set; }
        public string? RepUserPhone    { get; set; }
        public bool?   RepUserActive   { get; set; }
        public DateTime? CreatedAt     { get; set; }
    }

    public class AvailableUser
    {
        public int    Id            { get; set; }
        public string Name          { get; set; } = "";
        public string Email         { get; set; } = "";
        public string Role          { get; set; } = "";
        public string Phone         { get; set; } = "";
        public string LinkedCompany { get; set; } = "";
    }
}
