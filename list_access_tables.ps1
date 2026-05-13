$connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\Freight_Forwarders_Wilmin\FREIGHT_Prog.accdb;"
$conn = New-Object -ComObject ADODB.Connection
try {
    $conn.Open($connString)
    $tables = $conn.OpenSchema(20)
    while (!$tables.EOF) {
        if ($tables.Fields.Item("TABLE_TYPE").Value -eq "TABLE") {
            Write-Host $tables.Fields.Item("TABLE_NAME").Value
        }
        $tables.MoveNext()
    }
} catch {
    Write-Error $_.Exception.Message
} finally {
    if ($conn.State -eq 1) { $conn.Close() }
}
