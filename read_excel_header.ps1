$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open('C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\remarkes\GS_2012.xlsx')
$ws = $wb.Sheets.Item(1)
$header = ""
for($i=1; $i -le 30; $i++) {
    $val = $ws.Cells.Item(1, $i).Value2
    if ($val) {
        $header += "$val,"
    }
}
$header
$wb.Close()
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
