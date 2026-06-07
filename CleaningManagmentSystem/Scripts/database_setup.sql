-- Yeka Cleaning Management System
-- Complete Database Schema with Role Definitions and Sample Data
-- Created: 2026-04-19
-- Version: 1.0

-- ============================================
-- SECTION 1: CORE TABLES
-- ============================================

-- Users table (existing)
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    role VARCHAR(100) NOT NULL DEFAULT 'user',
    phone VARCHAR(50),
    email_notifications BOOLEAN DEFAULT TRUE,
    sms_notifications BOOLEAN DEFAULT TRUE,
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_by INT,
    created_at DATETIME DEFAULT NOW(),
    updated_at DATETIME DEFAULT NOW() ON UPDATE NOW()
);

-- Services table (existing)
CREATE TABLE IF NOT EXISTS services (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(10,2),
    duration INT,
    created_at DATETIME DEFAULT NOW()
);

-- Bookings table (existing)
CREATE TABLE IF NOT EXISTS bookings (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT,
    service_id INT,
    booking_date DATE,
    booking_time TIME,
    address TEXT,
    status VARCHAR(50),
    notes TEXT,
    created_at DATETIME DEFAULT NOW()
);

-- ============================================
-- SECTION 2: SUPER ADMIN MODULES
-- ============================================

-- Outsource Companies
CREATE TABLE IF NOT EXISTS outsource_companies (
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
);

-- Private Cleaning Companies
CREATE TABLE IF NOT EXISTS private_cleaning_companies (
    id INT AUTO_INCREMENT PRIMARY KEY,
    company_name VARCHAR(255) NOT NULL,
    license_number VARCHAR(100),
    contact_person VARCHAR(255),
    phone VARCHAR(50),
    email VARCHAR(255),
    address TEXT,
    services_offered TEXT,
    status VARCHAR(50) DEFAULT 'Active',
    is_active BOOLEAN DEFAULT TRUE,
    rep_user_id INT,
    deleted_at DATETIME NULL,
    created_at DATETIME DEFAULT NOW(),
    updated_at DATETIME DEFAULT NOW()
);

-- Receipts
CREATE TABLE IF NOT EXISTS receipts (
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
);

-- Payroll
CREATE TABLE IF NOT EXISTS payroll (
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
);

-- Capital Transactions
CREATE TABLE IF NOT EXISTS capital_transactions (
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
);

-- Posts (News/Announcements)
CREATE TABLE IF NOT EXISTS posts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255),
    category VARCHAR(100),
    content LONGTEXT,
    author VARCHAR(255),
    author_id INT,
    status VARCHAR(50),
    image_url VARCHAR(500),
    created_at DATETIME DEFAULT NOW(),
    updated_at DATETIME DEFAULT NOW()
);

-- ============================================
-- SECTION 3: WEREDA MAHBERAT MODULES
-- ============================================

-- Monthly Receipts
CREATE TABLE IF NOT EXISTS monthly_receipts (
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
);

-- ============================================
-- SECTION 4: DISPATCH OFFICER MODULES
-- ============================================

-- Meeting Rooms
CREATE TABLE IF NOT EXISTS meeting_rooms (
    id INT AUTO_INCREMENT PRIMARY KEY,
    room_name VARCHAR(255) NOT NULL,
    capacity INT,
    location VARCHAR(255),
    equipment TEXT,
    is_available BOOLEAN DEFAULT TRUE,
    status VARCHAR(50),
    created_at DATETIME DEFAULT NOW()
);

-- Mahberat Reports
CREATE TABLE IF NOT EXISTS mahberat_reports (
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
);

-- Dispatches
CREATE TABLE IF NOT EXISTS dispatches (
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
);

-- ============================================
-- SECTION 5: STAFF MODULES
-- ============================================

-- Office Plans
CREATE TABLE IF NOT EXISTS office_plans (
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
);

-- Library Items
CREATE TABLE IF NOT EXISTS library_items (
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
);

-- Agency Reports
CREATE TABLE IF NOT EXISTS agency_reports (
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
);

-- Yaka Reports
CREATE TABLE IF NOT EXISTS yaka_reports (
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
);

-- Subcity Officers
CREATE TABLE IF NOT EXISTS subcity_officers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(50),
    subcity VARCHAR(255),
    position VARCHAR(255),
    responsibilities TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT NOW()
);

