using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.SuperAdmin
{
    public class ReceiptPayrollModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string ActiveTab { get; set; } = "receipts";

        [BindProperty]
        public string SearchQuery { get; set; } = "";

        [BindProperty]
        public int EmployeeId { get; set; }

        [BindProperty]
        public string EmployeeName { get; set; } = "";

        [BindProperty]
        public decimal BaseSalary { get; set; }

        [BindProperty]
        public decimal Bonus { get; set; }

        [BindProperty]
        public decimal Deductions { get; set; }

        [BindProperty]
        public decimal NetSalary { get; set; }

        [BindProperty]
        public string Month { get; set; } = "";

        [BindProperty]
        public int Year { get; set; } = DateTime.Now.Year;

        [BindProperty]
        public string Status { get; set; } = "Pending";

        public List<PayrollRecord> PayrollRecords { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public ReceiptPayrollModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[ReceiptPayroll] OnGet called");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[ReceiptPayroll] No session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(ActiveTab))
            {
                ActiveTab = "receipts";
            }

            LoadPayrollRecords();
            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine($"[ReceiptPayroll] OnPost called with action");
            
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            var action = Request.Form["action"];
            var tab = Request.Form["tab"].ToString();
            ActiveTab = tab;

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                if (action == "create")
                {
                    NetSalary = BaseSalary + Bonus - Deductions;
                    CreatePayrollRecord(connection);
                    SuccessMessage = "Payroll record created!";
                }
                else if (action == "update")
                {
                    NetSalary = BaseSalary + Bonus - Deductions;
                    UpdatePayrollRecord(connection);
                    SuccessMessage = "Payroll record updated!";
                }
                else if (action == "delete")
                {
                    DeletePayrollRecord(connection);
                    SuccessMessage = "Payroll record deleted!";
                }
                else if (action == "calculate")
                {
                    NetSalary = BaseSalary + Bonus - Deductions;
                    SuccessMessage = $"Calculated Net Salary: {NetSalary:C}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReceiptPayroll] Database error: {ex.Message}");
                ErrorMessage = "Database error occurred.";
            }

            LoadPayrollRecords();
            return Page();
        }

        private void LoadPayrollRecords()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = "SELECT * FROM payroll ORDER BY id DESC";
                
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = @"SELECT * FROM payroll 
                              WHERE employee_name LIKE @Search OR month LIKE @Search 
                              ORDER BY id DESC";
                    PayrollRecords = connection.Query<PayrollRecord>(query, new { Search = $"%{SearchQuery}%" }).ToList();
                }
                else
                {
                    PayrollRecords = connection.Query<PayrollRecord>(query).ToList();
                }
                
                Console.WriteLine($"[ReceiptPayroll] Loaded {PayrollRecords.Count} payroll records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReceiptPayroll] Load error: {ex.Message}");
                ErrorMessage = "Error loading payroll records.";
            }
        }

        private void CreatePayrollRecord(MySqlConnection connection)
        {
            NetSalary = BaseSalary + Bonus - Deductions;
            
            string query = @"INSERT INTO payroll 
                           (employee_id, employee_name, base_salary, bonus, deductions, net_salary, month, year, status, created_at) 
                           VALUES (@EmployeeId, @EmployeeName, @BaseSalary, @Bonus, @Deductions, @NetSalary, @Month, @Year, @Status, NOW())";
            
            connection.Execute(query, new { 
                EmployeeId, 
                EmployeeName, 
                BaseSalary, 
                Bonus, 
                Deductions, 
                NetSalary, 
                Month, 
                Year, 
                Status 
            });
            
            Console.WriteLine($"[ReceiptPayroll] Created: {EmployeeName} - {NetSalary}");
        }

        private void UpdatePayrollRecord(MySqlConnection connection)
        {
            NetSalary = BaseSalary + Bonus - Deductions;
            
            string query = @"UPDATE payroll 
                           SET employee_id = @EmployeeId, employee_name = @EmployeeName, base_salary = @BaseSalary,
                               bonus = @Bonus, deductions = @Deductions, net_salary = @NetSalary,
                               month = @Month, year = @Year, status = @Status, updated_at = NOW()
                           WHERE id = @Id";
            
            connection.Execute(query, new { 
                EmployeeId, 
                EmployeeName, 
                BaseSalary, 
                Bonus, 
                Deductions, 
                NetSalary, 
                Month, 
                Year, 
                Status, 
                Id 
            });
            
            Console.WriteLine($"[ReceiptPayroll] Updated ID: {Id}");
        }

        private void DeletePayrollRecord(MySqlConnection connection)
        {
            string query = "UPDATE payroll SET status = 'Cancelled', deleted_at = NOW() WHERE id = @Id";
            connection.Execute(query, new { Id });
            Console.WriteLine($"[ReceiptPayroll] Deleted ID: {Id}");
        }
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public string Month { get; set; } = "";
        public int Year { get; set; }
        public string Status { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
    }
}