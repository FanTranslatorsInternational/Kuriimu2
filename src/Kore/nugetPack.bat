@echo off

set /p version="Version: "

call nuget.exe pack Kore.csproj -IncludeReferencedProjects -Properties Configuration=Release

set /p version="Version: "