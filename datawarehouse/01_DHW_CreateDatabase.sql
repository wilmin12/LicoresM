-- ============================================================
-- 01_DHW_CreateDatabase.sql
-- Crea la base de datos del Data Warehouse
-- Base de datos: DHW_DATABASE (separada de LicoresMaduoDB)
-- ============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DHW_DATABASE')
BEGIN
    CREATE DATABASE DHW_DATABASE;
END
GO

USE DHW_DATABASE;
GO

PRINT '=== DHW_DATABASE creada/seleccionada correctamente ===';
GO
