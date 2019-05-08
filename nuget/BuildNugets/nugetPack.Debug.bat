@echo off
echo ##################################
echo       Build Kuriimu2 Nugets
echo ##################################
echo.
echo ### Build Kuriimu2 Libraries ###
echo.
echo Building Kuriimu2.sln... 
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" (call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" ..\..\Kuriimu2.sln /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1) else (call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" ..\..\Kuriimu2.sln /p:Configuration=Debug /p:WarningLevel=0 > nul 2>&1)
echo.
echo ### Build Nugets ###
echo.
nuget.exe pack ..\..\src\Kontract\Kontract.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Komponent\Komponent.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Kanvas\Kanvas.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Kryptography\Kryptography.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Kore\Kore.csproj -Properties Configuration=Debug
copy /y *.nupkg ..\..\nuget\*
del *.nupkg
echo.
echo ##################################
echo       Build Nugets Complete
echo ##################################
echo.