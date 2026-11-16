@echo off
rem Builds and runs native regression tests: run_native_tests.bat NAME [NAME...]
rem NAME selects tools\tests\NAME_tests.cpp. Each test receives the repository
rem root and its own build directory as arguments.
setlocal EnableExtensions
set "WW_TESTS=%~dp0"
for %%I in ("%~dp0..\..") do set "WW_ROOT=%%~fI"
call "%WW_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
:next
if "%~1"=="" exit /b 0
set "WW_BUILD=%WW_ROOT%\build\native_tests\%~1"
if not exist "%WW_BUILD%" mkdir "%WW_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /utf-8 "%WW_TESTS%%~1_tests.cpp" /Fo:"%WW_BUILD%\test.obj" /Fe:"%WW_BUILD%\test.exe" /link user32.lib psapi.lib bcrypt.lib
if errorlevel 1 exit /b 1
"%WW_BUILD%\test.exe" "%WW_ROOT%" "%WW_BUILD%"
if errorlevel 1 exit /b 1
shift
goto next
