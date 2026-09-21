<#
.SYNOPSIS
    构建 SIBYL 耳机 Windows 控制客户端 (WinUI 3 / Windows App SDK)。

.DESCRIPTION
    输出为「非打包 + 自包含」的绿色版本，目标机器无需安装
    Windows App Runtime 或 .NET 运行时，双击 exe 即可运行。

.EXAMPLE
    .\build-windows.ps1
    .\build-windows.ps1 -Arch arm64
#>
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Arch = 'x64'
)

$ErrorActionPreference = 'Stop'

$platform = if ($Arch -eq 'arm64') { 'ARM64' } else { 'x64' }
$rid = "win-$Arch"
$outDir = "./dist/$rid"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  SIBYL 耳机 Windows 控制客户端 (WinUI 3) 构建脚本" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan

Write-Host "`n[1/3] 检查 .NET SDK..." -ForegroundColor Yellow
dotnet --version

Write-Host "`n[2/3] 运行核心协议单元测试..." -ForegroundColor Yellow
dotnet test tests/SibylEarbuds.Core.Tests/SibylEarbuds.Core.Tests.csproj -c Release

Write-Host "`n[3/3] 发布 WinUI 3 自包含绿色应用 ($rid / $platform)..." -ForegroundColor Yellow
dotnet publish src/SibylEarbuds.App/SibylEarbuds.App.csproj `
    -c Release `
    -r $rid `
    -p:Platform=$platform `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishSingleFile=false `
    -o $outDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] 构建成功！请运行 $outDir/SibylEarbudsManager.exe" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] 构建失败，请确认已安装 .NET 8.0 SDK 与 Windows App SDK 相关组件" -ForegroundColor Red
    exit $LASTEXITCODE
}