-- Subcity Drivers
CREATE TABLE IF NOT EXISTS subcity_drivers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255),
    license_number VARCHAR(100),
    phone VARCHAR(50),
    subcity VARCHAR(255),
    vehicle_assigned VARCHAR(255),
    is_available BOOLEAN DEFAULT TRUE,
    status VARCHAR(50),
    created_at DATETIME DEFAULT NOW()
);

-- Wereda Officers
CREATE TABLE IF NOT EXISTS wereda_officers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(50),
    wereda VARCHAR(255),
    position VARCHAR(255),
    responsibilities TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT NOW()
);

-- Dispatch Schedules
CREATE TABLE IF NOT EXISTS dispatch_schedules (
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
);

-- Outsource Receipts
CREATE TABLE IF NOT EXISTS outsource_receipts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    receipt_number VARCHAR(100) UNIQUE NOT NULL,
    company_id INT,
    company_name VARCHAR(255),
    service_type VARCHAR(255),
    amount DECIMAL(10,2),
    service_date DATE,
    payment_status VARCHAR(50),
    notes TEXT,
    created_at DATETIME DEFAULT NOW()
);

-- Office Recognitions (Awards)
CREATE TABLE IF NOT EXISTS office_recognitions (
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
);

-- Trainings
CREATE TABLE IF NOT EXISTS trainings (
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
    created_at DATETIME DEFAULT NOW()
);

-- ============================================
-- SECTION 6: DRIVER MODULES
-- ============================================

-- Driver Locations
CREATE TABLE IF NOT EXISTS driver_locations (
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
);

-- Delivery Tasks
CREATE TABLE IF NOT EXISTS delivery_tasks (
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
);

-- Contacts (for Drivers)
CREATE TABLE IF NOT EXISTS contacts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    driver_id INT,
    name VARCHAR(255),
    phone VARCHAR(50),
    email VARCHAR(255),
    company VARCHAR(255),
    notes TEXT,
    created_at DATETIME DEFAULT NOW(),
    updated_at DATETIME DEFAULT NOW()
);

-- Messages (SMS/Communication log)
CREATE TABLE IF NOT EXISTS messages (
    id INT AUTO_INCREMENT PRIMARY KEY,
    sender_id INT,
    recipient_phone VARCHAR(50),
    content TEXT,
    sent_at DATETIME DEFAULT NOW(),
    status VARCHAR(50) DEFAULT 'Sent'
);

-- ============================================
-- SECTION 7: SHARED MODULES
-- ============================================

-- Contact Messages (from website or internal)
CREATE TABLE IF NOT EXISTS contact_messages (
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
);

-- User Settings
CREATE TABLE IF NOT EXISTS user_settings (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT UNIQUE,
    setting_key VARCHAR(100),
    setting_value TEXT,
    description TEXT,
    updated_at DATETIME DEFAULT NOW() ON UPDATE NOW()
);

-- Gallery
CREATE TABLE IF NOT EXISTS gallery (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255),
    description TEXT,
    image_url VARCHAR(500),
    category VARCHAR(100),
    views INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT NOW()
);

-- Checklists
CREATE TABLE IF NOT EXISTS checklists (
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
);

-- ============================================
-- SECTION 8: ROLE MANAGEMENT TABLES
-- ============================================

-- Role Definitions (comprehensive role metadata)
CREATE TABLE IF NOT EXISTS role_definitions (
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
);

-- Role Permissions (granular module permissions)
CREATE TABLE IF NOT EXISTS role_permissions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(100) NOT NULL,
    module_name VARCHAR(255) NOT NULL,
    permission_type VARCHAR(50) NOT NULL,
    is_allowed BOOLEAN DEFAULT TRUE,
    description TEXT,
    created_at DATETIME DEFAULT NOW(),
    UNIQUE KEY unique_permission (role_name, module_name, permission_type)
);

-- Role Activity Logs (audit trail)
CREATE TABLE IF NOT EXISTS role_activity_logs (
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
);

-- System Usage Analytics
CREATE TABLE IF NOT EXISTS system_usage_analytics (
    id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(100),
    metric_name VARCHAR(255),
    metric_value INT,
    period VARCHAR(50),
    record_date DATE,
    notes TEXT,
    created_at DATETIME DEFAULT NOW(),
    INDEX idx_role_period (role_name, period, record_date)
);

