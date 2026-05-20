@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "SLN=NohBoard\NohBoard.sln"
set "CSPROJ=NohBoard\NohBoard\NohBoard.csproj"
set "EXE_NAME=bm-nohboard.exe"
set "VER_SRC=%~dp0NohBoard\NohBoard\Version.cs.template"
if not exist "%VER_SRC%" set "VER_SRC=%~dp0NohBoard\NohBoard\Version.cs"
if not exist "%VER_SRC%" (
  echo [build_win10] FAIL: missing Version.cs.template / Version.cs
  goto :end_fail
)
for /f "usebackq tokens=2 delims==" %%A in (`findstr /C:"public const string Get" "%VER_SRC%"`) do set "VER_RAW=%%A"
if not defined VER_RAW (
  echo [build_win10] FAIL: could not read version from %VER_SRC%
  goto :end_fail
)
set "VER_RAW=%VER_RAW: =%"
set "VER_RAW=%VER_RAW:"=%"
set "VER_RAW=%VER_RAW:v=%"
set "VER_RAW=%VER_RAW:;=%"
set "VER_LABEL=V%VER_RAW%"
if "%VER_RAW%"=="" (
  echo [build_win10] FAIL: invalid version in %VER_SRC%
  goto :end_fail
)
set "ZIP_NAME=bm-nohboard_windows_x64-%VER_LABEL%.zip"
set "PUBTMP=%~dp0dist\_publish"
set "BUNDLE=%~dp0dist\_bundle"

echo [build_win10] NohBoard Win10/x64: self-contained publish ^(.NET bundled^) + dist\%ZIP_NAME%

if not exist "%~dp0NohBoard\gotri.exe" (
  echo [build_win10] FAIL: missing NohBoard\gotri.exe
  goto :end_fail
)

taskkill /F /IM "%EXE_NAME%" /T >nul 2>&1

set "STRAY_RESX=NohBoard\NohBoard\Forms\MainForm.Editmode.resx"
if exist "%STRAY_RESX%" (
  echo [build_win10] removing stray %STRAY_RESX%
  del /f /q "%STRAY_RESX%" >nul 2>&1
)

if not exist "dist" mkdir "dist" 2>nul
call :clean_dir_contents "dist"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [build_win10] FAIL: dotnet SDK not found in PATH
  goto :end_fail
)

echo [build_win10] using:
dotnet --version

dotnet restore "%SLN%"
if errorlevel 1 (
  echo [build_win10] FAIL: dotnet restore
  goto :end_fail
)

dotnet publish "%CSPROJ%" -c Release --self-contained true "/p:SolutionDir=%~dp0NohBoard/" -o "%PUBTMP%"
if errorlevel 1 (
  echo [build_win10] FAIL: dotnet publish
  goto :end_fail
)

if not exist "%PUBTMP%\%EXE_NAME%" (
  echo [build_win10] FAIL: missing %PUBTMP%\%EXE_NAME%
  goto :end_fail
)

mkdir "%BUNDLE%" 2>nul
copy /y "%PUBTMP%\%EXE_NAME%" "%BUNDLE%\%EXE_NAME%" >nul
if errorlevel 1 (
  echo [build_win10] FAIL: copy exe to bundle
  goto :end_fail
)

if not exist "keyboards" (
  echo [build_win10] FAIL: missing keyboards\
  goto :end_fail
)

xcopy /e /i /q /y "keyboards" "%BUNDLE%\keyboards\" >nul
if errorlevel 4 (
  echo [build_win10] FAIL: xcopy keyboards
  goto :end_fail
)

if exist "%~dp0dist\%ZIP_NAME%" del /f /q "%~dp0dist\%ZIP_NAME%" >nul 2>&1

echo [build_win10] archiving ^(exe + keyboards^)

where 7z >nul 2>&1
if not errorlevel 1 (
  pushd "%BUNDLE%" >nul
  7z a -tzip "%~dp0dist\%ZIP_NAME%" *
  if errorlevel 1 (
    popd >nul
    goto try_tar
  )
  popd >nul
  if exist "%~dp0dist\%ZIP_NAME%" goto zip_ok
)

:try_tar
where tar >nul 2>&1
if not errorlevel 1 (
  pushd "%BUNDLE%" >nul
  tar -caf "%~dp0dist\%ZIP_NAME%" *
  if errorlevel 1 (
    popd >nul
    goto try_ps_zip
  )
  popd >nul
  if exist "%~dp0dist\%ZIP_NAME%" goto zip_ok
)

:try_ps_zip
powershell -NoProfile -Command "Set-Location -LiteralPath '%BUNDLE%'; Compress-Archive -Path '*' -DestinationPath '%~dp0dist\%ZIP_NAME%' -Force"
if errorlevel 1 goto zip_warn
if exist "%~dp0dist\%ZIP_NAME%" goto zip_ok

:zip_warn
echo [build_win10] WARN: zip failed
echo [build_win10] OK: staged folder remains - %BUNDLE%
goto :end_ok

:zip_ok
call :clean_dir_contents "%PUBTMP%"
rd /s /q "%PUBTMP%" 2>nul
call :clean_dir_contents "%BUNDLE%"
rd /s /q "%BUNDLE%" 2>nul

echo [build_win10] OK: %~dp0dist\%ZIP_NAME%

:end_ok
if /i "%~1"=="nopause" exit /b 0
echo.
pause
exit /b 0

:clean_dir_contents
set "TGT=%~1"
if not exist "%TGT%" exit /b 0
for /f "delims=" %%D in ('dir /b /ad "%TGT%" 2^>nul') do rd /s /q "%TGT%\%%D" 2>nul
del /f /q "%TGT%\*" 2>nul
exit /b 0

:end_fail
if /i "%~1"=="nopause" exit /b 1
echo.
pause
exit /b 1
