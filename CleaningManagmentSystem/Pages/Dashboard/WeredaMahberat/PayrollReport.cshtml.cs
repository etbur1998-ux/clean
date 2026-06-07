using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.WeredaMahberat
{
    public class PayrollReportModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public string? FilterMonth { get; set; }

        [BindProperty]
        public int? FilterYear { get; set; }

        [BindProperty]
        public string? FilterEmployeeName { get; set; }

        [BindProperty]
        public Payroll? Payroll { get; set; }

        public IEnumerable<Payroll>? PayrollRecords { get; set; }

        public decimal TotalPayroll { get; set; }

        public int TotalEmployees { get; set; }

        public PayrollReportModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[PayrollReport] No UserName in session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"[PayrollReport] OnGet called by UserName: {userName}");
            LoadPayrollRecords();
            return Page();
        }

        public IActionResult OnPostFilter()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"[PayrollReport] Filtering by Month: {FilterMonth}, Year: {FilterYear}");
            LoadPayrollRecords();
            return Page();
        }

        public IActionResult OnPostGenerateReport()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            if (Payroll == null || string.IsNullOrEmpty(Payroll.EmployeeName))
            {
                ModelState.AddModelError(string.Empty, "Employee Name is required");
                LoadPayrollRecords();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                Payroll.NetSalary = Payroll.BaseSalary + Payroll.Bonus - Payroll.Deductions;
                Payroll.Status = Payroll.Status ?? "pending";
                Payroll.CreatedAt = DateTime.Now;

                connection.Execute(
                    @"INSERT INTO payroll (employee_name, employee_role, base_salary, bonus, deductions, net_salary, month, year, status, payment_date, created_at)
                    VALUES (@EmployeeName, @EmployeeRole, @BaseSalary, @Bonus, @Deductions, @NetSalary, @Month, @Year, @Status, @PaymentDate, @CreatedAt)",
                    new
                    {
                        Payroll.EmployeeName,
                        Payroll.EmployeeRole,
                        Payroll.BaseSalary,
                        Payroll.Bonus,
                        Payroll.Deductions,
                        Payroll.NetSalary,
                        Payroll.Month,
                        Payroll.Year,
                        Payroll.Status,
                        Payroll.PaymentDate,
                        CreatedAt = DateTime.Now
                    });

                Console.WriteLine($"[PayrollReport] Generated payroll for: {Payroll.EmployeeName}");
                TempData["SuccessMessage"] = "Payroll record generated successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollReport] Generate error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error generating payroll record. Please try again.");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUpdateStatus(int id, string status)
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
                    "UPDATE payroll SET status = @Status WHERE id = @Id",
                    new { Id = id, Status = status });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[PayrollReport] Updated status for ID: {id} to {status}");
                    TempData["SuccessMessage"] = "Status updated successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Payroll record not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollReport] Update status error: {ex.Message}");
                TempData["ErrorMessage"] = "Error updating status";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteRecord(int id)
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
                    "DELETE FROM payroll WHERE id = @Id",
                    new { Id = id });

                if (affectedRows > 0)
                {
                    Console.WriteLine($"[PayrollReport] Deleted record ID: {id}");
                    TempData["SuccessMessage"] = "Payroll record deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Payroll record not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollReport] Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting record";
            }

            return RedirectToPage();
        }

        private void LoadPayrollRecords()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                string sql = "SELECT * FROM payroll WHERE 1=1";
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

                if (!string.IsNullOrEmpty(FilterEmployeeName))
                {
                    sql += " AND employee_name LIKE @EmployeeName";
                    parameters.Add("EmployeeName", $"%{FilterEmployeeName}%");
                }

                sql += " ORDER BY year DESC, month DESC, created_at DESC";

                var records = connection.Query<Payroll>(sql, parameters).ToList();

                PayrollRecords = records;
                TotalEmployees = records.Select(r => r.EmployeeName).Distinct().Count();
                TotalPayroll = records.Sum(r => r.NetSalary);

                Console.WriteLine($"[PayrollReport] Loaded {records.Count} records, Total: {TotalPayroll}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayrollReport] Load error: {ex.Message}");
                PayrollRecords = new List<Payroll>();
            }
        }
    }
}
