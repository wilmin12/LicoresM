-- ============================================================
-- Script 07: Add Freight Forwarder Quote Submodules
-- Adds 5 new submodules for the Freight Forwarder Quote pages
-- Run AFTER 06_FreightForwarderQuoteTables.sql
-- ============================================================

USE LicoresMaduoDB;
GO

-- ============================================================
-- INSERT new submodules (IDs 67-71, Module 2 = FREIGHT)
-- ============================================================
SET IDENTITY_INSERT dbo.LM_Submodules ON;

INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive) VALUES
(67, 2, 'Freight Forwarders',        'FF_FORWARDERS',          'FREIGHT_FORWARDERS',          18, 1),
(68, 2, 'Ocean Freight Quotes',      'FF_OCEAN_QUOTES',        'FF_OCEAN_FREIGHT_HEADER',     19, 1),
(69, 2, 'Inland Freight Quotes',     'FF_INLAND_QUOTES',       'FF_INLAND_FREIGHT_HEADER',    20, 1),
(70, 2, 'LCL Quotes',                'FF_LCL_QUOTES',          'FF_LCL_HEADER',               21, 1),
(71, 2, 'Inland Additional Charges', 'FF_INLAND_ADD_CHARGES',  'FF_INLAND_ADDITIONAL_CHARGES',22, 1);

SET IDENTITY_INSERT dbo.LM_Submodules OFF;
GO

-- ============================================================
-- GRANT permissions on the new submodules to existing roles
-- ============================================================

-- SuperAdmin (RoleId=1): full access
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT 1, SubmoduleId, 1, 1, 1, 1, 1
FROM dbo.LM_Submodules
WHERE SubmoduleCode IN ('FF_FORWARDERS','FF_OCEAN_QUOTES','FF_INLAND_QUOTES','FF_LCL_QUOTES','FF_INLAND_ADD_CHARGES');
GO

-- Admin (RoleId=2): full access except delete
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT 2, SubmoduleId, 1, 1, 1, 1, 0
FROM dbo.LM_Submodules
WHERE SubmoduleCode IN ('FF_FORWARDERS','FF_OCEAN_QUOTES','FF_INLAND_QUOTES','FF_LCL_QUOTES','FF_INLAND_ADD_CHARGES');
GO

-- ReadOnly (RoleId=8): read only
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT 8, SubmoduleId, 1, 1, 0, 0, 0
FROM dbo.LM_Submodules
WHERE SubmoduleCode IN ('FF_FORWARDERS','FF_OCEAN_QUOTES','FF_INLAND_QUOTES','FF_LCL_QUOTES','FF_INLAND_ADD_CHARGES');
GO
