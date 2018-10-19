@echo off

nuget.exe pack Kore.csproj -IncludeReferencedProjects -Properties Configuration=Debug
copy /y *.nupkg nuget\*
del *.nupkg

pause