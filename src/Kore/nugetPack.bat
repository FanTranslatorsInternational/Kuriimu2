@echo off

call nuget.exe pack Kore.csproj -IncludeReferencedProjects -Properties Configuration=Release

pause