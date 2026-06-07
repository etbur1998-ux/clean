#nullable disable

namespace CleaningManagmentSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; } = true;
        public string ResetToken { get; set; }
        public DateTime? ResetExpires { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ServiceId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan BookingTime { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public User User { get; set; }
        public Service Service { get; set; }
    }

    // Super Admin Models
    public class OutsourceCompany
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string Status { get; set; }
        public string ServicesProvided { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PrivateCleaningCompany
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string LicenseNumber { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string ServicesOffered { get; set; }
        public string ContractStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StaffReceipt
    {
        public int Id { get; set; }
        public int WeredaId { get; set; }
        public string WeredaName { get; set; } = "";
        public int MahberatId { get; set; }
        public string MahberatName { get; set; } = "";
        public int VehicleId { get; set; }
        public string PlateNumber { get; set; } = "";
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public TimeSpan ReceiptTime { get; set; }
        public DateTime ReceiptDate { get; set; }
        public decimal Kilogram { get; set; }
        public decimal Price { get; set; }
        public string RegisteredBy { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
        public string Status { get; set; } = "Registered";
    }

    public class Receipt
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; }
        public int? UserId { get; set; }
        public int? ServiceId { get; set; }
        public string ClientName { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public User User { get; set; }
    }

    public class Payroll
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeRole { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public string Month { get; set; }
        public int Year { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CapitalTransaction
    {
        public int Id { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Category { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public int AuthorId { get; set; }
        public string Status { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Wereda Mahberat Models
    public class MonthlyReceipt
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; }
        public string Month { get; set; }
        public int Year { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Dispatch Officer Models
    public class MeetingRoom
    {
        public int Id { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public string Equipment { get; set; }
        public bool IsAvailable { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MahberatReport
    {
        public int Id { get; set; }
        public string ReportNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReportType { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; }
        public int GeneratedBy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Dispatch
    {
        public int Id { get; set; }
        public string DispatchNumber { get; set; }
        public string Destination { get; set; }
        public string Origin { get; set; }
        public string DriverName { get; set; }
        public string VehicleNumber { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime ExpectedArrival { get; set; }
        public string Status { get; set; }
        public string Contents { get; set; }
        public string Priority { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Staff Models
    public class OfficePlan
    {
        public int Id { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public string Floor { get; set; }
        public string Section { get; set; }
        public string LayoutImage { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LibraryItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string ISBN { get; set; }
        public int Quantity { get; set; }
        public int Available { get; set; }
        public string Location { get; set; }
        public DateTime AddedDate { get; set; }
        public string Status { get; set; }
    }

    public class AgencyReport
    {
        public int Id { get; set; }
        public string ReportNumber { get; set; }
        public string AgencyName { get; set; }
        public string ReportType { get; set; }
        public string Period { get; set; }
        public string Summary { get; set; }
        public string FilePath { get; set; }
        public int GeneratedBy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class YakaReport
    {
        public int Id { get; set; }
        public string ReportNumber { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Period { get; set; }
        public string FilePath { get; set; }
        public int GeneratedBy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubcityOfficer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Subcity { get; set; }
        public string Position { get; set; }
        public string Responsibilities { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubcityDriver
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LicenseNumber { get; set; }
        public string Phone { get; set; }
        public string Subcity { get; set; }
        public string VehicleAssigned { get; set; }
        public bool IsAvailable { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WeredaOfficer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Wereda { get; set; }
        public string Position { get; set; }
        public string Responsibilities { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DispatchSchedule
    {
        public int Id { get; set; }
        public string ScheduleNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public string DriverName { get; set; }
        public string VehicleNumber { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OutsourceReceipt
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string ServiceType { get; set; }
        public decimal Amount { get; set; }
        public DateTime ServiceDate { get; set; }
        public string PaymentStatus { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OfficeRecognition
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string RecipientName { get; set; }
        public string RecipientRole { get; set; }
        public string Reason { get; set; }
        public string AwardType { get; set; }
        public DateTime AwardDate { get; set; }
        public string CertificateUrl { get; set; }
        public int PresentedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Training
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Trainer { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public int Participants { get; set; }
        public string Status { get; set; }
        public string Materials { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ContactMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RepliedAt { get; set; }
        public string Reply { get; set; }
    }

    public class UserSetting
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Gallery Models
    public class Gallery
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public int Views { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Driver Models
    public class DriverLocation
    {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DriverTask
    {
        public int Id { get; set; }
        public string TaskNumber { get; set; } = "";
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string DropoffLocation { get; set; } = "";
        public DateTime TaskDate { get; set; }
        public TimeSpan PickupTime { get; set; }
        public string Status { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Notes { get; set; } = "";
        public int? AssignedBy { get; set; }
        public string AssignedByName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Company { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string RecipientPhone { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; }
    }

    // Checklist Model
    public class Checklist
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedTo { get; set; }
        public int AssignedToUserId { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Role Usage and Permissions Models
    public class RoleDefinition
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string UsageContext { get; set; } = "";
        public string PrimaryResponsibilities { get; set; } = "";
        public string DailyActivities { get; set; } = "";
        public string ReportsAccess { get; set; } = "";
        public string ModulesAccess { get; set; } = "";
        public int AccessLevel { get; set; } // 1=Read, 2=Write, 3=Edit, 4=Admin
        public bool CanCreateUsers { get; set; }
        public bool CanViewFinancials { get; set; }
        public bool CanManageDispatch { get; set; }
        public bool CanViewPayroll { get; set; }
        public bool CanManageStaff { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RoleActivityLog
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

    public class RolePermission
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public string PermissionType { get; set; } = ""; // View, Create, Edit, Delete, Approve
        public bool IsAllowed { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    // Manager Registration Models
    public class Wereda
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Subcity { get; set; } = "";
       public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Mahberat
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int WeredaId { get; set; }
        public string WeredaName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Vehicle
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = "";
        public string VehicleType { get; set; } = ""; // Van, Truck, Car, etc.
        public string Model { get; set; } = "";
        public string Color { get; set; } = "";
        public int? DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string Status { get; set; } = "Available"; // Available, Assigned, Maintenance
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Driver
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public string LicenseType { get; set; } = "";
        public DateTime LicenseExpiry { get; set; }
        public string Address { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SystemUsageAnalytics
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = "";
        public string MetricName { get; set; } = "";
        public int MetricValue { get; set; }
        public string Period { get; set; } = ""; // Daily, Weekly, Monthly
        public DateTime RecordDate { get; set; }
        public string Notes { get; set; } = "";
    }

    // ─── Mahberat Transport Request & Payment Workflow ───────────────────────

    /// <summary>
    /// Transport request created by a Mahberat user.
    /// Status flow:
    ///   PendingDispatcher → DispatcherApproved → DriverAssigned → DriverAccepted
    ///   → PickedUp → MahberatApprovedPickup → ReceiptSubmitted → ReceiptVerified
    ///   → StaffApproved → Paid → Completed
    /// Alternative: DispatcherRejected | DriverRejected | StaffRejected
    /// </summary>
    public class TransportRequest
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = "";

        // Requester
        public int MahberatUserId { get; set; }
        public string MahberatUserName { get; set; } = "";
        public int? MahberatId { get; set; }
        public string MahberatName { get; set; } = "";

        // Trip details
        public string PickupLocation { get; set; } = "";
        public string Destination { get; set; } = "";
        public string PassengerItemDetails { get; set; } = "";
        public DateTime RequestedDate { get; set; }
        public string RequestedTime { get; set; } = "";
        public string SpecialInstructions { get; set; } = "";

        // Dispatcher
        public int? DispatcherId { get; set; }
        public string DispatcherName { get; set; } = "";
        public string DispatcherNotes { get; set; } = "";
        public DateTime? DispatcherActionAt { get; set; }

        // Driver
        public int? DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public int? VehicleId { get; set; }
        public string VehiclePlate { get; set; } = "";
        public string DriverNotes { get; set; } = "";
        public DateTime? DriverActionAt { get; set; }

        // Pickup confirmation
        public DateTime? PickupConfirmedAt { get; set; }
        public string PickupNotes { get; set; } = "";
        public DateTime? MahberatPickupApprovedAt { get; set; }
        public string MahberatPickupNotes { get; set; } = "";

        // Receipt
        public string? ReceiptPhotoUrl { get; set; }
        public string? DigitalReceiptUrl { get; set; }
        public string ReceiptNotes { get; set; } = "";
        public DateTime? ReceiptSubmittedAt { get; set; }

        // Mahberat receipt verification
        public DateTime? MahberatVerifiedAt { get; set; }
        public string MahberatVerificationNotes { get; set; } = "";

        // Staff verification
        public int? StaffId { get; set; }
        public string StaffName { get; set; } = "";
        public decimal? TransportCost { get; set; }
        public string StaffNotes { get; set; } = "";
        public DateTime? StaffActionAt { get; set; }

        // Payment
        public int? FinanceStaffId { get; set; }
        public string FinanceStaffName { get; set; } = "";
        public string? TransactionNumber { get; set; }
        public string? PaymentProofUrl { get; set; }
        public DateTime? PaidAt { get; set; }
        public string PaymentNotes { get; set; } = "";

        // Status
        public string Status { get; set; } = "PendingDispatcher";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Audit log for every status change on a TransportRequest.
    /// </summary>
    public class TransportRequestLog
    {
        public int Id { get; set; }
        public int TransportRequestId { get; set; }
        public string FromStatus { get; set; } = "";
        public string ToStatus { get; set; } = "";
        public int ActorUserId { get; set; }
        public string ActorName { get; set; } = "";
        public string ActorRole { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// In-app notification record for transport workflow events.
    /// </summary>
    public class TransportNotification
    {
        public int Id { get; set; }
        public int RecipientUserId { get; set; }
        public int TransportRequestId { get; set; }
        public string RequestNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string NotificationType { get; set; } = ""; // Info, Success, Warning, Action
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }

    // ─── WMRAS Request Forms Workflow ───────────────────────

    public class RequestModel
    {
        public int Id { get; set; }
        public int RequestorId { get; set; }
        public string RequestorRole { get; set; }
        public int? WeredaId { get; set; }
        public int? MahberatId { get; set; }
        public string RequestType { get; set; }
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public string Urgency { get; set; }
        public DateTime? RequestedDateTime { get; set; }
        public string AttachmentPath { get; set; }
        public int? AssignedToId { get; set; }
        public string ExecutionStatus { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string ExecutionReportPath { get; set; }
        public bool IsClosed { get; set; }
        public int? ClosedById { get; set; }
        public DateTime? ClosureDate { get; set; }
        public string ClosureRemarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RequestApproval
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string Level1Status { get; set; }
        public int? Level1By { get; set; }
        public DateTime? Level1Date { get; set; }
        public string Level1Comments { get; set; }
        public string Level2Status { get; set; }
        public int? Level2By { get; set; }
        public DateTime? Level2Date { get; set; }
        public string Level2Comments { get; set; }
        public string Level3Status { get; set; }
        public int? Level3By { get; set; }
        public DateTime? Level3Date { get; set; }
        public string Level3Comments { get; set; }
    }
}
