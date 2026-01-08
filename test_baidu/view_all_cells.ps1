$json = Get-Content 'e:\Code\C#\CYZLZDH\ocr_extract\OCR_JSON\BaiduOcr_Response_20260106_181821.json' | ConvertFrom-Json

Write-Host "=== 所有单元格内容（完整列表）===" -ForegroundColor Cyan
$index = 0
foreach ($cell in $json.tables_result[0].body) {
    $text = $cell.words -replace "`n", "" -replace "`r", ""
    Write-Host "$index. Row=$($cell.row_start) Col=$($cell.col_start): [$text]" -ForegroundColor $(if ($index % 2 -eq 0) { "White" } else { "Gray" })
    $index++
}

Write-Host ""
Write-Host "=== 搜索可能包含层/站/门数字的模式 ===" -ForegroundColor Cyan
foreach ($cell in $json.tables_result[0].body) {
    $text = $cell.words -replace "`n", "" -replace "`r", ""
    if ($text -match "\d+\s*层" -or $text -match "\d+\s*站" -or $text -match "\d+\s*门") {
        Write-Host "Row=$($cell.row_start) Col=$($cell.col_start): $text" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Header 部分 ===" -ForegroundColor Cyan
foreach ($cell in $json.tables_result[0].header) {
    $text = $cell.words -replace "`n", "" -replace "`r", ""
    Write-Host "Header: $text" -ForegroundColor Yellow
}