-- ============================================
-- SECTION 9: SAMPLE DATA INSERTION
-- ============================================

-- Insert Sample Users (default passwords listed - CHANGE IN PRODUCTION)
INSERT IGNORE INTO users (name, email, password, role, phone, is_active, created_at) VALUES
('Super Admin', 'superadmin@yeka.et', 'admin123', 'superadmin', '+251911234567', TRUE, NOW()),
('Operations Manager', 'manager@yeka.et', 'manager123', 'manager', '+251911234568', TRUE, NOW()),
('Staff Member A', 'staff@yeka.et', 'staff123', 'staff', '+251911234569', TRUE, NOW()),
('Cleaner B', 'cleaner@yeka.et', 'clean123', 'cleaner', '+251911234570', TRUE, NOW()),
('Customer X', 'user@client.et', 'user123', 'user', '+251911234571', TRUE, NOW()),
('Wereda Officer - Addis', 'wereda@addis.gov.et', 'wereda123', 'wereda_mahberat', '+251911234572', TRUE, NOW()),
('Dispatch Lead', 'dispatch@yeka.et', 'dispatch123', 'dispatch_officer', '+251911234573', TRUE, NOW()),
('Driver - Vehicle 01', 'driver1@yeka.et', 'driver123', 'driver', '+251911234574', TRUE, NOW());

-- Insert Role Definitions (detailed usage descriptions)
INSERT IGNORE INTO role_definitions (role_name, display_name, description, usage_context, primary_responsibilities, daily_activities, reports_access, modules_access, access_level, can_create_users, can_view_financials, can_manage_dispatch, can_view_payroll, can_manage_staff, created_at) VALUES
('superadmin', 'Super Administrator', 'Highest level access with complete system control. Responsible for overall system management, user administration, financial oversight, and policy enforcement.',
 'Used for top-level administrative tasks across the entire Yeka Cleaning Management System. This role has unrestricted access to all modules, reports, and settings.',
 '• Manage all system users (create, update, deactivate)\n• Configure system settings and permissions\n• View and generate all financial reports\n• Oversee capital and payroll management\n• Manage partnerships and company relationships\n• Create and publish system-wide announcements\n• Access all modules of all roles',
 '• Review system activity logs\n• Approve new user registrations\n• Monitor financial transactions\n• Generate daily/weekly reports\n• Update system policies and posts\n• Resolve escalated issues from other roles',
 'All Reports: Financial, Payroll, User Activity, Receipts, Capital Transactions, PDF Generation',
 'Complete access to: User Management, Financial Modules, Reports, Settings, All Dashboard Modules',
 4, TRUE, TRUE, TRUE, TRUE, TRUE, NOW()),

('manager', 'Manager', 'Middle management role responsible for overseeing operations, managing staff schedules, and monitoring service delivery quality.',
 'Used by operational managers who coordinate between staff and clients, manage day-to-day operations, and ensure service quality standards are met.',
 '• Manage assigned staff members\n• Review and approve service bookings\n• Monitor service completion and quality\n• Generate operational reports\n• Handle customer escalations\n• Coordinate resource allocation',
 '• Review daily schedule\n• Assign tasks to staff\n• Check booking confirmations\n• Monitor ongoing services\n• Address customer complaints\n• Prepare daily summary reports',
 'Booking Reports, Staff Performance, Service Completion, Revenue (own team)',
 'Staff Management, Bookings, Services, Reports, My Staff Dashboard',
 3, FALSE, TRUE, FALSE, FALSE, TRUE, NOW()),

('staff', 'Staff Member', 'Operational staff responsible for performing cleaning services, managing daily tasks, and reporting progress.',
 'Used by cleaning personnel and administrative staff who execute daily operations and maintain service records.',
 '• Perform assigned cleaning services\n• Maintain checklists and records\n• Submit service reports\n• Manage office resources\n• Update task status\n• Communicate with dispatchers and officers',
 '• View daily assigned tasks\n• Mark tasks as completed\n• Update service records\n• File reports and forms\n• Check dispatch schedules\n• Maintain agency documentation',
 'Personal Task Reports, Agency Reports, Checklist Completion, Yaka Reports',
 'Service Management, Office Plan, Library, Checklist, Reports, Dispatch Schedule, Receipts, Training, Gallery, Contact',
 2, FALSE, FALSE, FALSE, FALSE, FALSE, NOW()),

