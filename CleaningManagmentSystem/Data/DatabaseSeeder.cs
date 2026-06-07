using MySqlConnector;
using Dapper;
using CleaningManagmentSystem.Models;
using System.Text.Json;

namespace CleaningManagmentSystem.Data
{
    public static class DatabaseSeeder
    {
private static readonly string[] RequiredTables = new[]
{
    "users",
    "services",
    "bookings",
    "outsource_companies",
    "private_cleaning_companies",
    "private_company_receipts",
    "receipts",
    "payroll",
    "capital_transactions",
    "posts",
    "monthly_receipts",
    "meeting_rooms",
    "mahberat_reports",
    "dispatches",
    "office_plans",
    "library_items",
    "agency_reports",
    "yaka_reports",
    "subcity_officers",
    "subcity_drivers",
    "wereda_officers",
    "dispatch_schedules",
    "outsource_receipts",
    "office_recognitions",
    "trainings",
    "contact_messages",
    "user_settings",
    "gallery",
    "driver_locations",
    "delivery_tasks",
    "contacts",
    "messages",
    "checklists",
    "role_definitions",
    "role_permissions",
    "role_activity_logs",
    "system_usage_analytics",
    "weredas",
    "mahberats",
    "vehicles",
    "drivers",
    "staff_receipts",
    "transport_requests",
    "transport_request_logs",
    "transport_notifications",
    "requests",
    "request_approvals"
};

        public static async Task SeedAsync(string connectionString)
        {
            Console.WriteLine("[Seeder] Starting database seeding...");

            await CreateTablesAsync(connectionString);
            await SeedRolesAndPermissionsAsync(connectionString);
            await SeedSampleUsersAsync(connectionString);
            await SeedSampleDataAsync(connectionString);
            await AddMissingColumnsAsync(connectionString);

            Console.WriteLine("[Seeder] Database seeding completed successfully!");
        }

        private static async Task AddMissingColumnsAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            try
            {
                // ── Convert role column from ENUM to VARCHAR if still old schema ──
                var roleColumnType = await connection.QueryFirstOrDefaultAsync<string>(
                    @"SELECT COLUMN_TYPE FROM information_schema.COLUMNS
                      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'role'");
                if (!string.IsNullOrEmpty(roleColumnType) && roleColumnType.StartsWith("enum(", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Seeder] Converting users.role from ENUM to VARCHAR(100)...");
                    await connection.ExecuteAsync("ALTER TABLE users MODIFY COLUMN role VARCHAR(100) NOT NULL DEFAULT 'user'");
                }

                var hasResetToken = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'users' AND column_name = 'reset_token'");
                
                if (hasResetToken == 0)
                {
                    Console.WriteLine("[Seeder] Adding reset_token column to users table...");
                    await connection.ExecuteAsync("ALTER TABLE users ADD COLUMN reset_token VARCHAR(255) AFTER is_active");
                }

                var hasResetExpires = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'users' AND column_name = 'reset_expires'");
                
                if (hasResetExpires == 0)
                {
                    Console.WriteLine("[Seeder] Adding reset_expires column to users table...");
                    await connection.ExecuteAsync("ALTER TABLE users ADD COLUMN reset_expires DATETIME AFTER reset_token");
                }

                var hasAssignedTo = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'trainings' AND column_name = 'assigned_to_user_id'");
                
                if (hasAssignedTo == 0)
                {
                    Console.WriteLine("[Seeder] Adding assigned_to_user_id column to trainings table...");
                    await connection.ExecuteAsync("ALTER TABLE trainings ADD COLUMN assigned_to_user_id INT AFTER materials");
                }

                var hasCategory = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'trainings' AND column_name = 'category'");
                
                if (hasCategory == 0)
                {
                    Console.WriteLine("[Seeder] Adding category column to trainings table...");
                    await connection.ExecuteAsync("ALTER TABLE trainings ADD COLUMN category VARCHAR(100) DEFAULT 'General' AFTER assigned_to_user_id");
                }

                var hasTrainingId = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'posts' AND column_name = 'training_id'");
                
                if (hasTrainingId == 0)
                {
                    Console.WriteLine("[Seeder] Adding training_id column to posts table...");
                    await connection.ExecuteAsync("ALTER TABLE posts ADD COLUMN training_id INT AFTER content");
                }

                var hasIsPinned = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'posts' AND column_name = 'is_pinned'");
                
                if (hasIsPinned == 0)
                {
                    Console.WriteLine("[Seeder] Adding is_pinned column to posts table...");
                    await connection.ExecuteAsync("ALTER TABLE posts ADD COLUMN is_pinned TINYINT DEFAULT 0 AFTER training_id");
                }

                var hasPriority = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'posts' AND column_name = 'priority'");
                
                if (hasPriority == 0)
                {
                    Console.WriteLine("[Seeder] Adding priority column to posts table...");
                    await connection.ExecuteAsync("ALTER TABLE posts ADD COLUMN priority VARCHAR(50) DEFAULT 'Normal' AFTER is_pinned");
                }

                var hasTargetRole = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'posts' AND column_name = 'target_role'");
                
                if (hasTargetRole == 0)
                {
                    Console.WriteLine("[Seeder] Adding target_role column to posts table...");
                    await connection.ExecuteAsync("ALTER TABLE posts ADD COLUMN target_role VARCHAR(100) DEFAULT 'All' AFTER priority");
                }

                var hasNotesStaffReceipts = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'staff_receipts' AND column_name = 'notes'");
                
                if (hasNotesStaffReceipts == 0)
                {
                    Console.WriteLine("[Seeder] Adding missing columns to staff_receipts table...");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN notes TEXT");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN image_url VARCHAR(500)");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN latitude DECIMAL(10,8)");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN longitude DECIMAL(11,8)");
                }

                var hasWeredaNameOutsource = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'outsource_receipts' AND column_name = 'wereda_name'");
                
                if (hasWeredaNameOutsource == 0)
                {
                    Console.WriteLine("[Seeder] outsource_receipts has old schema. Recreating...");
                    await connection.ExecuteAsync("DROP TABLE IF EXISTS outsource_receipts");
                    await connection.ExecuteAsync(@"CREATE TABLE outsource_receipts (
                                  id INT AUTO_INCREMENT PRIMARY KEY,
                                  wereda_id INT,
                                  wereda_name VARCHAR(255),
                                  company_id INT,
                                  company_name VARCHAR(255),
                                  vehicle_id INT,
                                  plate_number VARCHAR(50),
                                  driver_id INT,
                                  driver_name VARCHAR(255),
                                  receipt_time TIME,
                                  receipt_date DATE,
                                  kilogram DECIMAL(10,2),
                                  price DECIMAL(10,2) DEFAULT 0.00,
                                  registered_by VARCHAR(255),
                                  registered_at DATETIME DEFAULT NOW(),
                                  status VARCHAR(50) DEFAULT 'Registered',
                                  notes TEXT,
                                  image_url VARCHAR(500),
                                  INDEX idx_date (receipt_date),
                                  INDEX idx_status (status),
                                  INDEX idx_kilogram (kilogram)
                              )");
                }

                // ── Transport receipt extra fields ──
                var hasActualKg = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'transport_requests' AND column_name = 'actual_kilogram'");
                if (hasActualKg == 0)
                {
                    Console.WriteLine("[Seeder] Adding receipt fields to transport_requests...");
                    await connection.ExecuteAsync("ALTER TABLE transport_requests ADD COLUMN actual_kilogram DECIMAL(10,2) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE transport_requests ADD COLUMN receipt_wereda_id INT DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE transport_requests ADD COLUMN receipt_mahberat_id INT DEFAULT NULL");
                }

