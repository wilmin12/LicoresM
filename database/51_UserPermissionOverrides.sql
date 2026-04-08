-- ============================================================
-- 51_UserPermissionOverrides.sql
-- Creates LM_UserPermissions table for per-user permission
-- overrides that take precedence over role permissions.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LM_UserPermissions')
BEGIN
    CREATE TABLE dbo.LM_UserPermissions (
        UP_Id          INT IDENTITY(1,1) PRIMARY KEY,
        UP_UserId      INT NOT NULL,
        UP_SubmoduleId INT NOT NULL,
        UP_CanAccess   BIT NOT NULL DEFAULT 0,
        UP_CanRead     BIT NOT NULL DEFAULT 0,
        UP_CanWrite    BIT NOT NULL DEFAULT 0,
        UP_CanEdit     BIT NOT NULL DEFAULT 0,
        UP_CanDelete   BIT NOT NULL DEFAULT 0,
        UP_CanApprove  BIT NOT NULL DEFAULT 0,

        CONSTRAINT FK_UserPerms_User
            FOREIGN KEY (UP_UserId) REFERENCES dbo.LM_Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserPerms_Submodule
            FOREIGN KEY (UP_SubmoduleId) REFERENCES dbo.LM_Submodules(SubmoduleId) ON DELETE CASCADE,
        CONSTRAINT UQ_UserPerms
            UNIQUE (UP_UserId, UP_SubmoduleId)
    );

    PRINT 'LM_UserPermissions created.';
END
ELSE
    PRINT 'LM_UserPermissions already exists — skipped.';
