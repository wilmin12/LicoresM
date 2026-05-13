USE LicoresMaduoDB;
GO

/* 
   Script 68: Nuevas columnas para Litraje y Factor en Detalles de Cálculo
   Módulo: Cost Calculation
*/

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.COST_CALC_PO_DET_FIN') AND name = 'CCPD_Liters')
BEGIN
    ALTER TABLE dbo.COST_CALC_PO_DET_FIN ADD CCPD_Liters DECIMAL(18, 4) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.COST_CALC_PO_DET_FIN') AND name = 'CCPD_Factor')
BEGIN
    ALTER TABLE dbo.COST_CALC_PO_DET_FIN ADD CCPD_Factor DECIMAL(18, 4) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.COST_CALC_PO_DET_FIN') AND name = 'CCPD_Free_Qty')
BEGIN
    ALTER TABLE dbo.COST_CALC_PO_DET_FIN ADD CCPD_Free_Qty DECIMAL(18, 4) NULL DEFAULT 0;
END
GO
