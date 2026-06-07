using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;

namespace CleaningManagmentSystem.Pages.Dashboard.Manager
{
    public class OutsourceCompanyModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string CompanyName { get; set; } = "";

        [BindProperty]
        public string ContactPerson { get; set; } = "";

        [BindProperty]
        public string Phone { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string LicenseNumber { get; set; } = "";

        [BindProperty]
        public DateTime? ContractStartDate { get; set; }

        [BindProperty]
        public DateTime? ContractEndDate { get; set; }

        [BindProperty]
        public string Status { get; set; } = "Active";

        [BindProperty]
        public string ServicesProvided { get; set; } = "";

        [BindProperty]
        public string SearchQuery { get; set; } = "";

        public List<OutsourceCompany> Companies { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public OutsourceCompanyModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[OutsourceCompany] OnGet called");

            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("[OutsourceCompany] No session, redirecting to Login");
                return RedirectToPage("/Login");
            }

            LoadCompanies();
            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine($"[OutsourceCompany] OnPost called with action");

            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }

            var action = Request.Form["action"];

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                if (action == "create")
                {
                    CreateCompany(connection);
                    SuccessMessage = "Company created successfully!";
                }
                else if (action == "update")
                {
                    UpdateCompany(connection);
                    SuccessMessage = "Company updated successfully!";
                }
                else if (action == "delete")
                {
                    DeleteCompany(connection);
                    SuccessMessage = "Company deleted successfully!";
                }
                else if (action == "toggle")
                {
                    ToggleStatus(connection);
                    SuccessMessage = "Status updated successfully!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OutsourceCompany] Database error: {ex.Message}");
                ErrorMessage = $"Error: {ex.Message}";
            }

            LoadCompanies();
            return Page();
        }

        private void LoadCompanies()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = @"SELECT 
                                    id AS Id,
                                    company_name AS CompanyName,
                                    contact_person AS ContactPerson,
                                    phone AS Phone,
                                    email AS Email,
                                    license_number AS LicenseNumber,
                                    contract_start_date AS ContractStartDate,
                                    contract_end_date AS ContractEndDate,
                                    status AS Status,
                                    services_provided AS ServicesProvided,
                                    created_at AS CreatedAt,
                                    updated_at AS UpdatedAt
                                FROM outsource_companies 
                                ORDER BY id DESC";

                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = @"SELECT 
                                 id AS Id,
                                 company_name AS CompanyName,
                                 contact_person AS ContactPerson,
                                 phone AS Phone,
                                 email AS Email,
                                 license_number AS LicenseNumber,
                                 contract_start_date AS ContractStartDate,
                                 contract_end_date AS ContractEndDate,
                                 status AS Status,
                                 services_provided AS ServicesProvided,
                                 created_at AS CreatedAt,
                                 updated_at AS UpdatedAt
                              FROM outsource_companies 
                              WHERE company_name LIKE @Search
                                 OR contact_person LIKE @Search
                                 OR phone LIKE @Search
                                 OR email LIKE @Search
                              ORDER BY id DESC";
                    Companies = connection.Query<OutsourceCompany>(query, new { Search = $"%{SearchQuery}%" }).ToList();
                }
                else
                {
                    Companies = connection.Query<OutsourceCompany>(query).ToList();
                }

                Console.WriteLine($"[OutsourceCompany] Loaded {Companies.Count} companies");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OutsourceCompany] Load error: {ex.Message}");
                ErrorMessage = "Error loading companies.";
            }
        }

        private void CreateCompany(MySqlConnection connection)
        {
            string query = @"INSERT INTO outsource_companies
                            (company_name, contact_person, phone, email, license_number, contract_start_date, contract_end_date, status, services_provided, created_at, updated_at)
                            VALUES
                            (@CompanyName, @ContactPerson, @Phone, @Email, @LicenseNumber, @ContractStartDate, @ContractEndDate, @Status, @ServicesProvided, NOW(), NOW())";

            connection.Execute(query, new
            {
                CompanyName,
                ContactPerson,
                Phone,
                Email,
                LicenseNumber,
                ContractStartDate,
                ContractEndDate,
                Status,
                ServicesProvided
            });

            Console.WriteLine($"[OutsourceCompany] Created: {CompanyName}");
        }

        private void UpdateCompany(MySqlConnection connection)
        {
            string query = @"UPDATE outsource_companies
                            SET company_name = @CompanyName,
                                contact_person = @ContactPerson,
                                phone = @Phone,
                                email = @Email,
                                license_number = @LicenseNumber,
                                contract_start_date = @ContractStartDate,
                                contract_end_date = @ContractEndDate,
                                status = @Status,
                                services_provided = @ServicesProvided,
                                updated_at = NOW()
                            WHERE id = @Id";

            connection.Execute(query, new
            {
                Id,
                CompanyName,
                ContactPerson,
                Phone,
                Email,
                LicenseNumber,
                ContractStartDate,
                ContractEndDate,
                Status,
                ServicesProvided
            });

            Console.WriteLine($"[OutsourceCompany] Updated ID: {Id}");
        }

        private void DeleteCompany(MySqlConnection connection)
        {
            // Using soft delete - mark as inactive
            string query = "UPDATE outsource_companies SET status = 'Inactive', updated_at = NOW() WHERE id = @Id";
            connection.Execute(query, new { Id });
            Console.WriteLine($"[OutsourceCompany] Deleted (soft) ID: {Id}");
        }

        private void ToggleStatus(MySqlConnection connection)
        {
            // Get current status
            var currentStatus = connection.QueryFirstOrDefault<string>(
                "SELECT status FROM outsource_companies WHERE id = @Id",
                new { Id });

            if (currentStatus == null)
            {
                throw new Exception("Company not found");
            }

            string newStatus = currentStatus == "Active" ? "Inactive" : "Active";
            string query = "UPDATE outsource_companies SET status = @Status, updated_at = NOW() WHERE id = @Id";
            connection.Execute(query, new { Status = newStatus, Id });
            Console.WriteLine($"[OutsourceCompany] Toggled status for ID: {Id} to {newStatus}");
        }
    }

    public class OutsourceCompany
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string Status { get; set; } = "Active";
        public string ServicesProvided { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
