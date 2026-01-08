$jsonContent = Get-Content 'e:\Code\C#\CYZLZDH\ocr_extract\OCR_JSON\BaiduOcr_Response_20260106_181821.json' -Raw
$objs = $jsonContent | ConvertFrom-Json

$layerCell = $objs.tables_result[0].body | Where-Object { $_.row_start -eq 11 -and $_.col_start -eq 3 }

if ($layerCell) {
    $originalText = $layerCell.words
    Write-Host "原始文本: [$originalText]" -ForegroundColor Yellow

    $lines = $originalText -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
    Write-Host ("分隔后的行数: " + $lines.Count) -ForegroundColor Yellow

    if ($lines.Count -ge 6) {
        $layer = ""
        $station = ""
        $door = ""

        foreach ($line in $lines) {
            $isNumber = $line -match "^\d+$"
            if ($isNumber) {
                if ($layer -eq "") {
                    $layer = $line
                }
                elseif ($station -eq "") {
                    $station = $line
                }
                elseif ($door -eq "") {
                    $door = $line
                }
            }
        }

        Write-Host ("layer='" + $layer + "' station='" + $station + "' door='" + $door + "'") -ForegroundColor Yellow

        if ($layer -ne "" -and $station -ne "" -and $door -ne "") {
            $result = $layer + "层" + $station + "站" + $door + "门"
            Write-Host ("解析成功: " + $result) -ForegroundColor Green
        }
        else {
            Write-Host "解析失败: 层/站/门 有空值" -ForegroundColor Red
        }
    }
    else {
        Write-Host "解析失败: 行数不足6行" -ForegroundColor Red
    }
}
else {
    Write-Host "未找到层站门数单元格" -ForegroundColor Red
}
