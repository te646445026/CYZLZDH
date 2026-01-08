$originalText = "10`n8`n8`n层`n站`n门"

Write-Host "原始文本: [$originalText]" -ForegroundColor Yellow

$lines = $originalText -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }

Write-Host ("分隔后的行数: " + $lines.Count) -ForegroundColor Yellow
Write-Host "每行内容:" -ForegroundColor Yellow
for ($i = 0; $i -lt $lines.Count; $i++) {
    $lineContent = $lines[$i]
    Write-Host ("  行" + $i + ": [" + $lineContent + "]") -ForegroundColor Gray
}

$layer = ""
$station = ""
$door = ""

foreach ($line in $lines) {
    $isNumber = $line -match "^\d+$"
    Write-Host ("检查: [" + $line + "] - 是否纯数字: " + $isNumber) -ForegroundColor Cyan
    if ($isNumber) {
        if ($layer -eq "") {
            $layer = $line
            Write-Host ("  -> 设置 layer = " + $layer) -ForegroundColor Green
        }
        elseif ($station -eq "") {
            $station = $line
            Write-Host ("  -> 设置 station = " + $station) -ForegroundColor Green
        }
        elseif ($door -eq "") {
            $door = $line
            Write-Host ("  -> 设置 door = " + $door) -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host ("最终结果: layer='" + $layer + "', station='" + $station + "', door='" + $door + "'") -ForegroundColor Yellow

if ($layer -ne "" -and $station -ne "" -and $door -ne "") {
    $result = $layer + "层" + $station + "站" + $door + "门"
    Write-Host ("解析成功: " + $result) -ForegroundColor Green
}
else {
    Write-Host "解析失败" -ForegroundColor Red
}
