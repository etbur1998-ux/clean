using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly string _connectionString;

        public RequestController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        // POST: api/request
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] RequestModel req)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO requests (
                        requestor_id, requestor_role, wereda_id, mahberat_id, request_type, 
                        description, quantity, urgency, requested_date_time, attachment_path, 
                        execution_status, is_closed
                    ) VALUES (
                        @RequestorId, @RequestorRole, @WeredaId, @MahberatId, @RequestType, 
                        @Description, @Quantity, @Urgency, @RequestedDateTime, @AttachmentPath, 
                        'Pending', FALSE
                    );
                    SELECT LAST_INSERT_ID();";

                var requestId = await connection.ExecuteScalarAsync<int>(sql, req);

                // Create associated approval tracking record
                var approvalSql = @"
                    INSERT INTO request_approvals (
                        request_id, level_1_status, level_2_status, level_3_status
                    ) VALUES (
                        @RequestId, 'Pending', 'Pending', 'Pending'
                    )";
                
                await connection.ExecuteAsync(approvalSql, new { RequestId = requestId });

                return Ok(new { success = true, requestId = requestId, message = "Request created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/request/pending
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = "SELECT * FROM requests WHERE is_closed = FALSE ORDER BY created_at DESC";
                var requests = await connection.QueryAsync<RequestModel>(sql);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/request/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"
                    SELECT r.*, 
                           u.name as RequestorName,
                           a.level_1_status, a.level_1_date, a.level_1_comments,
                           a.level_2_status, a.level_2_date, a.level_2_comments,
                           a.level_3_status, a.level_3_date, a.level_3_comments
                    FROM requests r
                    LEFT JOIN users u ON r.requestor_id = u.id
                    LEFT JOIN request_approvals a ON r.id = a.request_id
                    WHERE r.id = @Id";
                var req = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
                if (req == null) return NotFound();
                return Ok(req);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/request/{id}/approve
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveRequest(int id, [FromBody] ApprovalDto approval)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                string updateSql = "";
                if (approval.Level == 1)
                {
                    updateSql = "UPDATE request_approvals SET level_1_status = 'Approved', level_1_by = @UserId, level_1_date = NOW(), level_1_comments = @Comments WHERE request_id = @RequestId";
                }
                else if (approval.Level == 2)
                {
                    updateSql = "UPDATE request_approvals SET level_2_status = 'Approved', level_2_by = @UserId, level_2_date = NOW(), level_2_comments = @Comments WHERE request_id = @RequestId";
                }
                else if (approval.Level == 3)
                {
                    updateSql = "UPDATE request_approvals SET level_3_status = 'Approved', level_3_by = @UserId, level_3_date = NOW(), level_3_comments = @Comments WHERE request_id = @RequestId";
                    // If level 3 approved, we might also auto-assign or change request status
                }

                if (!string.IsNullOrEmpty(updateSql))
                {
                    await connection.ExecuteAsync(updateSql, new { RequestId = id, UserId = approval.UserId, Comments = approval.Comments });
                    return Ok(new { success = true, message = $"Level {approval.Level} approved successfully." });
                }
                
                return BadRequest(new { success = false, message = "Invalid approval level." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class ApprovalDto
    {
        public int Level { get; set; } // 1, 2, or 3
        public int UserId { get; set; }
        public string Comments { get; set; } = "";
    }
}
