USE LicoresMaduoDB;
GO

/* 
   Script 70: Tabla para Historial de Tasas de Cambio (Forex)
   Módulo: Cost Calculation / Scraping
*/

IF OBJECT_ID('dbo.cc_Forex_History', 'U') IS NOT NULL DROP TABLE dbo.cc_Forex_History;
GO

CREATE TABLE dbo.cc_Forex_History (
    Fx_Id INT IDENTITY(1,1) NOT NULL,
    Fx_Date DATE NOT NULL,
    Fx_Currency NVARCHAR(10) NOT NULL, -- USD, EUR, etc.
    Fx_Description NVARCHAR(50) NULL,  -- US Dollar, Euro, etc.
    Fx_Buying_Check DECIMAL(18, 4) NULL,
    Fx_Buying_Cash DECIMAL(18, 4) NULL,
    Fx_Selling_Rate DECIMAL(18, 4) NULL,
    Fx_CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_cc_Forex_History PRIMARY KEY (Fx_Id)
);
GO

-- Índice para búsquedas rápidas por fecha y moneda
CREATE INDEX IX_cc_Forex_History_Date_Currency ON dbo.cc_Forex_History (Fx_Date, Fx_Currency);
GO