                // ── Mahberat Level-1 approval columns for staff_receipts ──
                var hasMahberatApprovedStaff = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'staff_receipts' AND column_name = 'mahberat_approved'");
                if (hasMahberatApprovedStaff == 0)
                {
                    Console.WriteLine("[Seeder] Adding mahberat approval columns to staff_receipts...");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN mahberat_approved TINYINT DEFAULT NULL COMMENT '1=Approved, 0=Rejected, NULL=Pending'");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN mahberat_approved_by VARCHAR(255) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN mahberat_approved_at DATETIME DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN mahberat_notes TEXT DEFAULT NULL");
                }

                // ── Mahberat Level-1 approval columns for outsource_receipts ──
                var hasMahberatApprovedOutsource = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'outsource_receipts' AND column_name = 'mahberat_approved'");
                if (hasMahberatApprovedOutsource == 0)
                {
                    Console.WriteLine("[Seeder] Adding mahberat approval columns to outsource_receipts...");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN mahberat_approved TINYINT DEFAULT NULL COMMENT '1=Approved, 0=Rejected, NULL=Pending'");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN mahberat_approved_by VARCHAR(255) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN mahberat_approved_at DATETIME DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN mahberat_notes TEXT DEFAULT NULL");
                }

                // ── Staff approval columns for staff_receipts (approved_by, approved_at, rejected_by, rejected_at, reject_notes) ──
                var hasApprovedByStaff = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'staff_receipts' AND column_name = 'approved_by'");
                if (hasApprovedByStaff == 0)
                {
                    Console.WriteLine("[Seeder] Adding staff approval columns to staff_receipts...");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN approved_by VARCHAR(255) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN approved_at DATETIME DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN rejected_by VARCHAR(255) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN rejected_at DATETIME DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN reject_notes TEXT DEFAULT NULL");
                }

                // ── Staff approval columns for outsource_receipts ──
                var hasApprovedByOutsource = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'outsource_receipts' AND column_name = 'approved_by'");
                if (hasApprovedByOutsource == 0)
                {
                    Console.WriteLine("[Seeder] Adding staff approval columns to outsource_receipts...");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN approved_by VARCHAR(255) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN approved_at DATETIME DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN rejected_by VARCHAR(255) DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN rejected_at DATETIME DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE outsource_receipts ADD COLUMN reject_notes TEXT DEFAULT NULL");
                }

                // ── transport_request_id link column for staff_receipts ──
                var hasTrIdColumn = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'staff_receipts' AND column_name = 'transport_request_id'");
                if (hasTrIdColumn == 0)
                {
                    Console.WriteLine("[Seeder] Adding transport_request_id to staff_receipts...");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD COLUMN transport_request_id INT DEFAULT NULL");
                    await connection.ExecuteAsync("ALTER TABLE staff_receipts ADD INDEX idx_tr_id (transport_request_id)");
                }

                // ── Add is_active to private_cleaning_companies if missing ──
                var hasPccIsActive = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='private_cleaning_companies' AND column_name='is_active'");
                if (hasPccIsActive == 0)
                {
                    Console.WriteLine("[Seeder] Adding is_active to private_cleaning_companies...");
                    await connection.ExecuteAsync("ALTER TABLE private_cleaning_companies ADD COLUMN is_active TINYINT DEFAULT 1");
                }

                // ── Add rep_user_id to private_cleaning_companies if missing ──
                var hasPccRepUserId = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='private_cleaning_companies' AND column_name='rep_user_id'");
                if (hasPccRepUserId == 0)
                {
                    Console.WriteLine("[Seeder] Adding rep_user_id to private_cleaning_companies...");
                    await connection.ExecuteAsync("ALTER TABLE private_cleaning_companies ADD COLUMN rep_user_id INT DEFAULT NULL");
                }

                // ── Backfill rep_user_id where email matches a PrivateCompanyRep user ──
                try
                {
                    await connection.ExecuteAsync(
                        @"UPDATE private_cleaning_companies p
                          JOIN users u ON u.email = p.email
                             AND u.role = 'PrivateCompanyRep'
                             AND u.is_active = 1
                          SET p.rep_user_id = u.id
                          WHERE p.rep_user_id IS NULL AND p.email IS NOT NULL AND p.email != ''");
                    Console.WriteLine("[Seeder] Backfilled rep_user_id on private_cleaning_companies.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Seeder] rep_user_id backfill warning: {ex.Message}");
                }

                // ── Add deleted_at to private_cleaning_companies if missing ──
                var hasPccDeletedAt = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='private_cleaning_companies' AND column_name='deleted_at'");
                if (hasPccDeletedAt == 0)
                {
                    Console.WriteLine("[Seeder] Adding deleted_at to private_cleaning_companies...");
                    await connection.ExecuteAsync("ALTER TABLE private_cleaning_companies ADD COLUMN deleted_at DATETIME DEFAULT NULL");
                }

                // ── Rename contract_status to status in private_cleaning_companies if needed ──
                var hasPccContractStatus = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='private_cleaning_companies' AND column_name='contract_status'");
                if (hasPccContractStatus > 0)
                {
                    Console.WriteLine("[Seeder] Renaming contract_status to status in private_cleaning_companies...");
                    try
                    {
                        await connection.ExecuteAsync("ALTER TABLE private_cleaning_companies CHANGE COLUMN contract_status status VARCHAR(50) DEFAULT 'Active'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Seeder] contract_status rename warning: {ex.Message}");
                    }
                }

                // ── Fix users that were saved with empty/null role ──
                try
                {
                    var emptyRoleCount = await connection.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM users WHERE role IS NULL OR role = ''");
                    if (emptyRoleCount > 0)
                    {
                        Console.WriteLine($"[Seeder] Found {emptyRoleCount} user(s) with empty role — setting to 'staff'.");
                        await connection.ExecuteAsync(
                            "UPDATE users SET role = 'staff' WHERE role IS NULL OR role = ''");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Seeder] Empty role fix warning: {ex.Message}");
                }

                // ── Backfill monthly_receipts from paid transport requests ──
                try
                {
                    var unpopulated = await connection.QueryAsync<dynamic>(@"
                        SELECT tr.id, tr.request_number, tr.pickup_location, tr.destination,
                               tr.driver_name, tr.actual_kilogram, tr.transport_cost,
                               tr.paid_at, tr.staff_action_at, tr.updated_at, tr.created_at,
                               tr.mahberat_user_name,
                               m.name AS mahberat_name
                        FROM transport_requests tr
                        LEFT JOIN mahberats m ON m.id = COALESCE(tr.receipt_mahberat_id, tr.mahberat_id)
                        WHERE tr.status IN ('Paid','StaffApproved','ReceiptVerified')
                          AND tr.request_number NOT IN (SELECT receipt_number FROM monthly_receipts)");

                    foreach (var tr in unpopulated)
                    {
                        DateTime completedAt = (DateTime)(tr.paid_at ?? tr.staff_action_at ?? tr.updated_at ?? tr.created_at);
                        decimal cost = (decimal)(tr.transport_cost ?? 0m);
                        decimal kg   = (decimal)(tr.actual_kilogram ?? 0m);
                        string  src  = $"Transport | {tr.pickup_location} → {tr.destination} | Mahberat: {tr.mahberat_name ?? tr.mahberat_user_name} | Driver: {tr.driver_name ?? "-"} | {kg} KG";

                        await connection.ExecuteAsync(@"
                            INSERT IGNORE INTO monthly_receipts
                              (receipt_number, month, year, total_amount, paid_amount,
                               balance, status, source, created_at, updated_at)
                            VALUES (@Num, @Month, @Year, @Total, @Paid, 0, 'Billed', @Source, NOW(), NOW())",
                            new {
                                Num    = (string)tr.request_number,
                                Month  = completedAt.ToString("MMMM"),
                                Year   = completedAt.Year,
                                Total  = cost,
                                Paid   = cost,
                                Source = src
                            });
                    }

                    if (((IEnumerable<dynamic>)unpopulated).Any())
                        Console.WriteLine($"[Seeder] Backfilled {((IEnumerable<dynamic>)unpopulated).Count()} paid transport request(s) into monthly_receipts.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Seeder] monthly_receipts backfill warning: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seeder] Column check error: {ex.Message}");
            }
        }

        private static async Task CreateTablesAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var table in RequiredTables)
            {
                try
                {
                    var result = await connection.QueryFirstOrDefaultAsync<int>(
                        $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{table}'");

                    if (result == 0)
                    {
                        Console.WriteLine($"[Seeder] Creating table: {table}");
                        string? createTableSql = table switch
                        {
                            "users" => @"CREATE TABLE users (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255) NOT NULL,
                                email VARCHAR(255) UNIQUE NOT NULL,
                                password VARCHAR(255) NOT NULL,
                                role VARCHAR(100) NOT NULL,
                                phone VARCHAR(50),
                                address TEXT,
                                is_active BOOLEAN DEFAULT TRUE,
                                reset_token VARCHAR(255),
                                reset_expires DATETIME,
                                created_by INT,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "services" => @"CREATE TABLE services (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255) NOT NULL,
                                description TEXT,
                                price DECIMAL(10,2),
                                duration INT,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "bookings" => @"CREATE TABLE bookings (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                user_id INT,
                                service_id INT,
                                booking_date DATE,
                                booking_time TIME,
                                address TEXT,
                                status VARCHAR(50),
                                notes TEXT,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "outsource_companies" => @"CREATE TABLE outsource_companies (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                company_name VARCHAR(255) NOT NULL,
                                contact_person VARCHAR(255),
                                phone VARCHAR(50),
                                email VARCHAR(255),
                                license_number VARCHAR(100),
                                contract_start_date DATE,
                                contract_end_date DATE,
                                status VARCHAR(50) DEFAULT 'Active',
                                services_provided TEXT,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "private_cleaning_companies" => @"CREATE TABLE private_cleaning_companies (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                company_name VARCHAR(255) NOT NULL,
                                license_number VARCHAR(100),
                                contact_person VARCHAR(255),
                                phone VARCHAR(50),
                                email VARCHAR(255),
                                address TEXT,
                                services_offered TEXT,
                                status VARCHAR(50) DEFAULT 'Active',
                                is_active TINYINT DEFAULT 1,
                                rep_user_id INT DEFAULT NULL,
                                deleted_at DATETIME DEFAULT NULL,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "private_company_receipts" => @"CREATE TABLE private_company_receipts (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                company_id INT,
                                company_name VARCHAR(255),
                                wereda_id INT,
                                wereda_name VARCHAR(255),
                                vehicle_id INT,
                                plate_number VARCHAR(50),
                                driver_id INT,
                                driver_name VARCHAR(255),
                                receipt_time TIME,
                                receipt_date DATE,
                                kilogram DECIMAL(10,2),
                                price DECIMAL(10,2) DEFAULT 0.00,
                                total_amount DECIMAL(10,2) DEFAULT 0.00,
                                notes TEXT,
                                registered_by VARCHAR(255),
                                status VARCHAR(50) DEFAULT 'Registered',
                                registered_at DATETIME DEFAULT NOW(),
                                INDEX idx_company (company_id),
                                INDEX idx_date (receipt_date),
                                INDEX idx_status (status)
                            )",
                            "receipts" => @"CREATE TABLE receipts (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                receipt_number VARCHAR(100) UNIQUE NOT NULL,
                                user_id INT,
                                service_id INT,
                                client_name VARCHAR(255),
                                description TEXT,
                                amount DECIMAL(10,2),
                                payment_method VARCHAR(50),
                                receipt_date DATE,
                                status VARCHAR(50) DEFAULT 'Pending',
                                notes TEXT,
                                created_by INT,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "payroll" => @"CREATE TABLE payroll (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                employee_id INT NOT NULL,
                                employee_name VARCHAR(255),
                                employee_role VARCHAR(100),
                                base_salary DECIMAL(10,2),
                                bonus DECIMAL(10,2) DEFAULT 0,
                                deductions DECIMAL(10,2) DEFAULT 0,
                                net_salary DECIMAL(10,2),
                                month VARCHAR(20),
                                year INT,
                                status VARCHAR(50),
                                payment_date DATE,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "capital_transactions" => @"CREATE TABLE capital_transactions (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                transaction_type VARCHAR(50),
                                description TEXT,
                                amount DECIMAL(10,2),
                                balance DECIMAL(10,2),
                                transaction_date DATE,
                                category VARCHAR(100),
                                reference VARCHAR(255),
                                notes TEXT,
                                created_by INT,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "posts" => @"CREATE TABLE posts (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                title VARCHAR(255),
                                category VARCHAR(100),
                                content LONGTEXT,
                                training_id INT,
                                is_pinned TINYINT DEFAULT 0,
                                priority VARCHAR(50) DEFAULT 'Normal',
                                target_role VARCHAR(100) DEFAULT 'All',
                                author VARCHAR(255),
                                author_id INT,
                                status VARCHAR(50),
                                image_url VARCHAR(500),
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "monthly_receipts" => @"CREATE TABLE monthly_receipts (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                receipt_number VARCHAR(100) UNIQUE NOT NULL,
                                month VARCHAR(20),
                                year INT,
                                total_amount DECIMAL(10,2),
                                paid_amount DECIMAL(10,2),
                                balance DECIMAL(10,2),
                                status VARCHAR(50),
                                source VARCHAR(255),
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "meeting_rooms" => @"CREATE TABLE meeting_rooms (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                room_name VARCHAR(255) NOT NULL,
                                capacity INT,
                                location VARCHAR(255),
                                equipment TEXT,
                                is_available BOOLEAN DEFAULT TRUE,
                                status VARCHAR(50),
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "mahberat_reports" => @"CREATE TABLE mahberat_reports (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                report_number VARCHAR(100) UNIQUE NOT NULL,
                                title VARCHAR(255),
                                description TEXT,
                                report_type VARCHAR(100),
                                status VARCHAR(50),
                                file_path VARCHAR(500),
                                generated_by INT,
                                generated_at DATETIME,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "dispatches" => @"CREATE TABLE dispatches (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                dispatch_number VARCHAR(100) UNIQUE NOT NULL,
                                destination VARCHAR(255),
                                origin VARCHAR(255),
                                driver_name VARCHAR(255),
                                vehicle_number VARCHAR(100),
                                dispatch_date DATE,
                                expected_arrival DATE,
                                status VARCHAR(50),
                                contents TEXT,
                                priority VARCHAR(50),
                                created_by INT,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "office_plans" => @"CREATE TABLE office_plans (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                plan_name VARCHAR(255),
                                description TEXT,
                                floor VARCHAR(50),
                                section VARCHAR(50),
                                layout_image VARCHAR(500),
                                effective_from DATE,
                                effective_to DATE,
                                status VARCHAR(50),
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "library_items" => @"CREATE TABLE library_items (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                title VARCHAR(255),
                                author VARCHAR(255),
                                category VARCHAR(100),
                                isbn VARCHAR(50),
                                quantity INT DEFAULT 1,
                                available INT DEFAULT 1,
                                location VARCHAR(255),
                                added_date DATE,
                                status VARCHAR(50)
                            )",
                            "agency_reports" => @"CREATE TABLE agency_reports (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                report_number VARCHAR(100) UNIQUE NOT NULL,
                                agency_name VARCHAR(255),
                                report_type VARCHAR(100),
                                period VARCHAR(50),
                                summary TEXT,
                                file_path VARCHAR(500),
                                generated_by INT,
                                generated_at DATETIME,
                                status VARCHAR(50),
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "yaka_reports" => @"CREATE TABLE yaka_reports (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                report_number VARCHAR(100) UNIQUE NOT NULL,
                                title VARCHAR(255),
                                category VARCHAR(100),
                                description TEXT,
                                period VARCHAR(50),
                                file_path VARCHAR(500),
                                generated_by INT,
                                generated_at DATETIME,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "subcity_officers" => @"CREATE TABLE subcity_officers (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255),
                                email VARCHAR(255),
                                phone VARCHAR(50),
                                subcity VARCHAR(255),
                                position VARCHAR(255),
                                responsibilities TEXT,
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "subcity_drivers" => @"CREATE TABLE subcity_drivers (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255),
                                license_number VARCHAR(100),
                                phone VARCHAR(50),
                                subcity VARCHAR(255),
                                vehicle_assigned VARCHAR(255),
                                is_available BOOLEAN DEFAULT TRUE,
                                status VARCHAR(50),
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "wereda_officers" => @"CREATE TABLE wereda_officers (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255),
                                email VARCHAR(255),
                                phone VARCHAR(50),
                                wereda VARCHAR(255),
                                position VARCHAR(255),
                                responsibilities TEXT,
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "dispatch_schedules" => @"CREATE TABLE dispatch_schedules (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                schedule_number VARCHAR(100) UNIQUE NOT NULL,
                                origin VARCHAR(255),
                                destination VARCHAR(255),
                                scheduled_date DATE,
                                scheduled_time TIME,
                                driver_name VARCHAR(255),
                                vehicle_number VARCHAR(100),
                                purpose TEXT,
                                status VARCHAR(50),
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "outsource_receipts" => @"CREATE TABLE outsource_receipts (
                                 id INT AUTO_INCREMENT PRIMARY KEY,
                                 wereda_id INT,
                                 wereda_name VARCHAR(255),
                                 company_id INT,
                                 company_name VARCHAR(255),
                                 vehicle_id INT,
                                 plate_number VARCHAR(50),
                                 driver_id INT,
                                 driver_name VARCHAR(255),
                                 receipt_time TIME,
                                 receipt_date DATE,
                                 kilogram DECIMAL(10,2),
                                 price DECIMAL(10,2) DEFAULT 0.00,
                                 registered_by VARCHAR(255),
                                 registered_at DATETIME DEFAULT NOW(),
                                 status VARCHAR(50) DEFAULT 'Registered',
                                 notes TEXT,
                                 image_url VARCHAR(500),
                                 INDEX idx_date (receipt_date),
                                 INDEX idx_status (status),
                                 INDEX idx_kilogram (kilogram)
                             )",
                            "office_recognitions" => @"CREATE TABLE office_recognitions (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                title VARCHAR(255),
                                recipient_name VARCHAR(255),
                                recipient_role VARCHAR(255),
                                reason TEXT,
                                award_type VARCHAR(100),
                                award_date DATE,
                                certificate_url VARCHAR(500),
                                presented_by INT,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "trainings" => @"CREATE TABLE trainings (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                title VARCHAR(255),
                                trainer VARCHAR(255),
                                description TEXT,
                                start_date DATE,
                                end_date DATE,
                                location VARCHAR(255),
                                participants INT,
                                status VARCHAR(50),
                                materials TEXT,
                                assigned_to_user_id INT,
                                category VARCHAR(100) DEFAULT 'General',
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "contact_messages" => @"CREATE TABLE contact_messages (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255),
                                email VARCHAR(255),
                                subject VARCHAR(255),
                                message TEXT,
                                phone VARCHAR(50),
                                status VARCHAR(50) DEFAULT 'New',
                                created_at DATETIME DEFAULT NOW(),
                                replied_at DATETIME,
                                reply TEXT
                            )",
                            "user_settings" => @"CREATE TABLE user_settings (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                user_id INT UNIQUE,
                                setting_key VARCHAR(100),
                                setting_value TEXT,
                                description TEXT,
                                updated_at DATETIME DEFAULT NOW() ON UPDATE NOW()
                            )",
                            "gallery" => @"CREATE TABLE gallery (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                title VARCHAR(255),
                                description TEXT,
                                image_url VARCHAR(500),
                                category VARCHAR(100),
                                views INT DEFAULT 0,
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "driver_locations" => @"CREATE TABLE driver_locations (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                driver_id INT NOT NULL,
                                driver_name VARCHAR(255),
                                latitude DECIMAL(10,8),
                                longitude DECIMAL(11,8),
                                address TEXT,
                                notes TEXT,
                                updated_at DATETIME DEFAULT NOW(),
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "delivery_tasks" => @"CREATE TABLE delivery_tasks (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                task_number VARCHAR(100) UNIQUE NOT NULL,
                                driver_id INT,
                                driver_name VARCHAR(255),
                                pickup_location TEXT,
                                dropoff_location TEXT,
                                task_date DATE,
                                pickup_time TIME,
                                status VARCHAR(50) DEFAULT 'Pending',
                                description TEXT,
                                priority VARCHAR(50),
                                notes TEXT,
                                assigned_by INT,
                                assigned_by_name VARCHAR(255),
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "contacts" => @"CREATE TABLE contacts (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                driver_id INT,
                                name VARCHAR(255),
                                phone VARCHAR(50),
                                email VARCHAR(255),
                                company VARCHAR(255),
                                notes TEXT,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "messages" => @"CREATE TABLE messages (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                sender_id INT,
                                recipient_phone VARCHAR(50),
                                content TEXT,
                                sent_at DATETIME DEFAULT NOW(),
                                status VARCHAR(50) DEFAULT 'Sent'
                            )",
                            "checklists" => @"CREATE TABLE checklists (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                title VARCHAR(255),
                                description TEXT,
                                assigned_to VARCHAR(255),
                                assigned_to_user_id INT,
                                category VARCHAR(100),
                                priority VARCHAR(50),
                                status VARCHAR(50) DEFAULT 'Pending',
                                due_date DATE,
                                completed_date DATE,
                                created_at DATETIME DEFAULT NOW()
                            )",
                            "role_definitions" => @"CREATE TABLE role_definitions (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                role_name VARCHAR(100) UNIQUE NOT NULL,
                                display_name VARCHAR(255),
                                description TEXT,
                                usage_context TEXT,
                                primary_responsibilities LONGTEXT,
                                daily_activities LONGTEXT,
                                reports_access LONGTEXT,
                                modules_access LONGTEXT,
                                access_level INT DEFAULT 1,
                                can_create_users BOOLEAN DEFAULT FALSE,
                                can_view_financials BOOLEAN DEFAULT FALSE,
                                can_manage_dispatch BOOLEAN DEFAULT FALSE,
                                can_view_payroll BOOLEAN DEFAULT FALSE,
                                can_manage_staff BOOLEAN DEFAULT FALSE,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "role_permissions" => @"CREATE TABLE role_permissions (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                role_name VARCHAR(100) NOT NULL,
                                module_name VARCHAR(255) NOT NULL,
                                permission_type VARCHAR(50) NOT NULL,
                                is_allowed BOOLEAN DEFAULT TRUE,
                                description TEXT,
                                created_at DATETIME DEFAULT NOW(),
                                UNIQUE KEY unique_permission (role_name, module_name, permission_type)
                            )",
                            "role_activity_logs" => @"CREATE TABLE role_activity_logs (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                user_id INT,
                                user_name VARCHAR(255),
                                user_role VARCHAR(100),
                                activity_type VARCHAR(100),
                                page_accessed VARCHAR(255),
                                action_performed VARCHAR(255),
                                details TEXT,
                                timestamp DATETIME DEFAULT NOW(),
                                INDEX idx_user_role (user_role),
                                INDEX idx_timestamp (timestamp)
                            )",
                            "system_usage_analytics" => @"CREATE TABLE system_usage_analytics (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                role_name VARCHAR(100),
                                metric_name VARCHAR(255),
                                metric_value INT,
                                period VARCHAR(50),
                                record_date DATE,
                                notes TEXT,
                                created_at DATETIME DEFAULT NOW(),
                                INDEX idx_role_period (role_name, period, record_date)
                            )",
                            "weredas" => @"CREATE TABLE weredas (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255) NOT NULL,
                                description TEXT,
                                subcity VARCHAR(255),
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "mahberats" => @"CREATE TABLE mahberats (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                name VARCHAR(255) NOT NULL,
                                wereda_id INT,
                                wereda_name VARCHAR(255),
                                contact_person VARCHAR(255),
                                phone VARCHAR(50),
                                email VARCHAR(255),
                                address TEXT,
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "vehicles" => @"CREATE TABLE vehicles (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                plate_number VARCHAR(50) UNIQUE NOT NULL,
                                vehicle_type VARCHAR(100),
                                model VARCHAR(255),
                                color VARCHAR(100),
                                driver_id INT,
                                driver_name VARCHAR(255),
                                status VARCHAR(50) DEFAULT 'Available',
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
                            "drivers" => @"CREATE TABLE drivers (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                full_name VARCHAR(255) NOT NULL,
                                phone VARCHAR(50),
                                email VARCHAR(255),
                                license_number VARCHAR(100) UNIQUE,
                                license_type VARCHAR(100),
                                license_expiry DATE,
                                address TEXT,
                                is_active BOOLEAN DEFAULT TRUE,
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW()
                            )",
