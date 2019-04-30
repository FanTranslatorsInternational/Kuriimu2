@echo off
echo ##################################
echo  Publish Kuriimu2
echo ##################################

set PROJECT=Alpha
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set MYDATE=%%c-%%a-%%b)
for /f "tokens=1-2 delims=/: " %%a in ('time /t') do (set MYTIME=%%a-%%b)

cd "dist\Debug"
call "C:\Program Files\WinRAR\rar.exe" a "Kuriimu2_%PROJECT%.rar" "*.exe*" "*.dll" "-x*criware*" "-x*e.x._troopers*" "-x*valkyria_chronicles*" "plugins\*.dll" "-xplugins\Kontract.dll" "-xplugins\Komponent.dll" "-xplugins\Kryptography.dll" "-xplugins\System.Numerics.Vectors.dll" "Libraries"
cd ../../
move "dist\Debug\Kuriimu2_%PROJECT%.rar" "Kuriimu2_%PROJECT%_%MYDATE%_%MYTIME%.rar"

pause