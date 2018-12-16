@echo off

cd ..\..\
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" Kuriimu2.sln /p:Configuration=Debug /p:WarningLevel=0
cd nuget\Build
nuget.exe pack ..\..\src\Kontract\Kontract.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Komponent\Komponent.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Kanvas\Kanvas.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Kryptography\Kryptography.csproj -Properties Configuration=Debug
nuget.exe pack ..\..\src\Kore\Kore.csproj -Properties Configuration=Debug
copy /y *.nupkg ..\..\nuget\*
del *.nupkg

pause