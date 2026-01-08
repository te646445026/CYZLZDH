$json = Get-Content 'e:\Code\C#\CYZLZDH\ocr_extract\OCR_JSON\BaiduOcr_Response_20260106_181821.json' | ConvertFrom-Json
$json.tables_result[0].body | ForEach-Object { Write-Host ("Row:" + $_.row_start.ToString() + " Col:" + $_.col_start.ToString() + " Text:" + $_.words) }
