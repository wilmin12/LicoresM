-- ============================================================
-- 02_AuthTables.sql
-- Auth & Security tables: Roles, Users, Modules, Submodules,
-- RolePermissions, AuditLog
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Roles ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_Roles')
BEGIN
    CREATE TABLE LM_Roles (
        RoleId      INT IDENTITY(1,1) NOT NULL,
        RoleName    NVARCHAR(50)  NOT NULL,
        Description NVARCHAR(200) NOT NULL DEFAULT '',
        IsActive    BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_LM_Roles PRIMARY KEY (RoleId),
        CONSTRAINT UQ_LM_Roles_Name UNIQUE (RoleName)
    );

    INSERT INTO LM_Roles (RoleName, Description) VALUES
        ('SuperAdmin', 'Full system access'),
        ('Admin',      'Administrative access'),
        ('User',       'Standard user access');
END
GO

-- ── Users ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_Users')
BEGIN
    CREATE TABLE LM_Users (
        UserId       INT IDENTITY(1,1) NOT NULL,
        Username     NVARCHAR(50)  NOT NULL,
        PasswordHash NVARCHAR(256) NOT NULL,
        Email        NVARCHAR(100) NOT NULL,
        FullName     NVARCHAR(100) NOT NULL DEFAULT '',
        IsActive     BIT           NOT NULL DEFAULT 1,
        CreatedAt    DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        LastLogin    DATETIME      NULL,
        RoleId       INT           NOT NULL,
        AvatarUrl    NVARCHAR(300) NULL,
        CONSTRAINT PK_LM_Users    PRIMARY KEY (UserId),
        CONSTRAINT UQ_LM_Users_Username UNIQUE (Username),
        CONSTRAINT FK_LM_Users_Role FOREIGN KEY (RoleId) REFERENCES LM_Roles(RoleId)
    );
END
GO

-- ── Modules ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_Modules')
BEGIN
    CREATE TABLE LM_Modules (
        ModuleId     INT IDENTITY(1,1) NOT NULL,
        ModuleName   NVARCHAR(50)  NOT NULL,
        ModuleCode   NVARCHAR(30)  NOT NULL,
        Icon         NVARCHAR(50)  NULL,
        DisplayOrder INT           NOT NULL DEFAULT 0,
        IsActive     BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_LM_Modules     PRIMARY KEY (ModuleId),
        CONSTRAINT UQ_LM_Modules_Code UNIQUE (ModuleCode)
    );
END
GO

-- ── Submodules ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_Submodules')
BEGIN
    CREATE TABLE LM_Submodules (
        SubmoduleId   INT IDENTITY(1,1) NOT NULL,
        ModuleId      INT           NOT NULL,
        SubmoduleName NVARCHAR(100) NOT NULL,
        SubmoduleCode NVARCHAR(50)  NOT NULL,
        TableName     NVARCHAR(100) NULL,
        DisplayOrder  INT           NOT NULL DEFAULT 0,
        IsActive      BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_LM_Submodules     PRIMARY KEY (SubmoduleId),
        CONSTRAINT UQ_LM_Submodules_Code UNIQUE (SubmoduleCode),
        CONSTRAINT FK_LM_Submodules_Module FOREIGN KEY (ModuleId) REFERENCES LM_Modules(ModuleId)
    );
END
GO

-- ── RolePermissions ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_RolePermissions')
BEGIN
    CREATE TABLE LM_RolePermissions (
        PermissionId INT  IDENTITY(1,1) NOT NULL,
        RoleId       INT  NOT NULL,
        SubmoduleId  INT  NOT NULL,
        CanAccess    BIT  NOT NULL DEFAULT 0,
        CanRead      BIT  NOT NULL DEFAULT 0,
        CanWrite     BIT  NOT NULL DEFAULT 0,
        CanEdit      BIT  NOT NULL DEFAULT 0,
        CanDelete    BIT  NOT NULL DEFAULT 0,
        CONSTRAINT PK_LM_RolePermissions PRIMARY KEY (PermissionId),
        CONSTRAINT UQ_LM_RolePermissions_RoleSub UNIQUE (RoleId, SubmoduleId),
        CONSTRAINT FK_LM_RolePermissions_Role FOREIGN KEY (RoleId) REFERENCES LM_Roles(RoleId),
        CONSTRAINT FK_LM_RolePermissions_Sub  FOREIGN KEY (SubmoduleId) REFERENCES LM_Submodules(SubmoduleId)
    );
END
GO

-- ── AuditLog ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_AuditLog')
BEGIN
    CREATE TABLE LM_AuditLog (
        LogId     BIGINT IDENTITY(1,1) NOT NULL,
        UserId    INT           NULL,
        Action    NVARCHAR(10)  NOT NULL,   -- CREATE | UPDATE | DELETE
        TableName NVARCHAR(100) NOT NULL,
        RecordId  NVARCHAR(50)  NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        CreatedAt DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        IpAddress NVARCHAR(45)  NULL,
        CONSTRAINT PK_LM_AuditLog PRIMARY KEY (LogId)
    );
END
GO
