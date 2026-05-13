USE LicoresMaduoDB;
GO

/* 
   Script 67: Tablas para Excepciones de Alcohol y Factores de Litraje
   Módulo: Cost Calculation
*/

-- 1. Tabla de excepciones para productos cuyo factor siempre es Field4/100
IF OBJECT_ID('dbo.cc_Items_On_Alc_Perc', 'U') IS NOT NULL DROP TABLE dbo.cc_Items_On_Alc_Perc;
CREATE TABLE dbo.cc_Items_On_Alc_Perc (
    IOAP_ItemNo NVARCHAR(20) NOT NULL,
    CONSTRAINT PK_cc_Items_On_Alc_Perc PRIMARY KEY (IOAP_ItemNo)
);

-- Seed de ejemplo basado en observaciones
INSERT INTO dbo.cc_Items_On_Alc_Perc (IOAP_ItemNo) VALUES ('211130'), ('212001'), ('212003');

-- 2. Tabla para registrar el factor calculado final por ítem
IF OBJECT_ID('dbo.cc_Item_Liter_Factor_Calc_Fin', 'U') IS NOT NULL DROP TABLE dbo.cc_Item_Liter_Factor_Calc_Fin;
CREATE TABLE dbo.cc_Item_Liter_Factor_Calc_Fin (
    ItemNo NVARCHAR(20) NOT NULL,
    Factor DECIMAL(18, 4) NOT NULL,
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_cc_Item_Liter_Factor_Calc_Fin PRIMARY KEY (ItemNo)
);
GO
