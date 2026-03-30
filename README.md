# Licores Maduro - Enterprise Web Application

## Architecture Overview

### Technology Stack
- **Backend:** ASP.NET Core 8 Web API (C#12)
- **Database:** SQL Server (LicoresMaduoDB)
- **ORM:** Entity Framework Core 8
- **Authentication:** JWT Bearer Tokens + BCrypt password hashing
- **Frontend:** HTML5 / Bootstrap 5 / Vanilla JavaScript
- **UI Theme:** Wine/Burgundy (#722F37)

---

## Project Structure

```
Proyecto Licores Maduro/
├── README.md
├── database/
│   ├── 01_CreateDatabase.sql          # Database creation
│   ├── 02_AuthTables.sql              # Auth/security tables + seed data
│   └── 03_WebManagedTables.sql        # All 66 business tables
├── src/
│   └── LicoresMaduro.API/
│       ├── LicoresMaduro.API.csproj
│       ├── appsettings.json
│       ├── Program.cs
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── UsersController.cs
│       │   ├── RolesController.cs
│       │   ├── GenericCatalogController.cs
│       │   └── FreightForwarder/
│       │       ├── CurrenciesController.cs
│       │       └── VendorsController.cs
│       ├── Data/
│       │   └── ApplicationDbContext.cs
│       ├── DTOs/
│       │   └── Auth/
│       │       ├── LoginDto.cs
│       │       └── UserDto.cs
│       ├── Helpers/
│       │   └── ApiResponse.cs
│       ├── Middleware/
│       │   └── JwtMiddleware.cs
│       ├── Models/
│       │   └── Auth/
│       │       ├── User.cs
│       │       ├── Role.cs
│       │       ├── Module.cs
│       │       ├── Submodule.cs
│       │       └── RolePermission.cs
│       └── Services/
│           ├── AuthService.cs
│           └── PermissionService.cs
└── frontend/
    ├── index.html                     # Login page
    ├── dashboard.html                 # Main dashboard
    ├── pages/
    │   ├── users.html                 # User management
    │   └── catalog.html               # Generic catalog CRUD
    ├── css/
    │   └── main.css                   # Wine/burgundy theme
    └── js/
        ├── api.js                     # API client (fetch wrapper)
        ├── auth.js                    # Auth helpers
        └── sidebar.js                 # Sidebar/menu logic
```

---

## Modules & Tables

| # | Module             | Tables |
|---|-------------------|--------|
| 1 | Tracking           | 1      |
| 2 | Freight Forwarder  | 17     |
| 3 | Cost Calculation   | 0*     |
| 4 | Route Assignment   | 0*     |
| 5 | Stock Analysis     | 0*     |
| 6 | Activity Request   | 38     |
| 7 | Aankoopbon         | 10     |

> *Modules 3-5 use views/reports over the existing tables.

**Total web-managed tables: 66**

---

## Security Roles

| Role             | Access Level                              |
|-----------------|-------------------------------------------|
| SuperAdmin      | Full access to everything                 |
| Admin           | Full access except system config          |
| TrackingManager | Tracking module read/write                |
| FreightManager  | Freight Forwarder module read/write       |
| CostManager     | Cost Calculation module read/write        |
| ActivityManager | Activity Request module read/write        |
| PurchaseManager | Aankoopbon module read/write              |
| ReadOnly        | Read-only access to permitted modules     |

---

## Setup Instructions

### 1. Database
```sql
-- Run in order:
-- 1. database/01_CreateDatabase.sql
-- 2. database/02_AuthTables.sql
-- 3. database/03_WebManagedTables.sql
```

### 2. Backend API
```bash
cd src/LicoresMaduro.API
# Update appsettings.json with your SQL Server connection string
dotnet restore
dotnet run
# API available at https://localhost:7001
# Swagger UI at https://localhost:7001/swagger
```

### 3. Frontend
```
# Open frontend/index.html in browser or serve via IIS / nginx
# Default credentials: admin / Admin@123
```

---

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login, returns JWT token
- `POST /api/auth/logout` - Logout (client-side token removal)
- `GET /api/auth/me` - Get current user info + permissions

### User Management
- `GET /api/users` - List all users
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `PUT /api/users/{id}/password` - Change password
- `PUT /api/users/{id}/toggle-status` - Enable/disable user
- `DELETE /api/users/{id}` - Delete user

### Catalog Tables (Generic)
- `GET /api/{table}` - List records
- `GET /api/{table}/{id}` - Get single record
- `POST /api/{table}` - Create record
- `PUT /api/{table}/{id}` - Update record
- `DELETE /api/{table}/{id}` - Delete (soft delete)

---

## JWT Token Structure
```json
{
  "sub": "userId",
  "username": "admin",
  "email": "admin@licoresmaduro.com",
  "role": "SuperAdmin",
  "roleId": "1",
  "exp": 1234567890
}
```

---

## Audit Logging
All write operations (Create, Update, Delete) are logged to `LM_AuditLog` with:
- User ID and IP address
- Table name and record ID
- Old and new values (JSON)
- Timestamp

---

*Generated: March 2026 | Licores Maduro Enterprise System*
