@echo off

echo.
echo ##################################
echo  Publish Kuriimu2
echo ##################################

set arch32=0
set arch64=0

:publish
if %arch32%==0 (
	set arch32=1
	set arch="x86"
	set bin="bin32"
	
	echo.
	echo ### Publish Kuriimu2 x86 ###
	echo.
	
	goto build
)
if %arch64%==0 (
	set arch64=1
	set arch="x64"
	set bin="bin64"
	
	echo.
	echo ### Publish Kuriimu2 x64 ###
	echo.
	
	goto build
)
goto end

:build
set PROJECT=Alpha
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set MYDATE=%%c-%%a-%%b)
for /f "tokens=1-2 delims=/: " %%a in ('time /t') do (set MYTIME=%%a-%%b)

cd "dist\Debug\net472"
copy "lib\%bin%\*" "lib"

call "C:\Program Files\WinRAR\rar.exe" a "Kuriimu2_%PROJECT%_%arch%.rar" "*.exe" "*.dll" "plugins\*.dll" "lib\*.dll" "lib\*.so" "-xplugins\Kontract.dll" "-xplugins\Komponent.dll" "-xplugins\Kryptography.dll" "-xplugins\Kompression.dll" "-xplugins\Kanvas.dll" "-xplugins\System.Numerics.Vectors.dll" ^"-xplugins\lib" "-x*criware*" "-x*e.x._troopers*" "-x*valkyria_chronicles*"

del "lib\**"
cd ../../../

move "dist\Debug\net472\Kuriimu2_%PROJECT%_%arch%.rar" "Kuriimu2_%PROJECT%_%arch%_%MYDATE%_%MYTIME%.rar"
goto publish

:end
echo ### Published %PROJECT% Builds ###
pause
exit 0