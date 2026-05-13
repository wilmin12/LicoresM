USE LicoresMaduoDB;
GO

/* 
   Script 69: Columna CcphTotLiters en Cabecera de PO de Cálculo
   Módulo: Cost Calculation
*/

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.COST_CALC_PO_HEAD_FIN') AND name = 'CCPH_TotLiters')
BEGIN
    ALTER TABLE dbo.COST_CALC_PO_HEAD_FIN ADD CCPH_TotLiters DECIMAL(18, 4) NULL;
END
GO
