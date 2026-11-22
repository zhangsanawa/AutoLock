@echo off
chcp 65001 >nul
setlocal

set FW=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319
if not exist "%FW%\csc.exe" (
    echo [错误] 找不到 C# 编译器: %FW%\csc.exe
    echo 请确认已安装 .NET Framework 4.x
    pause
    exit /b 1
)

echo 正在编译 AutoLock.exe ...

"%FW%\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ ^
  /out:AutoLock.exe ^
  /win32icon:app.ico ^
  /win32manifest:app.manifest ^
  /resource:app.ico,app.ico ^
  /r:"%FW%\System.dll" ^
  /r:"%FW%\System.Drawing.dll" ^
  /r:"%FW%\System.Windows.Forms.dll" ^
  /r:"%FW%\System.Core.dll" ^
  AutoLock.cs

if errorlevel 1 (
    echo.
    echo [失败] 编译出错
    pause
    exit /b 1
)

echo.
echo [成功] 已生成 AutoLock.exe
echo.
pause
