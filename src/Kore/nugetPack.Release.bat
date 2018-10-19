@echo off

nuget.exe pack Kore.csproj -IncludeReferencedProjects -Properties Configuration=Release
copy /y *.nupkg nuget\*
del *.nupkg

pause