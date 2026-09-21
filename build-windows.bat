@echo off
setlocal
echo ===================================================
echo   SIBYL 耳机 Windows 控制客户端 (WinUI 3) 构建脚本
echo ===================================================
echo.

echo [1/3] 检查 .NET SDK...
dotnet --version
if %errorlevel% neq 0 (
    echo [错误] 未检测到 .NET SDK，请先安装 .NET 8.0 SDK。
    pause
    exit /b %errorlevel%
)

echo.
echo [2/3] 运行核心协议单元测试...
dotnet test tests\SibylEarbuds.Core.Tests\SibylEarbuds.Core.Tests.csproj -c Release
if %errorlevel% neq 0 (
    echo [错误] 单元测试失败。
    pause
    exit /b %errorlevel%
)

echo.
echo [3/3] 发布 WinUI 3 自包含绿色应用 (win-x64)...
dotnet publish src\SibylEarbuds.App\SibylEarbuds.App.csproj -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:PublishSingleFile=false -o .\dist\win-x64
if %errorlevel% neq 0 (
    echo [错误] 发布失败！
    pause
    exit /b %errorlevel%
)

echo.
echo ===================================================
echo   [成功] 构建完成！请运行 .\dist\win-x64\SibylEarbudsManager.exe
echo ===================================================
echo.
pause
