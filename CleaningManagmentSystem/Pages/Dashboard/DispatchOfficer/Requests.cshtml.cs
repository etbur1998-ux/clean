using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    [Authorize(Roles = "dispatch_officer,superadmin")]
    public class RequestsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<RequestViewModel> PendingRequests { get; set; } = new List<RequestViewModel>();

        public RequestsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            using var connection = new MySqlConnection(connectionString);
            
            string query = @"
                SELECT r.id, r.request_type as RequestType, r.urgency as Urgency, r.execution_status as ExecutionStatus, 
                       u.name as RequestorName, r.requestor_role as RequestorRole, r.created_at as CreatedAt
                FROM requests r
                LEFT JOIN users u ON r.requestor_id = u.id
                WHERE r.is_closed = FALSE 
                ORDER BY r.created_at DESC";
            
            var requests = await connection.QueryAsync<RequestViewModel>(query);
            PendingRequests = requests.ToList();
        }

        public class RequestViewModel
        {
            public int Id { get; set; }
            public string RequestType { get; set; } = "";
            public string Urgency { get; set; } = "";
            public string ExecutionStatus { get; set; } = "";
            public string RequestorName { get; set; } = "";
            public string RequestorRole { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}
