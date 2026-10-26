@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_WITNESS_ROOT=%%~fI"
call "%WW_WITNESS_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_WITNESS_BUILD=%WW_WITNESS_ROOT%\build\native_function_witness_tests"
if not exist "%WW_WITNESS_BUILD%" mkdir "%WW_WITNESS_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /wd4505 /utf-8 "%~dp0native_function_witness_tests.cpp" /Fo:"%WW_WITNESS_BUILD%\witness.obj" /Fe:"%WW_WITNESS_BUILD%\witness_tests.exe" /link user32.lib psapi.lib
if errorlevel 1 exit /b 1
"%WW_WITNESS_BUILD%\witness_tests.exe" "%WW_WITNESS_BUILD%\evidence"
exit /b %errorlevel%
