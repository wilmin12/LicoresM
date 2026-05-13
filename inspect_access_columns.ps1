$connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\Freight_Forwarders_Wilmin\FREIGHT_Prog.accdb;"
$conn = New-Object -ComObject ADODB.Connection
$tablesToInspect = @("FF_QT_HEADER", "FF_QT_OCEAN_PORT", "FF_QT_OCEAN_PORT_SLINE", "FF_QT_OCEAN_PORT_SLINE_CHARGES")
try {
    $conn.Open($connString)
    foreach ($tableName in $tablesToInspect) {
        Write-Host "Table: $tableName"
        $columns = $conn.OpenSchema(4, @($null, $null, $tableName, $null))
        while (!$columns.EOF) {
            Write-Host "  - $($columns.Fields.Item('COLUMN_NAME').Value) ($($columns.Fields.Item('DATA_TYPE').Value))"
            $columns.MoveNext()
        }
    }
} catch {
    Write-Error $_.Exception.Message
} finally {
    if ($conn.State -eq 1) { $conn.Close() }
}