('cleaner', 'Cleaner', 'Field personnel responsible for executing cleaning tasks at client locations. Focus on hands-on service delivery.',
 'Used by cleaning technicians who work on-site at client premises, following schedules and checklists.',
 '• Perform residential/commercial cleaning\n• Follow safety procedures\n• Use equipment properly\n• Report completion status\n• Request supplies as needed\n• Maintain time logs',
 '• View job assignments\n• Check task details and locations\n• Start/complete jobs\n• Log hours worked\n• Report issues\n• Update job status',
 'Personal Job History, Completed Tasks, Earnings',
 'My Jobs, Schedule, Profile',
 1, FALSE, FALSE, FALSE, FALSE, FALSE, NOW()),

('user', 'Registered User / Customer', 'Individual customer who books cleaning services. Can view services, make bookings, and track service history.',
 'Used by clients/customers who want to book cleaning services for their homes or businesses.',
 '• Browse available services\n• Book cleaning appointments\n• View booking history\n• Manage profile\n• Make payments\n• Provide feedback',
 '• View available services\n• Book new services\n• Check upcoming appointments\n• View past services\n• Update personal information\n• Contact support',
 'Personal Booking History, Payment Receipts',
 'Services, Bookings, Profile, Gallery, Contact',
 1, FALSE, FALSE, FALSE, FALSE, FALSE, NOW()),

('wereda_mahberat', 'Wereda Mahberat Officer', 'Wereda-level administrative officer responsible for managing receipts, payroll, capital records, and local coordination.',
 'Used by government or administrative officers at Wereda level who manage financial records, receipts, payroll for local operations, and liaison with dispatch.',
 '• Record and manage monthly receipts\n• Process payroll for local staff\n• Maintain capital transaction records\n• Coordinate with dispatch officer\n• Generate wereda-level reports\n• Manage local gallery and contacts',
 '• Enter daily receipts\n• Review payroll records\n• Update capital ledger\n• Check meeting room schedules\n• Generate monthly reports\n• Contact dispatch for coordination',
 'Monthly Receipts, Payroll Reports, Capital Statements, Mahberat Reports',
 'Monthly Receipt, Payroll Report, Capital, Gallery, Contact, All Mahberat Report (read)',
 2, FALSE, TRUE, FALSE, TRUE, FALSE, NOW()),

('dispatch_officer', 'Dispatch Officer', 'Logistics coordinator responsible for managing meeting rooms, creating dispatches, generating reports, and coordinating field operations.',
 'Used by dispatch team members who schedule deliveries, manage meeting facilities, and coordinate communication between offices and field staff.',
 '• Schedule and manage meeting rooms\n• Create and track dispatches\n• Generate mahberat reports\n• Coordinate with drivers and officers\n• Maintain dispatch records\n• Handle communication logistics',
 '• Check dispatch schedule\n• Create new dispatches\n• Assign drivers to tasks\n• Update dispatch status\n• Reserve meeting rooms\n• Generate daily reports',
 'Dispatch Reports, Meeting Room Usage, All Mahberat Reports, Activity Logs',
 'Meeting Room, Create Dispatch, All Mahberat Report, Gallery, Contact',
 3, FALSE, FALSE, TRUE, FALSE, FALSE, NOW()),

('driver', 'Driver', 'Vehicle operator responsible for transporting goods and personnel according to dispatch instructions. Track location and tasks.',
 'Used by drivers who carry out dispatch assignments, manage delivery tasks, and report location and status updates.',
 '• Follow dispatch instructions\n• Complete delivery tasks\n• Update task status in real-time\n• Maintain vehicle condition\n• Report location and ETA\n• Communicate with dispatch',
 '• View assigned tasks\n• Check pickup/dropoff details\n• Update task status (start/complete)\n• Report location updates\n• View contact information\n• Log completed deliveries',
 'Personal Task History, Location Log, Delivery Performance',
 'Dashboard, Location Tracking, Task Management, Contact',
 1, FALSE, FALSE, FALSE, FALSE, FALSE, NOW());

