# Database Seeding and Role Usage Guide

## Quick Start

### Automatic Seeding (via Program.cs)

The application automatically seeds the database on startup (if connection string is configured in `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=yeka_cleaning;User=root;Password=yourpassword;"
  }
}
```

**What gets seeded automatically:**
1. All required tables (if they don't exist)
2. 8 role definitions with full usage descriptions
3. 8 sample users (one per role)
4. Role permissions matrix
5. Activity logging infrastructure

### Manual Setup (via SQL Script)

If you prefer to run SQL manually:

1. Open MySQL command line or phpMyAdmin
2. Create database: `CREATE DATABASE yeka_cleaning CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`
3. Run the script: `mysql -u root -p yeka_cleaning < Scripts/database_setup.sql`
4. Update `appsettings.json` with connection string

---

## Role Usage Summary

### Super Admin (superadmin@yeka.et / admin123)
**Full system control**
- All modules: CRUD on users, finances, content, settings
- View/edit Role Usage page at `/Dashboard/SuperAdmin/RoleUsage`
- Generate PDF reports, manage posts, view all data

### Manager (manager@yeka.et / manager123)
**Team oversight**
- Staff management
- Booking approval
- Team performance reports

### Staff (staff@yeka.et / staff123)
**Operations execution**
- Task checklists
- Agency/Yaka reports
- Office resources
- Service management

### Cleaner (cleaner@yeka.et / clean123)
**Field work**
- View assigned jobs
- Mark tasks complete
- Limited dashboard

### User (user@client.et / user123)
**Customer portal**
- Book services
- View history
- Manage profile

### Wereda Mahberat (wereda@addis.gov.et / wereda123)
**Financial admin (Wereda-level)**
- Monthly receipts CRUD
- Payroll processing
- Capital transactions
- Financial reporting

### Dispatch Officer (dispatch@yeka.et / dispatch123)
**Logistics hub**
- Meeting room scheduling
- Create/manage dispatches
- Generate Mahberat reports
- Driver coordination

### Driver (driver1@yeka.et / driver123)
**Field operations**
- View assigned tasks
- Update status/logs
- Location tracking
- Task completion

---

## Database Schema Overview

### 38 Tables Total

**Core (3):** users, services, bookings

**Super Admin (7):** outsource_companies, private_cleaning_companies, receipts, payroll, capital_transactions, posts, role_definitions

**Wereda Mahberat (1):** monthly_receipts

**Dispatch Officer (3):** meeting_rooms, mahberat_reports, dispatches

**Staff (11):** office_plans, library_items, agency_reports, yaka_reports, subcity_officers, subcity_drivers, wereda_officers, dispatch_schedules, outsource_receipts, office_recognitions, trainings

**Driver (3):** driver_locations, delivery_tasks, contacts

**Shared (5):** contact_messages, user_settings, gallery, checklists, role_permissions, role_activity_logs, system_usage_analytics

---

## Accessing Role Usage Documentation

1. Login as Super Admin
2. Navigate to: `/Dashboard/SuperAdmin/RoleUsage`
3. View all 8 role cards with:
   - Description
   - Usage context
   - Primary responsibilities
   - Daily activities
   - Reports access
   - Modules access
   - Permission toggles (editable)
4. Click "Edit" on any role card to modify usage details
5. All changes saved to `role_definitions` table

---

## Role Permission Matrix

| Feature | SA | Mgr | Staff | Cleaner | User | Wereda | Dispatch | Driver |
|---------|----|-----|-------|---------|------|--------|----------|--------|
| User Management | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| Service Management | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ | ✗ |
| Booking Management | ✓ | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Receipt Management | ✓ | △ | ✗ | ✗ | △ | ✓ | ✗ | ✗ |
| Payroll Management | ✓ | △ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Capital Management | ✓ | ✗ | ✗ | ✗ | ✗ | △ | ✗ | ✗ |
| Dispatch Management | ✓ | ✗ | ✗ | ✗ | ✗ | △ | ✓ | △ |
| Meeting Room | ✓ | ✗ | ✗ | ✗ | ✗ | △ | ✓ | ✗ |
| Reports | All | Team | Personal | None | Personal | Financial | Operational | Personal |
| Gallery | ✓ | △ | ✓ | ✗ | ✓ | △ | △ | ✗ |
| Contact | ✓ | △ | △ | △ | △ | △ | △ | △ |

✓ Full │ △ Limited │ ✗ None

---

## Testing the System

### Quick Test Checklist

**1. Super Admin Login:**
- URL: `/Login`
- Email: superadmin@yeka.et
- Pass: admin123
- Expected: Redirect to `/Dashboard/SuperAdmin` with full sidebar

**2. Test each role:**
```bash
# Wereda Mahberat
Email: wereda@addis.gov.et
Pass: wereda123
Route: /Dashboard/WeredaMahberat

# Dispatch Officer
Email: dispatch@yeka.et
Pass: dispatch123
Route: /Dashboard/DispatchOfficer

# Driver
Email: driver1@yeka.et
Pass: driver123
Route: /Dashboard/Driver
```

**3. Verify RoleUsage Page:**
- Login as Super Admin
- Click "Role Usage" in sidebar
- Verify all 8 roles displayed
- Click "Edit" on any role, modify, save
- Refresh and confirm changes appear

**4. Test CRUD Operations:**
- Navigate to any subpage with forms (e.g., `/Dashboard/SuperAdmin/Users`)
- Create new record
- Edit existing record
- Delete record
- Verify changes persist in database

---

## Extending Role Usage

### Adding a New Role

1. Update `Models.cs` → Add new RoleDefinition entry in seeder
2. Update `Login.cshtml.cs` → Add role route
3. Create dashboard page `/Dashboard/NewRole.cshtml`
4. Create all subpages
5. Update seeder to assign permissions
6. Add sample user

### Changing Role Permissions

1. Login as Super Admin
2. Go to `/Dashboard/SuperAdmin/RoleUsage`
3. Click Edit on target role
4. Modify modules and permission toggles
5. Save → Changes written to `role_definitions` table

---

## Troubleshooting

**Seeding not running:**
- Check connection string in `appsettings.json`
- Ensure tables don't already exist with conflicting schema
- View console logs for `[Seeder]` messages

**Roles not redirecting:**
- Verify role name in DB matches `RedirectToRolePage` switch statement
- Role names are lowercase in switch: `"wereda_mahberat"`, `"dispatch_officer"`, `"driver"`

**Page not found:**
- Ensure `.cshtml` file exists in correct folder
- Razor Pages conventions: UpperCamelCase for folder names

**Permission errors:**
- Session not persisting → Check browser cookies, session middleware order
- Database connection → Check MySQL service running

---

## File Structure Reference

```
Pages/
├── Dashboard/
│   ├── SuperAdmin/
│   │   ├── Index.cshtml (main dashboard)
│   │   ├── Users.cshtml + .cs
│   │   ├── Checklist.cshtml + .cs
│   │   ├── RegisterReceipt.cshtml + .cs
│   │   ├── OutsourceCompany.cshtml + .cs
│   │   ├── PrivateCleaningCompany.cshtml + .cs
│   │   ├── Capital.cshtml + .cs
│   │   ├── ReceiptPayroll.cshtml + .cs
│   │   ├── PdfReports.cshtml + .cs
│   │   ├── Post.cshtml + .cs
│   │   └── RoleUsage.cshtml + .cs ← NEW
│   ├── WeredaMahberat/
│   │   ├── Index.cshtml
│   │   ├── MonthlyReceipt.cshtml + .cs
│   │   ├── PayrollReport.cshtml + .cs
│   │   ├── Capital.cshtml + .cs
│   │   ├── Gallery.cshtml + .cs
│   │   └── Contact.cshtml + .cs
│   ├── DispatchOfficer/
│   │   ├── Index.cshtml
│   │   ├── MeetingRoom.cshtml + .cs
│   │   ├── AllMahberatReport.cshtml + .cs
│   │   ├── CreateDispatch.cshtml + .cs
│   │   ├── Gallery.cshtml + .cs
│   │   └── Contact.cshtml + .cs
│   ├── Staff/
│   │   ├── Index.cshtml
│   │   ├── Service.cshtml + .cs
│   │   ├── OfficePlan.cshtml + .cs
│   │   ├── Library.cshtml + .cs
│   │   ├── AgencyReport.cshtml + .cs
│   │   ├── Checklist.cshtml + .cs
│   │   ├── YakaReport.cshtml + .cs
│   │   ├── SubcityOfficer.cshtml + .cs
│   │   ├── SubcityDriver.cshtml + .cs
│   │   ├── WeredaOfficer.cshtml + .cs
│   │   ├── DispatchSchedule.cshtml + .cs
│   │   ├── OutsourceReceipt.cshtml + .cs
│   │   ├── OfficeRecognition.cshtml + .cs
│   │   ├── Gallery.cshtml + .cs
│   │   ├── Training.cshtml + .cs
│   │   ├── Contact.cshtml + .cs
│   │   └── Setting.cshtml + .cs
│   ├── Driver/
│   │   ├── Index.cshtml
│   │   ├── Location.cshtml + .cs
│   │   ├── Task.cshtml + .cs
│   │   └── Contact.cshtml + .cs
│   ├── Manager.cshtml
│   ├── Cleaner.cshtml
│   ├── User.cshtml
│   └── Login.cshtml
├── Models.cs (all entity models)
├── Data/
│   └── DatabaseSeeder.cs ← NEW
├── Program.cs (updated)
├── Scripts/
│   └── database_setup.sql ← NEW
└── Docs/
    └── RoleUsage.md ← NEW
```

---

## Support

For questions or issues:
1. Check RoleUsage page in SuperAdmin dashboard
2. Review this documentation
3. Check console logs for seeding errors
4. Verify database connectivity

*Last Updated: 2026-04-19*
