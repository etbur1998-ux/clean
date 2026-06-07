-- Migration: Fix private_cleaning_companies table schema
-- This migration updates the table to match the C# code requirements
-- Created: 2026-06-06

-- Check if the table exists and add missing columns
ALTER TABLE private_cleaning_companies 
MODIFY COLUMN services_offered TEXT COMMENT 'services_offered' AFTER address;

-- Rename contract_status to status if it exists
ALTER TABLE private_cleaning_companies 
CHANGE COLUMN contract_status status VARCHAR(50) DEFAULT 'Active' AFTER services_offered;

-- Add missing columns if they don't exist
ALTER TABLE private_cleaning_companies 
ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE AFTER status;

ALTER TABLE private_cleaning_companies 
ADD COLUMN IF NOT EXISTS rep_user_id INT DEFAULT NULL AFTER is_active;

ALTER TABLE private_cleaning_companies 
ADD COLUMN IF NOT EXISTS deleted_at DATETIME DEFAULT NULL AFTER rep_user_id;

-- Verify the schema
DESCRIBE private_cleaning_companies;
