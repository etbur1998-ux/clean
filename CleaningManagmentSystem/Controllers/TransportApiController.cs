using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
using System.Data;

namespace CleaningManagmentSystem.Controllers
{
    /// <summary>
    /// REST API for the Mahberat Transport Request & Payment Workflow.
    /// Base route: /api/transport
    /// </summary>
    [Route("api/transport")]
    [ApiController]
    public class TransportApiController : ControllerBase
    {
        private readonly string _connectionString;

        public TransportApiController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

        // ─── Helper: generate request number ────────────────────────────────
        private static string GenerateRequestNumber()
            => $"TR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        // ─── Helper: insert notification ────────────────────────────────────
        private async Task NotifyAsync(IDbConnection conn, int recipientId, int requestId,
            string requestNumber, string title, string body, string type = "Info")
        {
            await conn.ExecuteAsync(
                @"INSERT INTO transport_notifications
                  (recipient_user_id, transport_request_id, request_number, title, body, notification_type)
                  VALUES (@RecipientId, @RequestId, @RequestNumber, @Title, @Body, @Type)",
                new { RecipientId = recipientId, RequestId = requestId,
                      RequestNumber = requestNumber, Title = title, Body = body, Type = type });
        }

        // ─── Helper: log status change ───────────────────────────────────────
        private async Task LogStatusAsync(IDbConnection conn, int requestId,
            string fromStatus, string toStatus, int actorId, string actorName,
            string actorRole, string notes = "")
        {
            await conn.ExecuteAsync(
                @"INSERT INTO transport_request_logs
                  (transport_request_id, from_status, to_status, actor_user_id, actor_name, actor_role, notes)
                  VALUES (@RequestId, @From, @To, @ActorId, @ActorName, @ActorRole, @Notes)",
                new { RequestId = requestId, From = fromStatus, To = toStatus,
                      ActorId = actorId, ActorName = actorName, ActorRole = actorRole, Notes = notes });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 1 — Mahberat User: Create Transport Request
        // POST /api/transport/requests
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateTransportRequestDto dto)
        {
            using var conn = CreateConnection();
            var requestNumber = GenerateRequestNumber();

            var mahberatName = dto.MahberatId.HasValue
                ? await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM mahberats WHERE id = @Id", new { Id = dto.MahberatId })
                : null;

            var id = await conn.QueryFirstAsync<int>(
                @"INSERT INTO transport_requests
                  (request_number, mahberat_user_id, mahberat_user_name, mahberat_id, mahberat_name,
                   pickup_location, destination, passenger_item_details,
                   requested_date, requested_time, special_instructions, status)
                  VALUES
                  (@RequestNumber, @UserId, @UserName, @MahberatId, @MahberatName,
                   @Pickup, @Destination, @Details,
                   @Date, @Time, @Instructions, 'PendingDispatcher');
                  SELECT LAST_INSERT_ID();",
                new
                {
                    RequestNumber = requestNumber,
                    UserId = dto.UserId, UserName = dto.UserName,
                    MahberatId = dto.MahberatId, MahberatName = mahberatName,
                    Pickup = dto.PickupLocation, Destination = dto.Destination,
                    Details = dto.PassengerItemDetails,
                    Date = dto.RequestedDate, Time = dto.RequestedTime,
                    Instructions = dto.SpecialInstructions
                });

            await LogStatusAsync(conn, id, "", "PendingDispatcher", dto.UserId, dto.UserName, "MahberatUser");

            // Notify all dispatchers
            var dispatchers = await conn.QueryAsync<int>(
                "SELECT id FROM users WHERE role = 'DispatchOfficer' AND is_active = TRUE");
            foreach (var dispId in dispatchers)
                await NotifyAsync(conn, dispId, id, requestNumber,
                    "New Transport Request", $"Request {requestNumber} from {dto.UserName} needs review.", "Action");

            return Ok(new { success = true, requestId = id, requestNumber });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 2 — Dispatcher: Approve/Reject + Assign Driver
        // POST /api/transport/requests/{id}/dispatcher-action
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/dispatcher-action")]
        public async Task<IActionResult> DispatcherAction(int id, [FromBody] DispatcherActionDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, mahberat_user_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            if (req.status != "PendingDispatcher") return BadRequest(new { message = "Request is not pending dispatcher review." });

            string newStatus;
            if (dto.Action == "Approve")
            {
                if (!dto.DriverId.HasValue) return BadRequest(new { message = "DriverId is required for approval." });
                var driverName = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM users WHERE id = @Id", new { Id = dto.DriverId });
                var vehiclePlate = dto.VehicleId.HasValue
                    ? await conn.QueryFirstOrDefaultAsync<string>(
                        "SELECT plate_number FROM vehicles WHERE id = @Id", new { Id = dto.VehicleId })
                    : null;

                newStatus = "DriverAssigned";
                await conn.ExecuteAsync(
                    @"UPDATE transport_requests SET
                        status = @Status, dispatcher_id = @DispId, dispatcher_name = @DispName,
                        dispatcher_notes = @Notes, dispatcher_action_at = NOW(),
                        driver_id = @DriverId, driver_name = @DriverName,
                        vehicle_id = @VehicleId, vehicle_plate = @VehiclePlate
                      WHERE id = @Id",
                    new { Status = newStatus, DispId = dto.DispatcherId, DispName = dto.DispatcherName,
                          Notes = dto.Notes, DriverId = dto.DriverId, DriverName = driverName,
                          VehicleId = dto.VehicleId, VehiclePlate = vehiclePlate, Id = id });

                await NotifyAsync(conn, dto.DriverId.Value, id, (string)req.request_number,
                    "New Trip Assigned", $"You have been assigned trip {req.request_number}. Please accept or reject.", "Action");
                await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                    "Request Approved", $"Your transport request {req.request_number} has been approved. Driver is being assigned.", "Success");
            }
            else
            {
                newStatus = "DispatcherRejected";
                await conn.ExecuteAsync(
                    @"UPDATE transport_requests SET
                        status = @Status, dispatcher_id = @DispId, dispatcher_name = @DispName,
                        dispatcher_notes = @Notes, dispatcher_action_at = NOW()
                      WHERE id = @Id",
                    new { Status = newStatus, DispId = dto.DispatcherId, DispName = dto.DispatcherName,
                          Notes = dto.Notes, Id = id });

                await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                    "Request Rejected", $"Your transport request {req.request_number} was rejected. Reason: {dto.Notes}", "Warning");
            }

