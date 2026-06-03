USE LicoresMaduoDB;
GO

-- Register COST_VENDOR_CIF submodule so it appears in the permissions management UI
DECLARE @CostId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'COST');

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_VENDOR_CIF')
BEGIN
    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (@CostId, 'CIF Vendors', 'COST_VENDOR_CIF', 'CC_VENDOR_CIF', 9);
END
GO

-- Seed default role permissions (idempotent MERGE)
DECLARE @SmId INT = (SELECT SubmoduleId FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_VENDOR_CIF');

MERGE dbo.LM_RolePermissions AS tgt
USING (VALUES
    (1, @SmId, 1,1,1,1,1,0),
    (2, @SmId, 1,1,1,1,1,0),
    (3, @SmId, 1,1,0,0,0,0)
) AS src (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
ON tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead,
            src.CanWrite, src.CanEdit, src.CanDelete, src.CanApprove);
GO

-- Verify
SELECT sm.SubmoduleCode, sm.SubmoduleName, r.RoleId,
       r.CanAccess, r.CanRead, r.CanWrite, r.CanDelete
FROM dbo.LM_Submodules sm
JOIN dbo.LM_RolePermissions r ON r.SubmoduleId = sm.SubmoduleId
WHERE sm.SubmoduleCode = 'COST_VENDOR_CIF'
ORDER BY r.RoleId;
GO
