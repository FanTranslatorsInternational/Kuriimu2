@echo off
rem ##################################
rem  Build All Kuriimu2 Solutions
rem ##################################

rem Kuriimu2
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" Kuriimu2.sln /p:Configuration=Debug /p:WarningLevel=0

rem Plugins
for /r %%i in (*.sln) do (echo %%i & call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MsBuild.exe" %%i /p:Configuration=Debug /p:WarningLevel=0)