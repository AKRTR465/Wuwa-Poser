@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_CAMERA_ROOT=%%~fI"
call "%WW_CAMERA_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_CAMERA_BUILD=%WW_CAMERA_ROOT%\build\camera_diagnostics_tests"
if not exist "%WW_CAMERA_BUILD%" mkdir "%WW_CAMERA_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /utf-8 "%~dp0camera_diagnostics_tests.cpp" /Fo:"%WW_CAMERA_BUILD%\diagnostics.obj" /Fe:"%WW_CAMERA_BUILD%\diagnostics_tests.exe"
if errorlevel 1 exit /b 1
"%WW_CAMERA_BUILD%\diagnostics_tests.exe"
exit /b %errorlevel%
