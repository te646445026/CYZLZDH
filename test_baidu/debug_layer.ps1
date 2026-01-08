$json = Get-Content 'e:\Code\C#\CYZLZDH\ocr_extract\OCR_JSON\BaiduOcr_Response_20260106_181821.json' | ConvertFrom-Json

Write-Host "=== Row 11 的单元格（层站门数所在行）===" -ForegroundColor Cyan
foreach ($cell in $json.tables_result[0].body) {
    if ($cell.row_start -eq 11) {
        $text = $cell.words -replace "`n", "" -replace "`r", ""
        Write-Host "Row=$($cell.row_start) Col=$($cell.col_start): $text" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== 所有包含 层/站/门 的单元格 ===" -ForegroundColor Cyan
foreach ($cell in $json.tables_result[0].body) {
    $text = $cell.words -replace "`n", "" -replace "`r", ""
    if ($text -match "层" -or $text -match "站" -or $text -match "门") {
        Write-Host "Row=$($cell.row_start) Col=$($cell.col_start): $text" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== 查找 10层 20站 20门 这类格式 ===" -ForegroundColor Cyan
foreach ($cell in $json.tables_result[0].body) {
    $text = $cell.words -replace "`n", "" -replace "`r", ""
    if ($text -match "\d+层" -or $text -match "\d+站" -or $text -match "\d+门") {
        Write-Host "Row=$($cell.row_start) Col=$($cell.col_start): $text" -ForegroundColor Magenta
    }
}
