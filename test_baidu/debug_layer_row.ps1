$json = Get-Content 'e:\Code\C#\CYZLZDH\ocr_extract\OCR_JSON\BaiduOcr_Response_20260106_181821.json' | ConvertFrom-Json

Write-Host "=== Row 11 附近的单元格（检查层站门数区域）===" -ForegroundColor Cyan
foreach ($cell in $json.tables_result[0].body) {
    if ($cell.row_start -ge 10 -and $cell.row_start -le 12) {
        $text = $cell.words -replace "`n", "\\n" -replace "`r", "\\r"
        Write-Host "Row=$($cell.row_start) Col=$($cell.col_start): $text" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Row 11 Col 1-10 的连续单元格组合测试 ===" -ForegroundColor Cyan
$layerText = ""
$stationText = ""
$doorText = ""

for ($col = 1; $col -le 10; $col++) {
    $cell = $json.tables_result[0].body | Where-Object { $_.row_start -eq 11 -and $_.col_start -eq $col }
    if ($cell) {
        $text = $cell.words -replace "`n", "" -replace "`r", ""
        Write-Host "Col=$col : [$text]" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== 完整分析层站门数所在行的所有单元格 ===" -ForegroundColor Cyan
$layerCells = $json.tables_result[0].body | Where-Object { $_.row_start -eq 11 } | Sort-Object col_start
foreach ($cell in $layerCells) {
    $text = $cell.words -replace "`n", "\\n" -replace "`r", "\\r"
    Write-Host "Col=$($cell.col_start): [$text]" -ForegroundColor $(if ($cell.col_start -ge 1 -and $cell.col_start -le 6) { "Magenta" } else { "Gray" })
}
