@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_COMPAT_ROOT=%%~fI"
call "%WW_COMPAT_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_COMPAT_BUILD=%WW_COMPAT_ROOT%\build\game_compat_resolution_tests"
if not exist "%WW_COMPAT_BUILD%" mkdir "%WW_COMPAT_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /utf-8 "%~dp0game_compat_resolution_tests.cpp" /Fo:"%WW_COMPAT_BUILD%\resolution.obj" /Fe:"%WW_COMPAT_BUILD%\resolution_tests.exe"
if errorlevel 1 exit /b 1
"%WW_COMPAT_BUILD%\resolution_tests.exe"
exit /b %errorlevel%
