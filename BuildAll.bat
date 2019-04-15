@echo off
echo ##################################
echo    Build All Kuriimu2 Solutions
echo ##################################
echo.
echo ### Restore Nugets ###
echo.
choice /m "Reinstall nugets from scratch (3..2..1..N)" /t 3 /d N
if errorlevel 2 goto restore
if errorlevel 1 goto clean
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
choice /m "Build all solutions (3..2..1..Y)" /t 3 /d Y
if errorlevel 2 goto skip
if errorlevel 1 goto build
:build
for /r %%i in (*.sln) do (echo Building %%i... & @if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" (call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" %%i /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1) else (call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" %%i /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1))
:skip
echo.
echo ##################################
echo         Build All Complete
echo ##################################
echo.
pause