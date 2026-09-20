Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  SIBYL 耳机 Windows 10/11 控制客户端编译脚本" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan

Write-Host "`n[1/2] 检查 .NET SDK..." -ForegroundColor Yellow
dotnet --version

Write-Host "`n[2/2] 正在编译并输出独立单文件绿色客户端..." -ForegroundColor Yellow
dotnet publish src/SibylEarbuds.App/SibylEarbuds.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -o ./dist

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] 编译成功！请在 ./dist 目录运行 SibylEarbudsManager.exe" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] 编译失败，请确认系统已安装 .NET 8.0 SDK (windows桌面组件)" -ForegroundColor Red
}
