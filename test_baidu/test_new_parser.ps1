$jsonContent = Get-Content 'e:\Code\C#\CYZLZDH\ocr_extract\OCR_JSON\BaiduOcr_Response_20260106_181821.json' -Raw
$objs = $jsonContent | ConvertFrom-Json

$allCells = @()

foreach ($table in $objs.tables_result) {
    if ($table.header) {
        foreach ($cell in $table.header) {
            $allCells += @{
                words = $cell.words
                row_start = 0
                col_start = 0
            }
        }
    }
    if ($table.body) {
        foreach ($cell in $table.body) {
            $allCells += @{
                words = $cell.words
                row_start = $cell.row_start
                col_start = $cell.col_start
            }
        }
    }
}

function Parse-LayerStationDoor {
    param([string]$text)

    if ([string]::IsNullOrEmpty($text)) { return $null }

    $lines = $text -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }

    if ($lines.Count -ge 6) {
        $layer = ""
        $station = ""
        $door = ""

        foreach ($line in $lines) {
            if ($line -match "^\d+$") {
                if ($layer -eq "") { $layer = $line }
                elseif ($station -eq "") { $station = $line }
                elseif ($door -eq "") { $door = $line }
            }
        }

        $lineStr = [string]::Concat($lines)
        if ($layer -eq "" -or $station -eq "" -or $door -eq "") {
            $layerMatch = [regex]::Match($lineStr, "(\d+)\s*层")
            $stationMatch = [regex]::Match($lineStr, "(\d+)\s*站")
            $doorMatch = [regex]::Match($lineStr, "(\d+)\s*门")

            if ($layerMatch.Success -and $stationMatch.Success -and $doorMatch.Success) {
                return "$($layerMatch.Groups[1].Value)层$($stationMatch.Groups[1].Value)站$($doorMatch.Groups[1].Value)门"
            }
        }
        else {
            return "$layer层$station站$door门"
        }
    }

    return $null
}

Write-Host "=== 测试 ParseLayerStationDoor 函数 ===" -ForegroundColor Cyan
$layerCell = $allCells | Where-Object { $_.row_start -eq 11 -and $_.col_start -eq 3 }
if ($layerCell) {
    $originalText = $layerCell.words
    Write-Host "原始文本: [$originalText]" -ForegroundColor Yellow
    $lineCount = ($originalText -split "`n" | Where-Object { $_ -ne "" }).Count
    Write-Host "换行分隔后的行数: $lineCount" -ForegroundColor Yellow

    $result = Parse-LayerStationDoor $originalText
    if ($result) {
        Write-Host "解析结果: $result" -ForegroundColor Green
    }
    else {
        Write-Host "解析失败" -ForegroundColor Red
    }
}
