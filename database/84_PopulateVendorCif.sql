-- Populate CC_VENDOR_CIF with vendors that qualify for CIF (insurance = 0)
-- Vendors: BCP, BM, DWL, EG, LH, ROS

MERGE INTO dbo.CC_VENDOR_CIF AS target
USING (
    VALUES
        ('BCP'),
        ('BM'),
        ('DWL'),
        ('EG'),
        ('LH'),
        ('ROS')
) AS source (VCIF_Vendor)
ON target.VCIF_Vendor = source.VCIF_Vendor
WHEN NOT MATCHED THEN
    INSERT (VCIF_Vendor)
    VALUES (source.VCIF_Vendor);
GO

-- Verify the populated rows
SELECT * FROM dbo.CC_VENDOR_CIF
ORDER BY VCIF_Vendor;
GO
