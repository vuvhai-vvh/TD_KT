param(
    [Parameter(Mandatory = $false)]
    [string]$ReleaseFolder = (Join-Path $PSScriptRoot "..\..\TD_KT\bin\Release"),

    [Parameter(Mandatory = $true)]
    [string]$ServerFolder,

    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$Description = "Cập nhật phần mềm TD-KT",

    [Parameter(Mandatory = $false)]
    [bool]$Mandatory = $true
)

$ErrorActionPreference = "Stop"

$releasePath = [System.IO.Path]::GetFullPath($ReleaseFolder)
$applicationPath = Join-Path $releasePath "TD_KT.exe"
$updaterPath = Join-Path $releasePath "TD_KT.Updater.exe"

if (-not (Test-Path $applicationPath)) {
    throw "Không tìm thấy TD_KT.exe tại: $applicationPath. Hãy Build cấu hình Release trước."
}

if (-not (Test-Path $updaterPath)) {
    throw "Không tìm thấy TD_KT.Updater.exe tại: $updaterPath. Hãy Build toàn bộ Solution trước."
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($applicationPath).FileVersion
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Không xác định được phiên bản của TD_KT.exe."
}

New-Item -ItemType Directory -Path $ServerFolder -Force | Out-Null

$workRoot = Join-Path $env:TEMP ("TDKT_Publish_" + [Guid]::NewGuid().ToString("N"))
$stagingFolder = Join-Path $workRoot "Package"
New-Item -ItemType Directory -Path $stagingFolder -Force | Out-Null

try {
    $preserveNames = @("Files", "Logs", "Backup", "TD_KT.exe.config")

    Get-ChildItem -LiteralPath $releasePath -Force | ForEach-Object {
        if ($preserveNames -notcontains $_.Name) {
            $destination = Join-Path $stagingFolder $_.Name
            if ($_.PSIsContainer) {
                Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force
            }
            else {
                Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
            }
        }
    }

    $packageName = "TD_KT_{0}.zip" -f $Version
    $packagePath = Join-Path $ServerFolder $packageName

    if (Test-Path $packagePath) {
        Remove-Item -LiteralPath $packagePath -Force
    }

    Compress-Archive -Path (Join-Path $stagingFolder "*") -DestinationPath $packagePath -CompressionLevel Optimal

    $hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash

    $manifest = [ordered]@{
        application = "TD_KT"
        version = $Version
        package = $packageName
        mandatory = $Mandatory
        sha256 = $hash
        description = $Description
    }

    $manifestPath = Join-Path $ServerFolder "manifest.json"
    $manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    Write-Host "Đã tạo bản cập nhật thành công:" -ForegroundColor Green
    Write-Host "  Gói cập nhật: $packagePath"
    Write-Host "  Manifest:     $manifestPath"
    Write-Host "  Phiên bản:    $Version"
    Write-Host "  SHA-256:      $hash"
}
finally {
    if (Test-Path $workRoot) {
        Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
