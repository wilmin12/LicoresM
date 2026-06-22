-- Add UM (unit of measure) and FC_PRICE USD columns to the cost calculation detail table
ALTER TABLE COST_CALC_PO_DET_FIN ADD CCPD_UM VARCHAR(5) NULL;
ALTER TABLE COST_CALC_PO_DET_FIN ADD CCPD_FOB_Price_USD DECIMAL(18,6) NULL;
