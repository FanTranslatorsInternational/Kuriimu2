@echo off
echo ##################################
echo    Build All Kuriimu2 Solutions
echo ##################################
echo.
echo ### Restore Nugets ###
echo.
for /r %%i in (*.sln) do (echo Restoring nugets for %%i & call "nuget\BuildNugets\nuget.exe" %%i > nul 2>&1)
echo.
echo ### Build Solutions ###
echo.
for /r %%i in (*.sln) do (echo Building %%i... & call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" %%i /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1)
echo.
echo ##################################
echo         Build All Complete
echo ##################################
echo.
pause