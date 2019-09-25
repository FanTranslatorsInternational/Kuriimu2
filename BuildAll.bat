@echo off

set com2019="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe"
set pro2019="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe"
set com2017="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\amd64\MSBuild.exe"

echo ##################################
echo    Build All Kuriimu2 Solutions
echo ##################################
echo.
echo ### Restore Nugets ###
echo.

choice /c YN /m "Reinstall nugets from scratch" /t 3 /d N
if errorlevel 1 goto clean
if errorlevel 2 goto restore

:clean
for /r /d %%i in (packages) do @if exist "%%i" (echo Cleaning "%%i"... & rd /s /q "%%i")
for /r %%i in (*.sln) do (echo Restoring nugets for %%i & call "nuget\BuildNugets\nuget.exe" restore "%%i" -NoCache > nul 2>&1)
goto build

:restore
for /r %%i in (*.sln) do (echo Restoring nugets for %%i & call "nuget\BuildNugets\nuget.exe" restore "%%i" > nul 2>&1)

:build
echo.
echo ### Build Solutions ###
echo.

choice /c YN /m "Build all solutions" /t 3 /d Y
if errorlevel 1 goto build
if errorlevel 2 goto skip

:build
for /r %%i in (*.sln) do (echo Building %%i... & @if exist %com2019% (call %com2019% %%i /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1) ^
else if exist %pro2019% (call %pro2019% %%i /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1) ^
else (call %com2017% %%i /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1))

:skip
echo.
echo ##################################
echo         Build All Complete
echo ##################################
echo.

pause