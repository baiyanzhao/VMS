@ECHO off
SET VersionText=%4
SET COMPILE_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

IF [%VersionText%]==[]  (@ECHO NOT EXIST VersionText & pause & GOTO :EOF)
IF [%COMPILE_PATH%]==[]  (@ECHO NOT EXIST MSBuild & pause & GOTO :EOF)
IF NOT EXIST %COMPILE_PATH% (@ECHO NOT EXIST MSBuild & GOTO :EOF)

@ECHO Compile MSBuild
for /r %%h in (*.sln) do if exist %%h echo %%h & %COMPILE_PATH% %%h /t:Clean;Publish /p:Configuration=Release /p:ApplicationVersion="%VersionText%"  /noconsolelogger >nul

rd ".\VMS\Publish\Application Files" /s /q >nul 2>nul
xcopy .\VMS\bin\Release\app.publish\*.* .\VMS\Publish\ /E /Y
::pause
