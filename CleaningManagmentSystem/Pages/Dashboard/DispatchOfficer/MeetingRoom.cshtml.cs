using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    public class MeetingRoomModel : PageModel
    {
        private readonly string _connectionString;

        [BindProperty]
        public List<MeetingRoom> MeetingRooms { get; set; } = new();

        [BindProperty]
        public MeetingRoom NewRoom { get; set; } = new();

        [BindProperty]
        public int EditId { get; set; }

        [BindProperty]
        public MeetingRoom EditRoom { get; set; } = new();

        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public int ScheduledMeetings { get; set; }
        public int TotalParticipants { get; set; }
        public string AvgDuration { get; set; } = "";

        public MeetingRoomModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult OnGet()
        {
            Console.WriteLine("[MeetingRoom] OnGet called");

            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole");

            Console.WriteLine($"[MeetingRoom] Session - UserId: {userId}, UserName: {userName}, Role: {role}");

            if (userId == null || userId == 0)
            {
                Console.WriteLine("[MeetingRoom] User not logged in, redirecting to Login");
                return RedirectToPage("/Login");
            }

            if (role?.ToLower() != "dispatch_officer")
            {
                Console.WriteLine($"[MeetingRoom] User role {role} not authorized for this page");
                return RedirectToPage("/Login");
            }

            try
            {
                LoadMeetingRooms();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeetingRoom] Error loading data: {ex.Message}");
                ErrorMessage = "Failed to load meeting room data";
            }

            return Page();
        }

        public IActionResult OnPostCreate()
        {
            Console.WriteLine("[MeetingRoom] OnPostCreate called");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(NewRoom.RoomName))
            {
                ErrorMessage = "Room name is required";
                OnGet();
                return Page();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"INSERT INTO meeting_rooms (room_name, capacity, location, equipment, is_available, status, created_at) 
                           VALUES (@RoomName, @Capacity, @Location, @Equipment, @IsAvailable, @Status, @CreatedAt)";
                
                NewRoom.Status = "Active";
                NewRoom.CreatedAt = DateTime.Now;
                
                connection.Execute(sql, new
                {
                    NewRoom.RoomName,
                    NewRoom.Capacity,
                    NewRoom.Location,
                    NewRoom.Equipment,
                    NewRoom.IsAvailable,
                    NewRoom.Status,
                    NewRoom.CreatedAt
                });

                Console.WriteLine($"[MeetingRoom] Created new meeting room: {NewRoom.RoomName}");
                SuccessMessage = "Meeting room created successfully";
                NewRoom = new MeetingRoom();
                LoadMeetingRooms();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeetingRoom] Error creating room: {ex.Message}");
                ErrorMessage = "Failed to create meeting room";
            }

            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            Console.WriteLine($"[MeetingRoom] OnPostUpdate called for ID: {EditId}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var sql = @"UPDATE meeting_rooms SET room_name = @RoomName, capacity = @Capacity, 
                           location = @Location, equipment = @Equipment, is_available = @IsAvailable, 
                           status = @Status WHERE id = @Id";
                
                connection.Execute(sql, new
                {
                    Id = EditId,
                    EditRoom.RoomName,
                    EditRoom.Capacity,
                    EditRoom.Location,
                    EditRoom.Equipment,
                    EditRoom.IsAvailable,
                    EditRoom.Status
                });

                Console.WriteLine($"[MeetingRoom] Updated meeting room ID: {EditId}");
                SuccessMessage = "Meeting room updated successfully";
                LoadMeetingRooms();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeetingRoom] Error updating room: {ex.Message}");
                ErrorMessage = "Failed to update meeting room";
            }

            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            Console.WriteLine($"[MeetingRoom] OnPostDelete called for ID: {id}");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute("DELETE FROM meeting_rooms WHERE id = @Id", new { Id = id });

                Console.WriteLine($"[MeetingRoom] Deleted meeting room ID: {id}");
                SuccessMessage = "Meeting room deleted successfully";
                LoadMeetingRooms();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeetingRoom] Error deleting room: {ex.Message}");
                ErrorMessage = "Failed to delete meeting room";
            }

            return Page();
        }

        private void LoadMeetingRooms()
        {
            using var connection = new MySqlConnection(_connectionString);
            MeetingRooms = connection.Query<MeetingRoom>("SELECT * FROM meeting_rooms ORDER BY created_at DESC").ToList();
            Console.WriteLine($"[MeetingRoom] Loaded {MeetingRooms.Count} meeting rooms");
        }

        private void LoadStatistics()
        {
            using var connection = new MySqlConnection(_connectionString);
            
            ScheduledMeetings = connection.QueryFirstOrDefault<int?>("SELECT COUNT(*) FROM meeting_rooms WHERE is_available = 1") ?? 0;
            TotalParticipants = connection.QueryFirstOrDefault<int?>("SELECT COALESCE(SUM(capacity), 0) FROM meeting_rooms WHERE status = 'Active'") ?? 0;
            AvgDuration = "2.5 hrs";
            
            Console.WriteLine($"[MeetingRoom] Statistics - Scheduled: {ScheduledMeetings}, Participants: {TotalParticipants}");
        }
    }
}
