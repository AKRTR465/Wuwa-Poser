@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_RETIRE_ROOT=%%~fI"
call "%WW_RETIRE_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_RETIRE_BUILD=%WW_RETIRE_ROOT%\build\session_retirement_tests"
if not exist "%WW_RETIRE_BUILD%" mkdir "%WW_RETIRE_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /wd4505 /utf-8 "%~dp0session_retirement_tests.cpp" /Fo:"%WW_RETIRE_BUILD%\retirement.obj" /Fe:"%WW_RETIRE_BUILD%\retirement_tests.exe" /link user32.lib psapi.lib bcrypt.lib
if errorlevel 1 exit /b 1
"%WW_RETIRE_BUILD%\retirement_tests.exe"
exit /b %errorlevel%