            await LogStatusAsync(conn, id, "PendingDispatcher", newStatus, dto.DispatcherId, dto.DispatcherName, "DispatchOfficer", dto.Notes ?? "");
            return Ok(new { success = true, newStatus });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 3 — Driver: Accept/Reject Trip
        // POST /api/transport/requests/{id}/driver-action
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/driver-action")]
        public async Task<IActionResult> DriverAction(int id, [FromBody] DriverActionDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, mahberat_user_id, dispatcher_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            if (req.status != "DriverAssigned" && req.status != "DriverAccepted")
                return BadRequest(new { message = "Trip is not in DriverAssigned status." });
            // Idempotent — already accepted
            if ((string)req.status == "DriverAccepted" && dto.Action == "Accept")
                return Ok(new { success = true, newStatus = "DriverAccepted" });

            string newStatus;
            if (dto.Action == "Accept")
            {
                newStatus = "DriverAccepted";
                await conn.ExecuteAsync(
                    @"UPDATE transport_requests SET status = @Status, driver_notes = @Notes, driver_action_at = NOW() WHERE id = @Id",
                    new { Status = newStatus, Notes = dto.Notes, Id = id });

                await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                    "Driver Accepted", $"Driver has accepted your trip {req.request_number}. They are on their way.", "Success");
            }
            else
            {
                newStatus = "PendingDispatcher";
                await conn.ExecuteAsync(
                    @"UPDATE transport_requests SET status = @Status, driver_id = NULL, driver_name = NULL,
                        vehicle_id = NULL, vehicle_plate = NULL, driver_notes = @Notes, driver_action_at = NOW()
                      WHERE id = @Id",
                    new { Status = newStatus, Notes = dto.Notes, Id = id });

                if (req.dispatcher_id != null)
                    await NotifyAsync(conn, (int)req.dispatcher_id, id, (string)req.request_number,
                        "Driver Rejected Trip", $"Driver rejected trip {req.request_number}. Please assign another driver.", "Warning");
            }

            await LogStatusAsync(conn, id, "DriverAssigned", newStatus, dto.DriverId, dto.DriverName, "Driver", dto.Notes ?? "");
            return Ok(new { success = true, newStatus });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 4 — Driver: Mark as Picked Up
        // POST /api/transport/requests/{id}/pickup
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/pickup")]
        public async Task<IActionResult> MarkPickedUp(int id, [FromBody] PickupDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, mahberat_user_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            if (req.status != "DriverAccepted") return BadRequest(new { message = "Trip must be in DriverAccepted status." });

            await conn.ExecuteAsync(
                @"UPDATE transport_requests SET status = 'PickedUp', pickup_confirmed_at = NOW(), pickup_notes = @Notes WHERE id = @Id",
                new { Notes = dto.Notes, Id = id });

            await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                "Pickup Confirmation", $"Driver has arrived and picked up for trip {req.request_number}. Please confirm.", "Action");

