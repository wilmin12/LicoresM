-- ============================================================
-- Script 50 — Add CanApprove column to LM_RolePermissions
-- Sesión: 2026-04-07  QA Remark: approve/reject permission per role
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LM_RolePermissions') AND name = 'CanApprove'
)
BEGIN
    ALTER TABLE dbo.LM_RolePermissions
        ADD CanApprove BIT NOT NULL DEFAULT 0;

    PRINT 'Column CanApprove added to LM_RolePermissions.';
END
ELSE
BEGIN
    PRINT 'Column CanApprove already exists in LM_RolePermissions.';
END
GO
