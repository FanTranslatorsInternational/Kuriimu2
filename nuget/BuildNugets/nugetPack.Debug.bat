@echo off

set com2019="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe"
set pro2019="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe"
set com2017="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\amd64\MSBuild.exe"

set msbuild=""
if exist %com2019% (set msbuild=%com2019%) ^
else if exist %pro2019% (set msbuild=%pro2019%) ^
else if exist %com2017% (set msbuild=%com2017%)
if %msbuild%=="" goto exit

echo ##################################
echo       Build Kuriimu2 Nugets
echo ##################################
echo.
echo ### Build Kuriimu2 Libraries ###
echo.

call %msbuild% ..\..\src\Kanvas\Kanvas.csproj /p:Configuration=Debug /p:WarningLevel=0 > nul
call %msbuild% ..\..\src\Komponent\Komponent.csproj /p:Configuration=Debug /p:WarningLevel=0 > nul
call %msbuild% ..\..\src\Kompression\Kompression.csproj /p:Configuration=Debug /p:WarningLevel=0 > nul
call %msbuild% ..\..\src\Kontract\Kontract.csproj /p:Configuration=Debug /p:WarningLevel=0 > nul
call %msbuild% ..\..\src\Kore\Kore.csproj /p:Configuration=Debug /p:WarningLevel=0 > nul
call %msbuild% ..\..\src\Kryptography\Kryptography.csproj /p:Configuration=Debug /p:WarningLevel=0 > nul

echo.
echo ### Copy Nugets ###
echo.

copy /y ..\..\src\Kanvas\bin\Debug\*.nupkg ..\..\nuget\*
copy /y ..\..\src\Komponent\bin\Debug\*.nupkg ..\..\nuget\*
copy /y ..\..\src\Kompression\bin\Debug\*.nupkg ..\..\nuget\*
copy /y ..\..\src\Kontract\bin\Debug\*.nupkg ..\..\nuget\*
copy /y ..\..\src\Kore\bin\Debug\*.nupkg ..\..\nuget\*
copy /y ..\..\src\Kryptography\bin\Debug\*.nupkg ..\..\nuget\*

echo.
echo ##################################
echo       Build Nugets Complete
echo ##################################
echo.

pause

:exit