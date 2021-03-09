@echo off

echo Postbuild script: %0

setlocal
set ProjectDir=%~dp0
if not exist "%ProjectDir%copyfiles.flag" goto _end
echo Copying files...
copy /B /Y "%ProjectDir%bin\Release\netstandard2.0\SOSIEL-v3.0.0.*" "C:\Program Files\LANDIS-II-v7\extensions"
:_end
endlocal