"staff_receipts" => @"CREATE TABLE staff_receipts (
                                 id INT AUTO_INCREMENT PRIMARY KEY,
                                 wereda_id INT,
                                 wereda_name VARCHAR(255),
                                 mahberat_id INT,
                                 mahberat_name VARCHAR(255),
                                 vehicle_id INT,
                                 plate_number VARCHAR(50),
                                 driver_id INT,
                                 driver_name VARCHAR(255),
                                 receipt_time TIME,
                                 receipt_date DATE,
                                 kilogram DECIMAL(10,2),
                                 price DECIMAL(10,2) DEFAULT 0.00,
                                 registered_by VARCHAR(255),
                                 registered_at DATETIME DEFAULT NOW(),
                                 status VARCHAR(50) DEFAULT 'Registered',
                                 notes TEXT,
                                 image_url VARCHAR(500),
                                 latitude DECIMAL(10,8),
                                 longitude DECIMAL(11,8),
                                 INDEX idx_date (receipt_date),
                                 INDEX idx_status (status),
                                 INDEX idx_kilogram (kilogram)
                             )",
                            "transport_requests" => @"CREATE TABLE transport_requests (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                request_number VARCHAR(50) UNIQUE NOT NULL,
                                mahberat_user_id INT NOT NULL,
                                mahberat_user_name VARCHAR(255) NOT NULL,
                                mahberat_id INT,
                                mahberat_name VARCHAR(255),
                                pickup_location TEXT NOT NULL,
                                destination TEXT NOT NULL,
                                passenger_item_details TEXT,
                                requested_date DATE NOT NULL,
                                requested_time VARCHAR(10),
                                special_instructions TEXT,
                                dispatcher_id INT,
                                dispatcher_name VARCHAR(255),
                                dispatcher_notes TEXT,
                                dispatcher_action_at DATETIME,
                                driver_id INT,
                                driver_name VARCHAR(255),
                                vehicle_id INT,
                                vehicle_plate VARCHAR(50),
                                driver_notes TEXT,
                                driver_action_at DATETIME,
                                pickup_confirmed_at DATETIME,
                                pickup_notes TEXT,
                                mahberat_pickup_approved_at DATETIME,
                                mahberat_pickup_notes TEXT,
                                receipt_photo_url VARCHAR(500),
                                digital_receipt_url VARCHAR(500),
                                receipt_notes TEXT,
                                receipt_submitted_at DATETIME,
                                mahberat_verified_at DATETIME,
                                mahberat_verification_notes TEXT,
                                staff_id INT,
                                staff_name VARCHAR(255),
                                transport_cost DECIMAL(10,2),
                                staff_notes TEXT,
                                staff_action_at DATETIME,
                                finance_staff_id INT,
                                finance_staff_name VARCHAR(255),
                                transaction_number VARCHAR(100),
                                payment_proof_url VARCHAR(500),
                                paid_at DATETIME,
                                payment_notes TEXT,
                                status VARCHAR(50) NOT NULL DEFAULT 'PendingDispatcher',
                                created_at DATETIME DEFAULT NOW(),
                                updated_at DATETIME DEFAULT NOW() ON UPDATE NOW(),
                                INDEX idx_status (status),
                                INDEX idx_mahberat_user (mahberat_user_id),
                                INDEX idx_driver (driver_id),
                                INDEX idx_dispatcher (dispatcher_id),
                                INDEX idx_created (created_at)
                            )",
                            "transport_request_logs" => @"CREATE TABLE transport_request_logs (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                transport_request_id INT NOT NULL,
                                from_status VARCHAR(50),
                                to_status VARCHAR(50) NOT NULL,
                                actor_user_id INT NOT NULL,
                                actor_name VARCHAR(255),
                                actor_role VARCHAR(100),
                                notes TEXT,
                                created_at DATETIME DEFAULT NOW(),
                                INDEX idx_request (transport_request_id),
                                INDEX idx_actor (actor_user_id)
                            )",
                            "transport_notifications" => @"CREATE TABLE transport_notifications (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                recipient_user_id INT NOT NULL,
                                transport_request_id INT NOT NULL,
                                request_number VARCHAR(50),
                                title VARCHAR(255) NOT NULL,
                                body TEXT,
                                notification_type VARCHAR(50) DEFAULT 'Info',
                                is_read TINYINT DEFAULT 0,
                                created_at DATETIME DEFAULT NOW(),
                                INDEX idx_recipient (recipient_user_id),
                                INDEX idx_request (transport_request_id),
                                INDEX idx_read (is_read)
                            )",
                            "requests" => @"CREATE TABLE requests (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                requestor_id INT NOT NULL,
                                requestor_role VARCHAR(50),
                                wereda_id INT,
                                mahberat_id INT,
                                request_type ENUM('Waste Material Pickup', 'Written Material', 'Vehicle Request', 'Driver Assignment', 'Equipment Request', 'Training Request', 'Report Approval', 'Other') NOT NULL,
                                description TEXT,
                                quantity DECIMAL(10,2),
                                urgency ENUM('Low', 'Medium', 'High', 'Critical') DEFAULT 'Medium',
                                requested_date_time DATETIME,
                                attachment_path VARCHAR(255),
                                assigned_to_id INT,
                                execution_status ENUM('Pending', 'Assigned', 'In Progress', 'Completed') DEFAULT 'Pending',
                                completion_date DATETIME,
                                execution_report_path VARCHAR(255),
                                is_closed BOOLEAN DEFAULT FALSE,
                                closed_by_id INT,
                                closure_date DATETIME,
                                closure_remarks TEXT,
                                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                INDEX idx_requestor (requestor_id),
                                INDEX idx_assigned (assigned_to_id),
                                INDEX idx_type (request_type)
                            )",
                            "request_approvals" => @"CREATE TABLE request_approvals (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                request_id INT NOT NULL,
                                level_1_status ENUM('Pending', 'Approved', 'Rejected') DEFAULT 'Pending',
                                level_1_by INT,
                                level_1_date DATETIME,
                                level_1_comments TEXT,
                                level_2_status ENUM('Pending', 'Approved', 'Rejected') DEFAULT 'Pending',
                                level_2_by INT,
                                level_2_date DATETIME,
                                level_2_comments TEXT,
                                level_3_status ENUM('Pending', 'Approved', 'Rejected') DEFAULT 'Pending',
                                level_3_by INT,
                                level_3_date DATETIME,
                                level_3_comments TEXT,
                                INDEX idx_request (request_id)
                            )",
                            _ => null
                        };

                        if (createTableSql != null)
                            await connection.ExecuteAsync(createTableSql);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Seeder] Error creating table {table}: {ex.Message}");
                }
            }
        }

        private static async Task SeedRolesAndPermissionsAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var roleDefinitions = new[]
            {
                new
                {
                    RoleName = "superadmin",
                    DisplayName = "Super Administrator",
                    Description = "Highest level access with complete system control. Responsible for overall system management, user administration, financial oversight, and policy enforcement.",
                    UsageContext = "Used for top-level administrative tasks across the entire Yeka Cleaning Management System. This role has unrestricted access to all modules, reports, and settings.",
                    PrimaryResponsibilities = "• Manage all system users (create, update, deactivate)\n• Configure system settings and permissions\n• View and generate all financial reports\n• Oversee capital and payroll management\n• Manage partnerships and company relationships\n• Create and publish system-wide announcements\n• Access all modules of all roles",
                    DailyActivities = "• Review system activity logs\n• Approve new user registrations\n• Monitor financial transactions\n• Generate daily/weekly reports\n• Update system policies and posts\n• Resolve escalated issues from other roles",
                    ReportsAccess = "All Reports: Financial, Payroll, User Activity, Receipts, Capital Transactions, PDF Generation",
                    ModulesAccess = "Complete access to: User Management, Financial Modules, Reports, Settings, All Dashboard Modules",
                    AccessLevel = 4,
                    CanCreateUsers = true,
                    CanViewFinancials = true,
                    CanManageDispatch = true,
                    CanViewPayroll = true,
                    CanManageStaff = true
                },
                new
                {
                    RoleName = "manager",
                    DisplayName = "Manager",
                    Description = "Middle management role responsible for overseeing operations, managing staff schedules, and monitoring service delivery quality.",
                    UsageContext = "Used by operational managers who coordinate between staff and clients, manage day-to-day operations, and ensure service quality standards are met.",
                    PrimaryResponsibilities = "• Manage assigned staff members\n• Review and approve service bookings\n• Monitor service completion and quality\n• Generate operational reports\n• Handle customer escalations\n• Coordinate resource allocation",
                    DailyActivities = "• Review daily schedule\n• Assign tasks to staff\n• Check booking confirmations\n• Monitor ongoing services\n• Address customer complaints\n• Prepare daily summary reports",
                    ReportsAccess = "Booking Reports, Staff Performance, Service Completion, Revenue (own team)",
                    ModulesAccess = "Staff Management, Bookings, Services, Reports, My Staff Dashboard",
                    AccessLevel = 3,
                    CanCreateUsers = false,
                    CanViewFinancials = true,
                    CanManageDispatch = false,
                    CanViewPayroll = false,
                    CanManageStaff = true
                },
                new
                {
                    RoleName = "staff",
                    DisplayName = "Staff Member",
                    Description = "Operational staff responsible for performing cleaning services, managing daily tasks, and reporting progress.",
                    UsageContext = "Used by cleaning personnel and administrative staff who execute daily operations and maintain service records.",
                    PrimaryResponsibilities = "• Perform assigned cleaning services\n• Maintain checklists and records\n• Submit service reports\n• Manage office resources\n• Update task status\n• Communicate with dispatchers and officers",
                    DailyActivities = "• View daily assigned tasks\n• Mark tasks as completed\n• Update service records\n• File reports and forms\n• Check dispatch schedules\n• Maintain agency documentation",
                    ReportsAccess = "Personal Task Reports, Agency Reports, Checklist Completion, Yaka Reports",
                    ModulesAccess = "Service Management, Office Plan, Library, Checklist, Reports, Dispatch Schedule, Receipts, Training, Gallery, Contact",
                    AccessLevel = 2,
                    CanCreateUsers = false,
                    CanViewFinancials = false,
                    CanManageDispatch = false,
                    CanViewPayroll = false,
                    CanManageStaff = false
                },
                new
                {
                    RoleName = "cleaner",
                    DisplayName = "Cleaner",
                    Description = "Field personnel responsible for executing cleaning tasks at client locations. Focus on hands-on service delivery.",
                    UsageContext = "Used by cleaning technicians who work on-site at client premises, following schedules and checklists.",
                    PrimaryResponsibilities = "• Perform residential/commercial cleaning\n• Follow safety procedures\n• Use equipment properly\n• Report completion status\n• Request supplies as needed\n• Maintain time logs",
                    DailyActivities = "• View job assignments\n• Check task details and locations\n• Start/complete jobs\n• Log hours worked\n• Report issues\n• Update job status",
                    ReportsAccess = "Personal Job History, Completed Tasks, Earnings",
                    ModulesAccess = "My Jobs, Schedule, Profile",
                    AccessLevel = 1,
                    CanCreateUsers = false,
                    CanViewFinancials = false,
                    CanManageDispatch = false,
                    CanViewPayroll = false,
                    CanManageStaff = false
                },
                new
                {
                    RoleName = "user",
                    DisplayName = "Registered User / Customer",
                    Description = "Individual customer who books cleaning services. Can view services, make bookings, and track service history.",
                    UsageContext = "Used by clients/customers who want to book cleaning services for their homes or businesses.",
                    PrimaryResponsibilities = "• Browse available services\n• Book cleaning appointments\n• View booking history\n• Manage profile\n• Make payments\n• Provide feedback",
                    DailyActivities = "• View available services\n• Book new services\n• Check upcoming appointments\n• View past services\n• Update personal information\n• Contact support",
                    ReportsAccess = "Personal Booking History, Payment Receipts",
                    ModulesAccess = "Services, Bookings, Profile, Gallery, Contact",
                    AccessLevel = 1,
                    CanCreateUsers = false,
                    CanViewFinancials = false,
                    CanManageDispatch = false,
                    CanViewPayroll = false,
                    CanManageStaff = false
                },
                new
                {
                    RoleName = "wereda_mahberat",
                    DisplayName = "Wereda Mahberat Officer",
                    Description = "Wereda-level administrative officer responsible for managing receipts, payroll, capital records, and local coordination.",
                    UsageContext = "Used by government or administrative officers at Wereda level who manage financial records, receipts, payroll for local operations, and liaison with dispatch.",
                    PrimaryResponsibilities = "• Record and manage monthly receipts\n• Process payroll for local staff\n• Maintain capital transaction records\n• Coordinate with dispatch officer\n• Generate wereda-level reports\n• Manage local gallery and contacts",
                    DailyActivities = "• Enter daily receipts\n• Review payroll records\n• Update capital ledger\n• Check meeting room schedules\n• Generate monthly reports\n• Contact dispatch for coordination",
                    ReportsAccess = "Monthly Receipts, Payroll Reports, Capital Statements, Mahberat Reports",
                    ModulesAccess = "Monthly Receipt, Payroll Report, Capital, Gallery, Contact, All Mahberat Report (read)",
                    AccessLevel = 2,
                    CanCreateUsers = false,
                    CanViewFinancials = true,
                    CanManageDispatch = false,
                    CanViewPayroll = true,
                    CanManageStaff = false
                },
                new
                {
                    RoleName = "dispatch_officer",
                    DisplayName = "Dispatch Officer",
                    Description = "Logistics coordinator responsible for managing meeting rooms, creating dispatches, generating reports, and coordinating field operations.",
                    UsageContext = "Used by dispatch team members who schedule deliveries, manage meeting facilities, and coordinate communication between offices and field staff.",
                    PrimaryResponsibilities = "• Schedule and manage meeting rooms\n• Create and track dispatches\n• Generate mahberat reports\n• Coordinate with drivers and officers\n• Maintain dispatch records\n• Handle communication logistics",
                    DailyActivities = "• Check dispatch schedule\n• Create new dispatches\n• Assign drivers to tasks\n• Update dispatch status\n• Reserve meeting rooms\n• Generate daily reports",
                    ReportsAccess = "Dispatch Reports, Meeting Room Usage, All Mahberat Reports, Activity Logs",
                    ModulesAccess = "Meeting Room, Create Dispatch, All Mahberat Report, Gallery, Contact",
                    AccessLevel = 3,
                    CanCreateUsers = false,
                    CanViewFinancials = false,
                    CanManageDispatch = true,
                    CanViewPayroll = false,
                    CanManageStaff = false
                },
                new
                {
                    RoleName = "driver",
                    DisplayName = "Driver",
                    Description = "Vehicle operator responsible for transporting goods and personnel according to dispatch instructions. Track location and tasks.",
                    UsageContext = "Used by drivers who carry out dispatch assignments, manage delivery tasks, and report location and status updates.",
                   PrimaryResponsibilities = "• Follow dispatch instructions\n• Complete delivery tasks\n• Update task status in real-time\n• Maintain vehicle condition\n• Report location and ETA\n• Communicate with dispatch",
                    DailyActivities = "• View assigned tasks\n• Check pickup/dropoff details\n• Update task status (start/complete)\n• Report location updates\n• View contact information\n• Log completed deliveries",
                    ReportsAccess = "Personal Task History, Location Log, Delivery Performance",
                    ModulesAccess = "Dashboard, Location Tracking, Task Management, Contact",
                    AccessLevel = 1,
                    CanCreateUsers = false,
                    CanViewFinancials = false,
                    CanManageDispatch = false,
                    CanViewPayroll = false,
                    CanManageStaff = false
                }
            };

            foreach (var role in roleDefinitions)
            {
                var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM role_definitions WHERE role_name = @RoleName",
                    new { role.RoleName });

                if (existing == null)
                {
                    Console.WriteLine($"[Seeder] Inserting role definition: {role.RoleName}");
                    await connection.ExecuteAsync(@"
                        INSERT INTO role_definitions 
                        (role_name, display_name, description, usage_context, primary_responsibilities,
                         daily_activities, reports_access, modules_access, access_level,
                         can_create_users, can_view_financials, can_manage_dispatch,
                         can_view_payroll, can_manage_staff, created_at)
                        VALUES
                        (@RoleName, @DisplayName, @Description, @UsageContext, @PrimaryResponsibilities,
                         @DailyActivities, @ReportsAccess, @ModulesAccess, @AccessLevel,
                         @CanCreateUsers, @CanViewFinancials, @CanManageDispatch,
                         @CanViewPayroll, @CanManageStaff, NOW())",
                        role);
                }
            }
        }

        private static async Task SeedSampleUsersAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var sampleUsers = new[]
            {
                new { Name = "Super Admin", Email = "superadmin@yeka.et", Password = "admin123", Role = "superadmin", Phone = "+251911234567" },
                new { Name = "Operations Manager", Email = "manager@yeka.et", Password = "manager123", Role = "manager", Phone = "+251911234568" },
                new { Name = "Staff Member A", Email = "staff@yeka.et", Password = "staff123", Role = "staff", Phone = "+251911234569" },
                new { Name = "Cleaner B", Email = "cleaner@yeka.et", Password = "clean123", Role = "cleaner", Phone = "+251911234570" },
                new { Name = "Customer X", Email = "user@client.et", Password = "user123", Role = "user", Phone = "+251911234571" },
                new { Name = "Wereda Officer - Addis", Email = "wereda@addis.gov.et", Password = "wereda123", Role = "wereda_mahberat", Phone = "+251911234572" },
                new { Name = "Dispatch Lead", Email = "dispatch@yeka.et", Password = "dispatch123", Role = "dispatch_officer", Phone = "+251911234573" },
                new { Name = "Driver - Vehicle 01", Email = "driver1@yeka.et", Password = "driver123", Role = "driver", Phone = "+251911234574" },
                new { Name = "Test Driver", Email = "driver@yeka.com", Password = "driver123", Role = "driver", Phone = "+251911234575" },
                new { Name = "Test Outsource", Email = "outsource@yeka.com", Password = "outsource123", Role = "outsource", Phone = "+251911234576" },
                new { Name = "Outsource Rep", Email = "outsource@yeka.et", Password = "outsource123", Role = "outsource", Phone = "+251911234581" },
                new { Name = "Private Company Rep", Email = "private@yeka.et", Password = "private123", Role = "PrivateCompanyRep", Phone = "+251911234582" },
                // Transport workflow users
                new { Name = "Mahberat User - Bole", Email = "mahberat@yeka.et", Password = "mahberat123", Role = "WeredaMahberat", Phone = "+251911234577" },
                new { Name = "Dispatcher - Transport", Email = "dispatcher@yeka.et", Password = "dispatch123", Role = "DispatchOfficer", Phone = "+251911234578" },
                new { Name = "Driver - Transport 01", Email = "tdriver1@yeka.et", Password = "driver123", Role = "Driver", Phone = "+251911234579" },
                new { Name = "Finance Staff", Email = "finance@yeka.et", Password = "finance123", Role = "Staff", Phone = "+251911234580" }
            };

            foreach (var user in sampleUsers)
            {
                var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM users WHERE email = @Email",
                    new { user.Email });

                if (existing == null)
                {
                    Console.WriteLine($"[Seeder] Creating sample user: {user.Name} ({user.Role})");
                    await connection.ExecuteAsync(@"
                        INSERT INTO users (name, email, password, role, phone, is_active, created_at)
                        VALUES (@Name, @Email, @Password, @Role, @Phone, TRUE, NOW())",
                        user);
                }
            }

            // ── Link private@yeka.et to a company profile if missing ──
            try
            {
                var privUser = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id FROM users WHERE email = 'private@yeka.et'");
                if (privUser != null)
                {
                    int privId = (int)privUser.id;
                    var privCompany = await connection.QueryFirstOrDefaultAsync<int?>(
                        "SELECT id FROM private_cleaning_companies WHERE rep_user_id = @Id",
                        new { Id = privId });
                    if (privCompany == null)
                    {
                        Console.WriteLine("[Seeder] Linking private@yeka.et to sample private company...");
                        await connection.ExecuteAsync(@"
                            INSERT INTO private_cleaning_companies
                              (company_name, license_number, contact_person, phone, email,
                               address, services_offered, status, is_active, rep_user_id, created_at, updated_at)
                            VALUES
                              ('Sample Private Cleaners', 'LIC-SAMPLE-01', 'Private Company Rep',
                               '+251911234582', 'private@yeka.et',
                               'Yeka Sub-city, Addis Ababa', 'Waste collection, Street sweeping',
                               'Active', 1, @RepId, NOW(), NOW())",
                            new { RepId = privId });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seeder] Private company seed warning: {ex.Message}");
            }

            // ── Auto-create companies for ALL PrivateCompanyRep users that have none ──
            try
            {
                var unlinkedReps = await connection.QueryAsync<dynamic>(
                    @"SELECT u.id, u.name, u.email, u.phone
                      FROM users u
                      WHERE u.role = 'PrivateCompanyRep'
                        AND u.is_active = 1
                        AND NOT EXISTS (
                            SELECT 1 FROM private_cleaning_companies p
                            WHERE p.rep_user_id = u.id
                               OR p.email = u.email
                        )");

                foreach (var rep in unlinkedReps)
                {
                    Console.WriteLine($"[Seeder] Auto-creating company for unlinked PrivateCompanyRep: {rep.name} ({rep.email})");
                    await connection.ExecuteAsync(
                        @"INSERT INTO private_cleaning_companies
                            (company_name, contact_person, phone, email,
                             license_number, address, services_offered,
                             status, is_active, rep_user_id, created_at, updated_at)
                          VALUES
                            (@cn, @cp, @ph, @em, '', '', '',
                             'Active', 1, @uid, NOW(), NOW())",
                        new {
                            cn  = (string)rep.name + "'s Company",
                            cp  = (string)rep.name,
                            ph  = (string)(rep.phone ?? ""),
                            em  = (string)rep.email,
                            uid = (int)rep.id
                        });
                }

                if (((IEnumerable<dynamic>)unlinkedReps).Any())
                    Console.WriteLine($"[Seeder] Auto-created companies for {((IEnumerable<dynamic>)unlinkedReps).Count()} unlinked rep(s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seeder] Auto-create company warning: {ex.Message}");
            }
        }

        private static async Task SeedSampleDataAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var permissions = new[]
            {
                new { RoleName = "superadmin", ModuleName = "users", PermissionType = "CRUD", Description = "Full user management", IsAllowed = true },
                new { RoleName = "superadmin", ModuleName = "receipts", PermissionType = "CRUD", Description = "All receipt operations", IsAllowed = true },
                new { RoleName = "superadmin", ModuleName = "payroll", PermissionType = "CRUD", Description = "Payroll management", IsAllowed = true },
                new { RoleName = "superadmin", ModuleName = "capital", PermissionType = "CRUD", Description = "Capital transactions", IsAllowed = true },
                new { RoleName = "superadmin", ModuleName = "reports", PermissionType = "View", Description = "All reports", IsAllowed = true },
                new { RoleName = "superadmin", ModuleName = "posts", PermissionType = "CRUD", Description = "Post management", IsAllowed = true },
                new { RoleName = "wereda_mahberat", ModuleName = "monthly_receipts", PermissionType = "CRUD", Description = "Monthly receipt management", IsAllowed = true },
                new { RoleName = "wereda_mahberat", ModuleName = "payroll", PermissionType = "View", Description = "View payroll", IsAllowed = true },
                new { RoleName = "wereda_mahberat", ModuleName = "capital", PermissionType = "CRUD", Description = "Capital records", IsAllowed = true },
                new { RoleName = "wereda_mahberat", ModuleName = "reports", PermissionType = "View", Description = "View mahberat reports", IsAllowed = true },
                new { RoleName = "dispatch_officer", ModuleName = "meeting_rooms", PermissionType = "CRUD", Description = "Meeting room management", IsAllowed = true },
                new { RoleName = "dispatch_officer", ModuleName = "dispatches", PermissionType = "CRUD", Description = "Create and manage dispatches", IsAllowed = true },
                new { RoleName = "dispatch_officer", ModuleName = "reports", PermissionType = "View", Description = "View all mahberat reports", IsAllowed = true },
                new { RoleName = "staff", ModuleName = "checklist", PermissionType = "CRUD", Description = "Manage personal checklist", IsAllowed = true },
                new { RoleName = "staff", ModuleName = "reports", PermissionType = "Create", Description = "Create agency and yaka reports", IsAllowed = true }
            };

            foreach (var perm in permissions)
            {
                var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM role_permissions WHERE role_name = @RoleName AND module_name = @ModuleName AND permission_type = @PermissionType",
                    perm);

                if (existing == null)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO role_permissions (role_name, module_name, permission_type, description, is_allowed, created_at)
                        VALUES (@RoleName, @ModuleName, @PermissionType, @Description, @IsAllowed, NOW())",
                        perm);
                }
            }

            // Seed sample outsource companies if none exist
            var sampleCompanies = new[]
            {
                new { Name = "Addis Cleaning PLC", ContactPerson = "Abebe Kebede", Phone = "+251911234567", Email = "info@addiscleaning.et", LicenseNumber = "BL-1001", ContractStartDate = new DateTime(2025, 1, 1), ServicesProvided = "General cleaning, office cleaning, waste management" },
                new { Name = "Capital Cleaning Services", ContactPerson = "Tigist Haile", Phone = "+251911234568", Email = "contact@capital.et", LicenseNumber = "BL-1002", ContractStartDate = new DateTime(2025, 3, 15), ServicesProvided = "Deep cleaning, window cleaning, carpet cleaning" },
                new { Name = "Sparkle Cleaners", ContactPerson = "Marta Alemu", Phone = "+251911234569", Email = "info@sparkle.et", LicenseNumber = "BL-1003", ContractStartDate = new DateTime(2025, 5, 1), ServicesProvided = "Residential cleaning, office cleaning" }
            };

            foreach (var comp in sampleCompanies)
            {
                var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM outsource_companies WHERE company_name = @Name",
                    new { comp.Name });

                if (existing == null)
                {
                    Console.WriteLine($"[Seeder] Creating sample outsource company: {comp.Name}");
                    await connection.ExecuteAsync(@"
                        INSERT INTO outsource_companies 
                        (company_name, contact_person, phone, email, license_number, contract_start_date, status, services_provided, created_at, updated_at)
                        VALUES (@Name, @ContactPerson, @Phone, @Email, @LicenseNumber, @ContractStartDate, 'Active', @ServicesProvided, NOW(), NOW())",
                        comp);
                }
            }

            // Seed sample trainings
            var staffUser = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id FROM users WHERE email = 'staff@yeka.et'");
            int? staffId = staffUser != null ? (int?)staffUser.id : null;

            var sampleTrainings = new[]
            {
                new { Title = "Safety Procedures", Trainer = "Sarah Johnson", Description = "Basic safety procedures for cleaning staff", StartDate = DateTime.Now.AddDays(5), EndDate = DateTime.Now.AddDays(7), Location = "Training Room A", Participants = 12, Status = "Upcoming", Materials = "PDF, Video", AssignedToUserId = (int?)null, Category = "Safety" },
                new { Title = "Equipment Training", Trainer = "Mike Chen", Description = "How to use advanced cleaning equipment", StartDate = DateTime.Now.AddDays(10), EndDate = DateTime.Now.AddDays(12), Location = "Lab 1", Participants = 15, Status = "Upcoming", Materials = "Video", AssignedToUserId = staffId, Category = "Equipment" },
                new { Title = "Customer Service", Trainer = "Lisa Brown", Description = "Interacting with clients and handling complaints", StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(-3), Location = "Conference Hall", Participants = 18, Status = "In Progress", Materials = "PDF, Exam", AssignedToUserId = (int?)null, Category = "Customer Service" }
            };

            foreach (var tr in sampleTrainings)
            {
                var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM trainings WHERE title = @Title",
                    new { tr.Title });

                if (existing == null)
                {
                    Console.WriteLine($"[Seeder] Creating sample training: {tr.Title}");
                    await connection.ExecuteAsync(@"
                        INSERT INTO trainings 
                        (title, trainer, description, start_date, end_date, location, participants, status, materials, assigned_to_user_id, category, created_at)
                        VALUES (@Title, @Trainer, @Description, @StartDate, @EndDate, @Location, @Participants, @Status, @Materials, @AssignedToUserId, @Category, NOW())",
                        tr);
                }
            }

            // Seed sample requests for WMRAS
            var sampleRequests = new[]
            {
                new { RequestorId = 11, RequestorRole = "WeredaMahberat", RequestType = "Waste Material Pickup", Description = "Need pickup at Bole area", Quantity = 50.5m, Urgency = "High" },
                new { RequestorId = 11, RequestorRole = "WeredaMahberat", RequestType = "Written Material", Description = "Derite forms supply needed", Quantity = 100m, Urgency = "Medium" },
                new { RequestorId = 4, RequestorRole = "cleaner", RequestType = "Equipment Request", Description = "Need new brooms and mops", Quantity = 5m, Urgency = "Low" }
            };

            foreach (var req in sampleRequests)
            {
                var existingReq = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM requests WHERE description = @Description",
                    new { req.Description });

                if (existingReq == null)
                {
                    Console.WriteLine($"[Seeder] Creating sample request: {req.RequestType}");
                    var reqId = await connection.ExecuteScalarAsync<int>(@"
                        INSERT INTO requests (requestor_id, requestor_role, request_type, description, quantity, urgency, execution_status, is_closed, created_at)
                        VALUES (@RequestorId, @RequestorRole, @RequestType, @Description, @Quantity, @Urgency, 'Pending', FALSE, NOW());
                        SELECT LAST_INSERT_ID();", req);

                    await connection.ExecuteAsync(@"
                        INSERT INTO request_approvals (request_id, level_1_status, level_2_status, level_3_status)
                        VALUES (@RequestId, 'Pending', 'Pending', 'Pending')", new { RequestId = reqId });
                }
            }
        }
    }
}
