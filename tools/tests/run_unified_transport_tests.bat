@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_TEST_ROOT=%%~fI"
call "%WW_TEST_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_TEST_BUILD=%WW_TEST_ROOT%\build\unified_transport_tests"
if not exist "%WW_TEST_BUILD%" mkdir "%WW_TEST_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /utf-8 "%~dp0unified_transport_tests.cpp" /Fo:"%WW_TEST_BUILD%\transport.obj" /Fe:"%WW_TEST_BUILD%\transport_tests.exe"
if errorlevel 1 exit /b 1
"%WW_TEST_BUILD%\transport_tests.exe"
exit /b %errorlevel%
