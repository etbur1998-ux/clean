-- ============================================
-- YEKA CLEANING DATABASE - Run this in phpMyAdmin
-- ============================================

-- 1. Create database (run this first)
CREATE DATABASE IF NOT EXISTS yeka_cleaning;

-- 2. Use the database
USE yeka_cleaning;

-- 3. Create users table
CREATE TABLE IF NOT EXISTS users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    role ENUM('superadmin', 'manager', 'staff', 'cleaner', 'user') DEFAULT 'user',
    phone VARCHAR(20),
    email_notifications BOOLEAN DEFAULT TRUE,
    sms_notifications BOOLEAN DEFAULT TRUE,
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_by INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 4. Insert default superadmin (password: P@ssw0rd1)
INSERT INTO users (name, email, password, role, phone) 
VALUES ('Super Admin', 'superadmin@gmail.com', 'P@ssw0rd1', 'superadmin', '0931503513');

-- 5. Create services table
CREATE TABLE IF NOT EXISTS services (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    price DECIMAL(10,2),
    duration INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 6. Insert default services
INSERT INTO services (name, description, price, duration) VALUES
('Residential Cleaning', 'Regular house cleaning service', 50.00, 120),
('Commercial Cleaning', 'Office and business cleaning', 75.00, 180),
('Deep Cleaning', 'Thorough top-to-bottom cleaning', 150.00, 240),
('Carpet Cleaning', 'Professional carpet cleaning', 80.00, 180),
('Window Cleaning', 'Interior and exterior windows', 60.00, 120),
('Post-Construction', 'Construction cleanup', 200.00, 300);

-- 7. Create bookings table
CREATE TABLE IF NOT EXISTS bookings (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    service_id INT NOT NULL,
    booking_date DATE,
    booking_time TIME,
    address TEXT,
    status ENUM('pending', 'confirmed', 'in_progress', 'completed', 'cancelled') DEFAULT 'pending',
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (service_id) REFERENCES services(id)
);

-- 8. Set default notification preferences for existing users where NULL
UPDATE users SET email_notifications = TRUE WHERE email_notifications IS NULL;
UPDATE users SET sms_notifications = TRUE WHERE sms_notifications IS NULL;

-- Add transport_request_id to staff_receipts (links transport receipt to approval queue)
ALTER TABLE staff_receipts
  ADD COLUMN IF NOT EXISTS transport_request_id INT NULL DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS image_url VARCHAR(1000) NULL DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS notes TEXT NULL DEFAULT NULL;
