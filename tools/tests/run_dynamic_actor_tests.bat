@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_DYNAMIC_ROOT=%%~fI"
call "%WW_DYNAMIC_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_DYNAMIC_BUILD=%WW_DYNAMIC_ROOT%\build\dynamic_actor_tests"
if not exist "%WW_DYNAMIC_BUILD%" mkdir "%WW_DYNAMIC_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /wd4505 /utf-8 "%~dp0dynamic_actor_tests.cpp" /Fo:"%WW_DYNAMIC_BUILD%\dynamic.obj" /Fe:"%WW_DYNAMIC_BUILD%\dynamic_tests.exe" /link user32.lib psapi.lib bcrypt.lib
if errorlevel 1 exit /b 1
"%WW_DYNAMIC_BUILD%\dynamic_tests.exe"
exit /b %errorlevel%