            await LogStatusAsync(conn, id, "DriverAccepted", "PickedUp", dto.DriverId, dto.DriverName, "Driver", dto.Notes ?? "");
            return Ok(new { success = true, newStatus = "PickedUp" });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 5 — Mahberat: Approve/Reject Pickup
        // POST /api/transport/requests/{id}/mahberat-pickup
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/mahberat-pickup")]
        public async Task<IActionResult> MahberatPickupApproval(int id, [FromBody] MahberatPickupDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, driver_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            if (req.status != "PickedUp") return BadRequest(new { message = "Request must be in PickedUp status." });

            string newStatus = dto.Action == "Approve" ? "MahberatApprovedPickup" : "PickedUp";
            await conn.ExecuteAsync(
                @"UPDATE transport_requests SET status = @Status, mahberat_pickup_approved_at = NOW(), mahberat_pickup_notes = @Notes WHERE id = @Id",
                new { Status = newStatus, Notes = dto.Notes, Id = id });

            if (dto.Action == "Approve" && req.driver_id != null)
                await NotifyAsync(conn, (int)req.driver_id, id, (string)req.request_number,
                    "Pickup Approved", $"Mahberat confirmed pickup for trip {req.request_number}. Proceed with the trip.", "Success");

            await LogStatusAsync(conn, id, "PickedUp", newStatus, dto.UserId, dto.UserName, "MahberatUser", dto.Notes ?? "");
            return Ok(new { success = true, newStatus });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 6 — Driver: Submit Receipt
        // POST /api/transport/requests/{id}/submit-receipt
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/submit-receipt")]
        public async Task<IActionResult> SubmitReceipt(int id, [FromBody] SubmitReceiptDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, mahberat_user_id, mahberat_id, mahberat_name, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            // Accept re-submission (ReceiptSubmitted) and all active pickup statuses
            var submitAllowed = new[] { "PickedUp", "MahberatApprovedPickup", "DriverAccepted", "ReceiptSubmitted" };
            if (!Array.Exists(submitAllowed, s => s == (string)req.status))
                return BadRequest(new { message = $"Cannot submit receipt from status: {req.status}" });

            // ── Resolve Wereda / Mahberat names ──────────────────────────
            int? weredaId   = dto.WeredaId;
            int? mahberatId = dto.MahberatId ?? (int?)req.mahberat_id;

            string? weredaName = weredaId.HasValue
                ? await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM weredas WHERE id = @Id", new { Id = weredaId })
                : null;

            string? mahberatName = mahberatId.HasValue
                ? await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM mahberats WHERE id = @Id", new { Id = mahberatId })
                : (string?)req.mahberat_name;

            // ── Resolve driver vehicle ────────────────────────────────────
            string? plateNumber = null;
            int?    vehicleId   = null;
            if (dto.DriverId > 0)
            {
                var vehicle = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id, plate_number FROM vehicles WHERE driver_id = @DriverId LIMIT 1",
                    new { DriverId = dto.DriverId });
                if (vehicle != null)
                {
                    vehicleId   = (int?)vehicle.id;
                    plateNumber = (string?)vehicle.plate_number;
                }
            }

            // ── Get default rate from system_settings ─────────────────────
            decimal rate = 0m;
            try
            {
                var rateStr = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultTransportRatePerKg'");
                if (rateStr != null) decimal.TryParse(rateStr, out rate);
                if (rate == 0)
                {
                    var fallback = await conn.QueryFirstOrDefaultAsync<string>(
                        "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");
                    if (fallback != null) decimal.TryParse(fallback, out rate);
                }
            }
            catch { /* table may not exist yet, use 0 */ }

            decimal kg    = dto.ActualKilogram ?? 0m;
            decimal total = rate > 0 ? kg * rate : 0m;

            // ── Update transport_requests ─────────────────────────────────
            await conn.ExecuteAsync(
                @"UPDATE transport_requests SET
                    status = 'ReceiptSubmitted', receipt_photo_url = @PhotoUrl,
                    digital_receipt_url = @DigitalUrl, receipt_notes = @Notes,
                    receipt_submitted_at = NOW(),
                    actual_kilogram = @Kg, receipt_wereda_id = @WeredaId,
                    receipt_mahberat_id = @MahberatId,
                    transport_cost = COALESCE(NULLIF(@Total, 0), transport_cost)
                  WHERE id = @Id",
                new { PhotoUrl = dto.ReceiptPhotoUrl, DigitalUrl = dto.DigitalReceiptUrl,
                      Notes = dto.Notes, Kg = kg, Total = total,
                      WeredaId = weredaId, MahberatId = mahberatId, Id = id });

            // ── Auto-insert into staff_receipts so Mahberat approval queue picks it up ──
            try
            {
                // Avoid duplicate if re-submitted
                var existing = await conn.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM staff_receipts WHERE transport_request_id = @TrId",
                    new { TrId = id });

                if (existing == 0)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO staff_receipts
                            (wereda_id, wereda_name, mahberat_id, mahberat_name,
                             vehicle_id, plate_number, driver_id, driver_name,
                             receipt_time, receipt_date, kilogram, price,
                             registered_by, status, notes, image_url,
                             transport_request_id, registered_at)
                          VALUES
                            (@WeredaId, @WeredaName, @MahberatId, @MahberatName,
                             @VehicleId, @PlateNumber, @DriverId, @DriverName,
                             CURTIME(), CURDATE(), @Kg, @Total,
                             @RegisteredBy, 'Pending', @Notes, @ImageUrl,
                             @TrId, NOW())",
                        new
                        {
                            WeredaId    = (object?)weredaId   ?? DBNull.Value,
                            WeredaName  = weredaName  ?? "",
                            MahberatId  = (object?)mahberatId ?? DBNull.Value,
                            MahberatName = mahberatName ?? "",
                            VehicleId   = (object?)vehicleId  ?? DBNull.Value,
                            PlateNumber = plateNumber ?? "",
                            DriverId    = dto.DriverId > 0 ? (object)dto.DriverId : DBNull.Value,
                            DriverName  = dto.DriverName ?? "",
                            Kg          = kg,
                            Total       = total,   // kg × rate
                            RegisteredBy = dto.DriverName ?? "Driver",
                            Notes       = $"[Transport {(string)req.request_number}] {dto.Notes ?? ""}".Trim(),
                            ImageUrl    = dto.ReceiptPhotoUrl ?? "",
                            TrId        = id
                        });
                }
                else
                {
                    // Re-submission: update KG, recalculate total, reset approval state
                    await conn.ExecuteAsync(
                        @"UPDATE staff_receipts SET
                            kilogram = @Kg, price = @Total, image_url = @ImageUrl,
                            notes = @Notes, mahberat_approved = NULL,
                            mahberat_approved_by = NULL, mahberat_approved_at = NULL,
                            mahberat_notes = NULL, status = 'Pending'
                          WHERE transport_request_id = @TrId",
                        new
                        {
                            Kg       = kg,
                            Total    = total,   // kg × rate
                            ImageUrl = dto.ReceiptPhotoUrl ?? "",
                            Notes    = $"[Transport {(string)req.request_number}] {dto.Notes ?? ""}".Trim(),
                            TrId     = id
                        });
                }
            }
            catch (Exception ex)
            {
                // Column transport_request_id may not exist yet — log and continue
                Console.WriteLine($"[TransportAPI] staff_receipts insert warning: {ex.Message}");
            }

            await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                "Receipt Submitted", $"Driver submitted receipt for trip {req.request_number}. Please verify in Receipt Approvals.", "Action");

            await LogStatusAsync(conn, id, (string)req.status, "ReceiptSubmitted", dto.DriverId, dto.DriverName, "Driver", dto.Notes ?? "");
            return Ok(new { success = true, newStatus = "ReceiptSubmitted" });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 7 — Mahberat: Verify Receipt → feeds existing Staff Approvals queue
        // POST /api/transport/requests/{id}/mahberat-verify
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/mahberat-verify")]
        public async Task<IActionResult> MahberatVerifyReceipt(int id, [FromBody] MahberatVerifyDto? dto)
        {
            if (dto == null) return BadRequest(new { message = "Request body is required." });
            if (string.IsNullOrEmpty(dto.Action)) dto.Action = "Verify"; // default to Verify
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, driver_id, mahberat_user_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();

            // Allow approval from any active status after pickup
            var allowedStatuses = new[]
            {
                "PickedUp", "MahberatApprovedPickup",
                "ReceiptSubmitted", "ReceiptVerified", "MahberatVerified"
            };
            if (!Array.Exists(allowedStatuses, s => s == (string)req.status))
                return BadRequest(new { message = $"Cannot approve from current status: {req.status}" });

            // Idempotent — already approved, just return success
            if ((string)req.status == "MahberatVerified" && dto.Action == "Verify")
                return Ok(new { success = true, newStatus = "MahberatVerified",
                    message = "Already approved by Mahberat." });

            if (dto.Action == "Verify")
            {
                // ── Mark Mahberat Level-1 approval on the linked staff_receipts row ──
                // This makes it appear in Staff Approvals (existing flow).
                // transport_request stays ReceiptSubmitted until Staff approves it.
                try
                {
                    var srId = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT id FROM staff_receipts WHERE transport_request_id = @TrId LIMIT 1",
                        new { TrId = id });

                    if (srId.HasValue && srId.Value > 0)
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE staff_receipts
                              SET mahberat_approved    = 1,
                                  mahberat_approved_by = @By,
                                  mahberat_approved_at = NOW(),
                                  mahberat_notes       = @Notes
                              WHERE id = @Id",
                            new { By = dto.UserName, Notes = dto.Notes ?? "", Id = srId.Value });
                    }
                    else
                    {
                        // No staff_receipts row yet — create one now so it enters the queue
                        var tr = await conn.QueryFirstOrDefaultAsync<dynamic>(
                            "SELECT * FROM transport_requests WHERE id = @Id", new { Id = id });

                        if (tr != null)
                        {
                            // Resolve names
                            string? weredaName = tr.receipt_wereda_id != null
                                ? await conn.QueryFirstOrDefaultAsync<string>(
                                    "SELECT name FROM weredas WHERE id = @Id", new { Id = (int)tr.receipt_wereda_id })
                                : null;
                            string? mahberatName = tr.receipt_mahberat_id != null
                                ? await conn.QueryFirstOrDefaultAsync<string>(
                                    "SELECT name FROM mahberats WHERE id = @Id", new { Id = (int)tr.receipt_mahberat_id })
                                : (string?)tr.mahberat_name;

                            // Rate from settings
                            decimal rate = 0m;
                            try
                            {
                                var rv = await conn.QueryFirstOrDefaultAsync<string>(
                                    "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultTransportRatePerKg'");
                                if (rv == null) rv = await conn.QueryFirstOrDefaultAsync<string>(
                                    "SELECT setting_value FROM system_settings WHERE setting_key = 'DefaultPricePerKg'");
                                if (rv != null) decimal.TryParse(rv, out rate);
                            }
                            catch { }

                            string? plateNumber = null;
                            int?    vehicleId   = null;
                            if (tr.driver_id != null)
                            {
                                var v = await conn.QueryFirstOrDefaultAsync<dynamic>(
                                    "SELECT id, plate_number FROM vehicles WHERE driver_id = @DId LIMIT 1",
                                    new { DId = (int)tr.driver_id });
                                if (v != null) { vehicleId = (int?)v.id; plateNumber = (string?)v.plate_number; }
                            }

                            await conn.ExecuteAsync(
                                @"INSERT INTO staff_receipts
                                    (wereda_id, wereda_name, mahberat_id, mahberat_name,
                                     vehicle_id, plate_number, driver_id, driver_name,
                                     receipt_time, receipt_date, kilogram, price,
                                     registered_by, status, notes, image_url,
                                     transport_request_id, registered_at,
                                     mahberat_approved, mahberat_approved_by,
                                     mahberat_approved_at, mahberat_notes)
                                  VALUES
                                    (@WeredaId, @WeredaName, @MahberatId, @MahberatName,
                                     @VehicleId, @PlateNumber, @DriverId, @DriverName,
                                     CURTIME(), CURDATE(), @Kg, @Rate,
                                     'TransportRequest', 'Pending', @Notes, @ImageUrl,
                                     @TrId, NOW(),
                                     1, @By, NOW(), @Notes)",
                                new
                                {
                                    WeredaId     = (object?)(int?)tr.receipt_wereda_id   ?? DBNull.Value,
                                    WeredaName   = weredaName   ?? "",
                                    MahberatId   = (object?)(int?)tr.receipt_mahberat_id ?? DBNull.Value,
                                    MahberatName = mahberatName ?? "",
                                    VehicleId    = (object?)vehicleId  ?? DBNull.Value,
                                    PlateNumber  = plateNumber ?? "",
                                    DriverId     = tr.driver_id  ?? (object)DBNull.Value,
                                    DriverName   = (string?)tr.driver_name ?? "",
                                    Kg           = tr.actual_kilogram ?? 0m,
                                    Rate         = rate,
                                    Notes        = $"[TR {(string)tr.request_number}] {dto.Notes ?? ""}".Trim(),
                                    ImageUrl     = (string?)tr.receipt_photo_url ?? "",
                                    TrId         = id,
                                    By           = dto.UserName
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[mahberat-verify] staff_receipts update warning: {ex.Message}");
                }

                // Advance transport_request to MahberatVerified so UI reflects the change
                try
                {
                    await conn.ExecuteAsync(
                        @"UPDATE transport_requests
                          SET status = 'MahberatVerified',
                              mahberat_verification_notes = @Notes
                          WHERE id = @Id",
                        new { Notes = dto.Notes ?? "", Id = id });
                }
                catch
                {
                    // Fallback if column missing
                    await conn.ExecuteAsync(
                        "UPDATE transport_requests SET status = 'MahberatVerified' WHERE id = @Id",
                        new { Id = id });
                }

                // Notify Staff
                var staffList = await conn.QueryAsync<int>(
                    "SELECT id FROM users WHERE role IN ('Staff','Finance') AND is_active = TRUE");
                foreach (var sId in staffList)
                    await NotifyAsync(conn, sId, id, (string)req.request_number,
                        "Receipt Awaiting Staff Approval",
                        $"Trip {req.request_number} receipt approved by Mahberat. Needs staff approval.",
                        "Action");

                await LogStatusAsync(conn, id, "ReceiptSubmitted", "MahberatVerified",
                    dto.UserId, dto.UserName, "MahberatUser",
                    $"Mahberat Level-1 approved. Forwarded to Staff. {dto.Notes ?? ""}");

                return Ok(new { success = true, newStatus = "MahberatVerified",
                    message = "Approved — forwarded to Staff Approvals queue." });
            }
            else
            {
                // Correction requested — reset mahberat_approved on staff_receipts row
                try
                {
                    await conn.ExecuteAsync(
                        @"UPDATE staff_receipts
                          SET mahberat_approved = NULL,
                              mahberat_approved_by = NULL,
                              mahberat_approved_at = NULL,
                              mahberat_notes = @Notes
                          WHERE transport_request_id = @TrId",
                        new { Notes = dto.Notes ?? "", TrId = id });
                }
                catch { }

                if (req.driver_id != null)
                    await NotifyAsync(conn, (int)req.driver_id, id, (string)req.request_number,
                        "Receipt Correction Requested",
                        $"Mahberat requested correction for trip {req.request_number}. Notes: {dto.Notes}",
                        "Warning");

                await LogStatusAsync(conn, id, "ReceiptSubmitted", "ReceiptSubmitted",
                    dto.UserId, dto.UserName, "MahberatUser", dto.Notes ?? "");

                return Ok(new { success = true, newStatus = "ReceiptSubmitted",
                    message = "Correction requested — driver notified." });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 8 — Staff: Approve/Reject Payment
        // POST /api/transport/requests/{id}/staff-action
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/staff-action")]
        public async Task<IActionResult> StaffAction(int id, [FromBody] StaffActionDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, mahberat_user_id, driver_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            if (req.status != "ReceiptVerified") return BadRequest(new { message = "Receipt must be verified first." });

            string newStatus = dto.Action == "Approve" ? "StaffApproved" : "StaffRejected";
            await conn.ExecuteAsync(
                @"UPDATE transport_requests SET
                    status = @Status, staff_id = @StaffId, staff_name = @StaffName,
                    transport_cost = @Cost, staff_notes = @Notes, staff_action_at = NOW()
                  WHERE id = @Id",
                new { Status = newStatus, StaffId = dto.StaffId, StaffName = dto.StaffName,
                      Cost = dto.TransportCost, Notes = dto.Notes, Id = id });

            if (dto.Action == "Approve")
            {
                var financeStaff = await conn.QueryAsync<int>(
                    "SELECT id FROM users WHERE role IN ('Staff','Finance') AND is_active = TRUE");
                foreach (var fId in financeStaff)
                    await NotifyAsync(conn, fId, id, (string)req.request_number,
                        "Payment Approved — Process Payment", $"Trip {req.request_number} approved. Cost: {dto.TransportCost}. Please process payment.", "Action");
            }
            else
            {
                if (req.mahberat_user_id != null)
                    await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                        "Payment Rejected", $"Payment for trip {req.request_number} was rejected. Reason: {dto.Notes}", "Warning");
            }

            await LogStatusAsync(conn, id, "ReceiptVerified", newStatus, dto.StaffId, dto.StaffName, "Staff", dto.Notes ?? "");
            return Ok(new { success = true, newStatus });
        }

        // ════════════════════════════════════════════════════════════════════
        // STEP 9 — Finance Staff: Process Payment
        // POST /api/transport/requests/{id}/process-payment
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("requests/{id}/process-payment")]
        public async Task<IActionResult> ProcessPayment(int id, [FromBody] ProcessPaymentDto dto)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, request_number, mahberat_user_id, driver_id, status FROM transport_requests WHERE id = @Id",
                new { Id = id });
            if (req == null) return NotFound();
            if (req.status != "StaffApproved") return BadRequest(new { message = "Payment must be staff-approved first." });

            await conn.ExecuteAsync(
                @"UPDATE transport_requests SET
                    status = 'Paid', finance_staff_id = @FinanceId, finance_staff_name = @FinanceName,
                    transaction_number = @TxNumber, payment_proof_url = @ProofUrl,
                    paid_at = NOW(), payment_notes = @Notes
                  WHERE id = @Id",
                new { FinanceId = dto.FinanceStaffId, FinanceName = dto.FinanceStaffName,
                      TxNumber = dto.TransactionNumber, ProofUrl = dto.PaymentProofUrl,
                      Notes = dto.Notes, Id = id });

            // ── Auto-register a staff_receipt from the transport request ──
            // Register it after Payment is Done.
            try
            {
                var tripFull = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM transport_requests WHERE id = @Id", new { Id = id });

                if (tripFull != null)
                {
                    string? weredaName = null;
                    string? mahberatName = null;
                    int? weredaId   = tripFull.receipt_wereda_id   ?? tripFull.wereda_id;
                    int? mahberatId = tripFull.receipt_mahberat_id ?? tripFull.mahberat_id;

                    if (weredaId.HasValue)
                        weredaName = await conn.QueryFirstOrDefaultAsync<string>(
                            "SELECT name FROM weredas WHERE id = @Id", new { Id = weredaId });
                    if (mahberatId.HasValue)
                        mahberatName = await conn.QueryFirstOrDefaultAsync<string>(
                            "SELECT name FROM mahberats WHERE id = @Id", new { Id = mahberatId });

                    string? driverName = null;
                    string? plateNumber = null;
                    int? vehicleId = null;
                    if (tripFull.driver_id != null)
                    {
                        driverName = await conn.QueryFirstOrDefaultAsync<string>(
                            "SELECT name FROM users WHERE id = @Id", new { Id = (int)tripFull.driver_id });
                        var vehicle = await conn.QueryFirstOrDefaultAsync<dynamic>(
                            "SELECT id, plate_number FROM vehicles WHERE driver_id = @DriverId",
                            new { DriverId = (int)tripFull.driver_id });
                        if (vehicle != null)
                        {
                            vehicleId   = vehicle.id;
                            plateNumber = vehicle.plate_number;
                        }
                    }

                    decimal kg = tripFull.actual_kilogram ?? 0m;
                    decimal cost = tripFull.transport_cost ?? 0m;
                    string  photoUrl = tripFull.receipt_photo_url ?? "";
                    string  notes    = $"Auto-registered from Transport Request {tripFull.request_number}. Tx: {dto.TransactionNumber}. {tripFull.receipt_notes ?? ""}".Trim();

                    var existing = await conn.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM staff_receipts WHERE notes LIKE @Pattern",
                        new { Pattern = $"%{tripFull.request_number}%" });

                    if (existing == 0)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO staff_receipts
                              (wereda_id, wereda_name, mahberat_id, mahberat_name,
                               vehicle_id, plate_number, driver_id, driver_name,
                               receipt_time, receipt_date, kilogram, price,
                               registered_by, status, notes, image_url,
                               mahberat_approved, mahberat_approved_by, mahberat_approved_at, mahberat_notes,
                               registered_at)
                              VALUES
                              (@WeredaId, @WeredaName, @MahberatId, @MahberatName,
                               @VehicleId, @PlateNumber, @DriverId, @DriverName,
                               CURTIME(), CURDATE(), @Kg, @Cost,
                               'TransportRequest', 'Approved', @Notes, @ImageUrl,
                               1, @MahberatUser, NOW(), @MahberatNotes,
                               NOW())",
                            new
                            {
                                WeredaId   = (object?)weredaId   ?? DBNull.Value,
                                WeredaName = weredaName   ?? "",
                                MahberatId = (object?)mahberatId ?? DBNull.Value,
                                MahberatName = mahberatName ?? "",
                                VehicleId  = (object?)vehicleId  ?? DBNull.Value,
                                PlateNumber = plateNumber ?? "",
                                DriverId   = tripFull.driver_id  ?? (object)DBNull.Value,
                                DriverName = driverName  ?? "",
                                Kg         = kg,
                                Cost       = cost,
                                Notes      = notes,
                                ImageUrl   = photoUrl,
                                MahberatUser  = tripFull.mahberat_user_name ?? "System",
                                MahberatNotes = "Verified by Mahberat and Paid by Staff"
                            });

                        Console.WriteLine($"[TransportAPI] Auto-registered staff_receipt for trip {tripFull.request_number}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransportAPI] Warning: Could not auto-register receipt: {ex.Message}");
            }

            if (req.mahberat_user_id != null)
                await NotifyAsync(conn, (int)req.mahberat_user_id, id, (string)req.request_number,
                    "Payment Processed", $"Payment for trip {req.request_number} has been processed. Tx: {dto.TransactionNumber}", "Success");
            if (req.driver_id != null)
                await NotifyAsync(conn, (int)req.driver_id, id, (string)req.request_number,
                    "Payment Processed", $"Payment for your trip {req.request_number} has been processed.", "Success");

            await LogStatusAsync(conn, id, "StaffApproved", "Paid", dto.FinanceStaffId, dto.FinanceStaffName, "Finance", dto.Notes ?? "");
            return Ok(new { success = true, newStatus = "Paid" });
        }

        // ════════════════════════════════════════════════════════════════════
        // QUERY ENDPOINTS
        // ════════════════════════════════════════════════════════════════════

        // GET /api/transport/requests?userId=&role=&status=
        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests([FromQuery] int? userId, [FromQuery] string? role, [FromQuery] string? status)
        {
            using var conn = CreateConnection();
            var where = new List<string>();
            var param = new DynamicParameters();

            if (!string.IsNullOrEmpty(status)) { where.Add("tr.status = @Status"); param.Add("Status", status); }

            if (userId.HasValue && !string.IsNullOrEmpty(role))
            {
                var r = role.ToLower();
                if (r == "mahberatuser" || r == "weredamahberat")
                    { where.Add("tr.mahberat_user_id = @UserId"); param.Add("UserId", userId); }
                else if (r == "driver")
                    { where.Add("tr.driver_id = @UserId"); param.Add("UserId", userId); }
                else if (r == "dispatchofficer")
                    { /* dispatcher sees all */ }
                else if (r == "staff" || r == "finance")
                    { /* staff sees all */ }
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $@"SELECT tr.id, tr.request_number, tr.mahberat_user_name,
                            COALESCE(tr.mahberat_name, m1.name, tr.mahberat_user_name) AS mahberat_name,
                            tr.pickup_location, tr.destination, tr.passenger_item_details,
                            tr.requested_date, tr.requested_time, tr.special_instructions,
                            tr.dispatcher_name, tr.driver_name, tr.vehicle_plate,
                            tr.transport_cost, tr.transaction_number, tr.status,
                            tr.actual_kilogram, tr.receipt_wereda_id, tr.receipt_mahberat_id,
                            COALESCE(m2.name, m1.name, tr.mahberat_name) AS receipt_mahberat_name,
                            w.name AS receipt_wereda_name,
                            tr.receipt_photo_url, tr.receipt_notes,
                            tr.created_at, tr.updated_at
                         FROM transport_requests tr
                         LEFT JOIN mahberats m1 ON m1.id = tr.mahberat_id
                         LEFT JOIN mahberats m2 ON m2.id = tr.receipt_mahberat_id
                         LEFT JOIN weredas   w  ON w.id  = tr.receipt_wereda_id
                         {whereClause}
                         ORDER BY tr.created_at DESC LIMIT 200";

            var results = await conn.QueryAsync(sql, param);
            return Ok(results);
        }

        // GET /api/transport/requests/{id}
        [HttpGet("requests/{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            using var conn = CreateConnection();
            var req = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM transport_requests WHERE id = @Id", new { Id = id });
            if (req == null) return NotFound();

            var logs = await conn.QueryAsync(
                "SELECT * FROM transport_request_logs WHERE transport_request_id = @Id ORDER BY created_at ASC",
                new { Id = id });

            // ── Resolve receipt wereda/mahberat names if only IDs are stored ──
            string? receiptWeredaName   = null;
            string? receiptMahberatName = null;
            string? mahberatName        = null;
            if (req.receipt_wereda_id != null)
                receiptWeredaName = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM weredas WHERE id = @Id", new { Id = (int)req.receipt_wereda_id });
            if (req.receipt_mahberat_id != null)
                receiptMahberatName = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM mahberats WHERE id = @Id", new { Id = (int)req.receipt_mahberat_id });
            // Fallback: resolve from mahberat_id if mahberat_name is null
            if (string.IsNullOrEmpty((string?)req.mahberat_name) && req.mahberat_id != null)
                mahberatName = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT name FROM mahberats WHERE id = @Id", new { Id = (int)req.mahberat_id });

            // Build response dict so we can add the resolved names
            var reqDict = new Dictionary<string, object?>();
            foreach (var prop in ((IDictionary<string, object>)req))
                reqDict[prop.Key] = prop.Value;
            reqDict["receipt_wereda_name"]   = receiptWeredaName;
            reqDict["receipt_mahberat_name"] = receiptMahberatName ?? mahberatName;
            // Ensure mahberat_name is populated
            if (reqDict["mahberat_name"] == null || string.IsNullOrEmpty(reqDict["mahberat_name"]?.ToString()))
                reqDict["mahberat_name"] = receiptMahberatName ?? mahberatName ?? reqDict["mahberat_user_name"];

            return Ok(new { request = reqDict, logs });
        }

        // GET /api/transport/notifications/{userId}
        [HttpGet("notifications/{userId}")]
        public async Task<IActionResult> GetNotifications(int userId)
        {
            using var conn = CreateConnection();
            var notifs = await conn.QueryAsync(
                @"SELECT id, transport_request_id, request_number, title, body, notification_type, is_read, created_at
                  FROM transport_notifications WHERE recipient_user_id = @UserId
                  ORDER BY created_at DESC LIMIT 50",
                new { UserId = userId });
            return Ok(notifs);
        }

        // POST /api/transport/notifications/{id}/read
        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync("UPDATE transport_notifications SET is_read = 1 WHERE id = @Id", new { Id = id });
            return Ok(new { success = true });
        }

        // GET /api/transport/drivers/available
        [HttpGet("drivers/available")]
        public async Task<IActionResult> GetAvailableDrivers()
        {
            using var conn = CreateConnection();
            var drivers = await conn.QueryAsync(
                @"SELECT u.id, u.name, u.phone,
                         v.id as vehicleId, v.plate_number as vehiclePlate, v.model as vehicleModel
                  FROM users u
                  LEFT JOIN vehicles v ON v.driver_id = u.id AND v.status = 'Available'
                  WHERE u.role = 'Driver' AND u.is_active = TRUE
                  ORDER BY u.name ASC");
            return Ok(drivers);
        }

        // GET /api/transport/stats?userId=&role=
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int? userId, [FromQuery] string? role)
        {
            using var conn = CreateConnection();
            var r = role?.ToLower() ?? "";
            string filter = "";
            var param = new DynamicParameters();

            if (userId.HasValue && (r == "mahberatuser" || r == "weredamahberat"))
            { filter = "WHERE mahberat_user_id = @UserId"; param.Add("UserId", userId); }
            else if (userId.HasValue && r == "driver")
            { filter = "WHERE driver_id = @UserId"; param.Add("UserId", userId); }

            var stats = await conn.QueryFirstAsync(
                $@"SELECT
                    COUNT(*) as total,
                    SUM(status = 'PendingDispatcher') as pending,
                    SUM(status = 'DriverAssigned') as assigned,
                    SUM(status = 'DriverAccepted') as accepted,
                    SUM(status = 'PickedUp') as pickedUp,
                    SUM(status = 'ReceiptSubmitted') as receiptSubmitted,
                    SUM(status = 'ReceiptVerified') as receiptVerified,
                    SUM(status = 'StaffApproved') as staffApproved,
                    SUM(status = 'Paid') as paid,
                    SUM(status IN ('DispatcherRejected','StaffRejected')) as rejected
                  FROM transport_requests {filter}", param);
            return Ok(stats);
        }

        // POST /api/transport/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "No file provided" });
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "transport-uploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
            return Ok(new { url = $"/transport-uploads/{fileName}" });
        }
    }

    // ─── DTOs ────────────────────────────────────────────────────────────────

    public class CreateTransportRequestDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int? MahberatId { get; set; }
        public string PickupLocation { get; set; } = "";
        public string Destination { get; set; } = "";
        public string PassengerItemDetails { get; set; } = "";
        public string RequestedDate { get; set; } = "";
        public string RequestedTime { get; set; } = "";
        public string SpecialInstructions { get; set; } = "";
    }

    public class DispatcherActionDto
    {
        public int DispatcherId { get; set; }
        public string DispatcherName { get; set; } = "";
        public string Action { get; set; } = ""; // "Approve" | "Reject"
        public int? DriverId { get; set; }
        public int? VehicleId { get; set; }
        public string? Notes { get; set; }
    }

    public class DriverActionDto
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string Action { get; set; } = ""; // "Accept" | "Reject"
        public string? Notes { get; set; }
    }

    public class PickupDto
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string? Notes { get; set; }
    }

    public class MahberatPickupDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = ""; // "Approve" | "Reject"
        public string? Notes { get; set; }
    }

    public class SubmitReceiptDto
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string? ReceiptPhotoUrl { get; set; }
        public string? DigitalReceiptUrl { get; set; }
        public string? Notes { get; set; }
        // Extended receipt fields
        public decimal? ActualKilogram { get; set; }
        public int? WeredaId { get; set; }
        public int? MahberatId { get; set; }
    }

    public class MahberatVerifyDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";   // "Verify" | "RequestCorrection"
        public string? Notes { get; set; }
    }

    public class StaffActionDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = "";
        public string Action { get; set; } = ""; // "Approve" | "Reject"
        public decimal? TransportCost { get; set; }
        public string? Notes { get; set; }
    }

    public class ProcessPaymentDto
    {
        public int FinanceStaffId { get; set; }
        public string FinanceStaffName { get; set; } = "";
        public string TransactionNumber { get; set; } = "";
        public string? PaymentProofUrl { get; set; }
        public string? Notes { get; set; }
    }
}
