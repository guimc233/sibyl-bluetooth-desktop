@echo off
echo ===================================================
echo   SIBYL 耳机 Windows 10/11 控制客户端编译打包脚本
echo ===================================================
echo.

echo [1/3] 还原依赖中...
dotnet restore src/SibylEarbuds.App/SibylEarbuds.App.csproj

if %errorlevel% neq 0 (
    echo [错误] 还原依赖失败，请检查是否已安装 .NET 8.0 SDK。
    pause
    exit /b %errorlevel%
)

echo.
echo [2/3] 编译并发布单文件可执行程序 (win-x64)...
dotnet publish src/SibylEarbuds.App/SibylEarbuds.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./dist

if %errorlevel% neq 0 (
    echo [错误] 编译失败！
    pause
    exit /b %errorlevel%
)

echo.
echo ===================================================
echo   [成功] 编译完成！可执行文件已生成在 ./dist 目录:
echo   ./dist/SibylEarbudsManager.exe
echo ===================================================
echo.
pause
