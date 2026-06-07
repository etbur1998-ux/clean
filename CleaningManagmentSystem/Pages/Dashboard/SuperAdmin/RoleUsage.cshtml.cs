using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class RoleUsageModel : PageModel
    {
        private readonly string _connectionString;

        public RoleUsageModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [BindProperty]
        public int? EditId { get; set; }

        [BindProperty]
        public string RoleName { get; set; } = "";

        [BindProperty]
        public string DisplayName { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public string UsageContext { get; set; } = "";

        [BindProperty]
        public string PrimaryResponsibilities { get; set; } = "";

        [BindProperty]
        public string DailyActivities { get; set; } = "";

        [BindProperty]
        public string ReportsAccess { get; set; } = "";

        [BindProperty]
        public string ModulesAccess { get; set; } = "";

        [BindProperty]
        public int AccessLevel { get; set; }

        [BindProperty]
        public bool CanCreateUsers { get; set; }

        [BindProperty]
        public bool CanViewFinancials { get; set; }

        [BindProperty]
        public bool CanManageDispatch { get; set; }

        [BindProperty]
        public bool CanViewPayroll { get; set; }

        [BindProperty]
        public bool CanManageStaff { get; set; }

        public IEnumerable<RoleDefinition> RoleDefinitions { get; set; } = new List<RoleDefinition>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            LoadRoles();
            return Page();
        }

        public IActionResult OnPostLoad()
        {
            LoadRoles();
            return Page();
        }

        public IActionResult OnPostEdit(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var role = connection.QueryFirstOrDefault<RoleDefinition>(
                    "SELECT * FROM role_definitions WHERE id = @Id", new { Id = id });

                if (role != null)
                {
                    EditId = id;
                    RoleName = role.RoleName;
                    DisplayName = role.DisplayName;
                    Description = role.Description;
                    UsageContext = role.UsageContext;
                    PrimaryResponsibilities = role.PrimaryResponsibilities;
                    DailyActivities = role.DailyActivities;
                    ReportsAccess = role.ReportsAccess;
                    ModulesAccess = role.ModulesAccess;
                    AccessLevel = role.AccessLevel;
                    CanCreateUsers = role.CanCreateUsers;
                    CanViewFinancials = role.CanViewFinancials;
                    CanManageDispatch = role.CanManageDispatch;
                    CanViewPayroll = role.CanViewPayroll;
                    CanManageStaff = role.CanManageStaff;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RoleUsage] Error loading role: {ex.Message}");
                ErrorMessage = "Error loading role details.";
            }

            LoadRoles();
            return Page();
        }

        public IActionResult OnPostSave()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            if (string.IsNullOrEmpty(RoleName) || string.IsNullOrEmpty(DisplayName))
            {
                ErrorMessage = "Role Name and Display Name are required.";
                LoadRoles();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                if (EditId.HasValue)
                {
                    connection.Execute(@"
                        UPDATE role_definitions SET
                            display_name = @DisplayName,
                            description = @Description,
                            usage_context = @UsageContext,
                            primary_responsibilities = @PrimaryResponsibilities,
                            daily_activities = @DailyActivities,
                            reports_access = @ReportsAccess,
                            modules_access = @ModulesAccess,
                            access_level = @AccessLevel,
                            can_create_users = @CanCreateUsers,
                            can_view_financials = @CanViewFinancials,
                            can_manage_dispatch = @CanManageDispatch,
                            can_view_payroll = @CanViewPayroll,
                            can_manage_staff = @CanManageStaff,
                            updated_at = NOW()
                        WHERE id = @EditId",
                        new
                        {
                            EditId,
                            DisplayName,
                            Description,
                            UsageContext,
                            PrimaryResponsibilities,
                            DailyActivities,
                            ReportsAccess,
                            ModulesAccess,
                            AccessLevel,
                            CanCreateUsers,
                            CanViewFinancials,
                            CanManageDispatch,
                            CanViewPayroll,
                            CanManageStaff
                        });

                    SuccessMessage = $"Role '{RoleName}' updated successfully.";
                    Console.WriteLine($"[RoleUsage] Updated role ID: {EditId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RoleUsage] Database error: {ex.Message}");
                ErrorMessage = "Database error occurred.";
            }

            LoadRoles();
            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToPage("/Login");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM role_definitions WHERE id = @Id", new { Id = id });
                SuccessMessage = "Role definition deleted.";
                Console.WriteLine($"[RoleUsage] Deleted role ID: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RoleUsage] Delete error: {ex.Message}");
                ErrorMessage = "Error deleting role.";
            }

            LoadRoles();
            return Page();
        }

        private void LoadRoles()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                RoleDefinitions = connection.Query<RoleDefinition>(
                    "SELECT * FROM role_definitions ORDER BY access_level DESC, display_name ASC").ToList();

                Console.WriteLine($"[RoleUsage] Loaded {RoleDefinitions.Count()} role definitions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RoleUsage] Load error: {ex.Message}");
                ErrorMessage = "Error loading role definitions.";
            }
        }
    }

    public class RoleActivityLogEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string ActivityType { get; set; } = "";
        public string PageAccessed { get; set; } = "";
        public string ActionPerformed { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class RolePermissionEntry
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public string PermissionType { get; set; } = "";
        public bool IsAllowed { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
