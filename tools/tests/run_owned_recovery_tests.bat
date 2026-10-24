@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_RECOVERY_ROOT=%%~fI"
call "%WW_RECOVERY_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_RECOVERY_BUILD=%WW_RECOVERY_ROOT%\build\owned_recovery_tests"
if not exist "%WW_RECOVERY_BUILD%" mkdir "%WW_RECOVERY_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /utf-8 "%~dp0owned_recovery_tests.cpp" /Fo:"%WW_RECOVERY_BUILD%\recovery.obj" /Fe:"%WW_RECOVERY_BUILD%\recovery_tests.exe"
if errorlevel 1 exit /b 1
"%WW_RECOVERY_BUILD%\recovery_tests.exe"
exit /b %errorlevel%