-- Insert Default Permissions for Each Role
INSERT IGNORE INTO role_permissions (role_name, module_name, permission_type, is_allowed, description, created_at) VALUES
-- SuperAdmin permissions
('superadmin', 'users', 'CRUD', TRUE, 'Full user management', NOW()),
('superadmin', 'receipts', 'CRUD', TRUE, 'All receipt operations', NOW()),
('superadmin', 'payroll', 'CRUD', TRUE, 'Payroll management', NOW()),
('superadmin', 'capital', 'CRUD', TRUE, 'Capital transactions', NOW()),
('superadmin', 'reports', 'View', TRUE, 'All reports', NOW()),
('superadmin', 'posts', 'CRUD', TRUE, 'Post management', NOW()),
('superadmin', 'role_definitions', 'CRUD', TRUE, 'Role management', NOW()),

-- WeredaMahberat permissions
('wereda_mahberat', 'monthly_receipts', 'CRUD', TRUE, 'Monthly receipt management', NOW()),
('wereda_mahberat', 'payroll', 'View', TRUE, 'View payroll', NOW()),
('wereda_mahberat', 'capital', 'CRUD', TRUE, 'Capital records', NOW()),
('wereda_mahberat', 'reports', 'View', TRUE, 'View mahberat reports', NOW()),
('wereda_mahberat', 'dispatches', 'View', TRUE, 'View dispatches', NOW()),

-- DispatchOfficer permissions
('dispatch_officer', 'meeting_rooms', 'CRUD', TRUE, 'Meeting room management', NOW()),
('dispatch_officer', 'dispatches', 'CRUD', TRUE, 'Create and manage dispatches', NOW()),
('dispatch_officer', 'reports', 'View', TRUE, 'View all mahberat reports', NOW()),
('dispatch_officer', 'drivers', 'View', TRUE, 'View driver list', NOW()),

-- Staff permissions
('staff', 'checklist', 'CRUD', TRUE, 'Manage personal checklist', NOW()),
('staff', 'reports', 'Create', TRUE, 'Create agency and yaka reports', NOW()),
('staff', 'library', 'View', TRUE, 'Access library resources', NOW()),
('staff', 'training', 'View', TRUE, 'View training materials', NOW()),
('staff', 'office_plans', 'View', TRUE, 'View office layouts', NOW()),
('staff', 'gallery', 'View', TRUE, 'View gallery', NOW()),

-- Driver permissions
('driver', 'tasks', 'View', TRUE, 'View assigned tasks', NOW()),
('driver', 'tasks', 'Update', TRUE, 'Update task status', NOW()),
('driver', 'location', 'Update', TRUE, 'Update location', NOW()),
('driver', 'contacts', 'View', TRUE, 'View contact list', NOW()),

-- Cleaner permissions
('cleaner', 'tasks', 'View', TRUE, 'View own jobs', NOW()),
('cleaner', 'bookings', 'View', TRUE, 'View own schedule', NOW()),

-- User/Customer permissions
('user', 'services', 'View', TRUE, 'Browse services', NOW()),
('user', 'bookings', 'CRUD', TRUE, 'Manage own bookings', NOW()),
('user', 'gallery', 'View', TRUE, 'View gallery', NOW()),
('user', 'contact', 'Create', TRUE, 'Send messages', NOW());

-- Add price column to staff_receipts table (run after table creation)
ALTER TABLE staff_receipts ADD COLUMN price DECIMAL(10,2) DEFAULT 0.00 AFTER kilogram;

-- ============================================
-- SECTION 10: DATA MIGRATION
-- ============================================

-- Set default notification preferences for existing users where NULL
UPDATE users SET email_notifications = TRUE WHERE email_notifications IS NULL;
UPDATE users SET sms_notifications = TRUE WHERE sms_notifications IS NULL;

-- Note: Foreign key constraints can be added after all tables exist:
-- ALTER TABLE bookings ADD CONSTRAINT fk_bookings_user FOREIGN KEY (user_id) REFERENCES users(id);
-- ALTER TABLE bookings ADD CONSTRAINT fk_bookings_service FOREIGN KEY (service_id) REFERENCES services(id);
-- ALTER TABLE receipts ADD CONSTRAINT fk_receipts_user FOREIGN KEY (user_id) REFERENCES users(id);
-- etc.

-- ============================================
-- END OF DATABASE SETUP
-- ============================================
