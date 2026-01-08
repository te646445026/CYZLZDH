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

$keyPositions = @{}

foreach ($cell in $allCells) {
    $text = $cell.words -replace "`n", "" -replace "`r", "" -replace " ", ""
    if ([string]::IsNullOrEmpty($text)) { continue }

    if ($text -eq "使用单位名称") { $keyPositions["使用单位名称"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "统一社会信用代码" -or $text -eq "统一社会信用代码") { $keyPositions["统一社会信用代码"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "设备品种") { $keyPositions["设备品种"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "安装地点") { $keyPositions["安装地点"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "产品型号") { $keyPositions["产品型号"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "产品编号") { $keyPositions["产品编号"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "制造单位名称") { $keyPositions["制造单位名称"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "维护保养单位名称") { $keyPositions["维护保养单位名称"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "施工单位名称") { $keyPositions["施工单位名称"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "额定速度") { $keyPositions["额定速度"] = @($cell.row_start, $cell.col_start) }
    elseif ($text -eq "层站门数") { $keyPositions["层站门数"] = @($cell.row_start, $cell.col_start) }
}

Write-Host "=== 关键字位置 ===" -ForegroundColor Cyan
foreach ($key in $keyPositions.Keys) {
    Write-Host "$key : Row=$($keyPositions[$key][0]), Col=$($keyPositions[$key][1])" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== 提取结果 ===" -ForegroundColor Cyan

$result = @{
    UserName = "/"
    DeviceType = "/"
    UsingAddress = "/"
    Model = "/"
    SerialNum = "/"
    ManufacturingUnit = "/"
    MaintenanceUnit = "/"
    ConstructionUnit = "/"
    Speed = "/"
    LayerStationDoor = "/"
}

foreach ($cell in $allCells) {
    $text = $cell.words -replace "`n", "" -replace "`r", "" -replace " ", ""
    if ([string]::IsNullOrEmpty($text)) { continue }

    $rowStart = $cell.row_start
    $colStart = $cell.col_start

    if ($keyPositions.ContainsKey("使用单位名称")) {
        $pos = $keyPositions["使用单位名称"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.UserName -eq "/" -or [string]::IsNullOrEmpty($result.UserName))) {
            $result.UserName = $text
        }
    }

    if ($keyPositions.ContainsKey("设备品种")) {
        $pos = $keyPositions["设备品种"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.DeviceType -eq "/" -or [string]::IsNullOrEmpty($result.DeviceType))) {
            $result.DeviceType = $text
        }
    }

    if ($keyPositions.ContainsKey("安装地点")) {
        $pos = $keyPositions["安装地点"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.UsingAddress -eq "/" -or [string]::IsNullOrEmpty($result.UsingAddress))) {
            $result.UsingAddress = $text
        }
    }

    if ($keyPositions.ContainsKey("产品型号")) {
        $pos = $keyPositions["产品型号"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.Model -eq "/" -or [string]::IsNullOrEmpty($result.Model))) {
            $result.Model = $text
        }
    }

    if ($keyPositions.ContainsKey("产品编号")) {
        $pos = $keyPositions["产品编号"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.SerialNum -eq "/" -or [string]::IsNullOrEmpty($result.SerialNum))) {
            $result.SerialNum = $text
        }
    }

    if ($keyPositions.ContainsKey("制造单位名称")) {
        $pos = $keyPositions["制造单位名称"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.ManufacturingUnit -eq "/" -or [string]::IsNullOrEmpty($result.ManufacturingUnit))) {
            $result.ManufacturingUnit = $text
        }
    }

    if ($keyPositions.ContainsKey("维护保养单位名称")) {
        $pos = $keyPositions["维护保养单位名称"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.MaintenanceUnit -eq "/" -or [string]::IsNullOrEmpty($result.MaintenanceUnit))) {
            $result.MaintenanceUnit = $text
        }
    }

    if ($keyPositions.ContainsKey("施工单位名称")) {
        $pos = $keyPositions["施工单位名称"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.ConstructionUnit -eq "/" -or [string]::IsNullOrEmpty($result.ConstructionUnit))) {
            $result.ConstructionUnit = $text
        }
    }

    if ($keyPositions.ContainsKey("额定速度")) {
        $pos = $keyPositions["额定速度"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.Speed -eq "/" -or [string]::IsNullOrEmpty($result.Speed))) {
            $result.Speed = $text
        }
    }

    if ($keyPositions.ContainsKey("层站门数")) {
        $pos = $keyPositions["层站门数"]
        if ($rowStart -eq $pos[0] -and $colStart -gt $pos[1] -and ($result.LayerStationDoor -eq "/" -or [string]::IsNullOrEmpty($result.LayerStationDoor))) {
            $result.LayerStationDoor = $text
        }
    }
}

Write-Host "使用单位名称: $($result.UserName)" -ForegroundColor Green
Write-Host "设备品种: $($result.DeviceType)" -ForegroundColor Green
Write-Host "安装地点: $($result.UsingAddress)" -ForegroundColor Green
Write-Host "产品型号: $($result.Model)" -ForegroundColor Green
Write-Host "产品编号: $($result.SerialNum)" -ForegroundColor Green
Write-Host "制造单位名称: $($result.ManufacturingUnit)" -ForegroundColor Green
Write-Host "维护保养单位名称: $($result.MaintenanceUnit)" -ForegroundColor Green
Write-Host "施工单位名称: $($result.ConstructionUnit)" -ForegroundColor Green
Write-Host "额定速度: $($result.Speed)" -ForegroundColor Green
Write-Host "层站门数: $($result.LayerStationDoor)" -ForegroundColor Green

Write-Host ""
Write-Host "=== 查找日期 ===" -ForegroundColor Cyan
$datePattern = "\d{4}年\d{1,2}月\d{0,2}日"
foreach ($cell in $allCells) {
    $text = $cell.words -replace "`n", "" -replace "`r", ""
    if ($text -match $datePattern) {
        $matches = [regex]::Matches($text, $datePattern)
        foreach ($match in $matches) {
            Write-Host "找到日期: $($match.Value)" -ForegroundColor Magenta
        }
    }
    if ($text -match "安装监检日期") {
        Write-Host "找到关键字: $text" -ForegroundColor Magenta
    }
}
