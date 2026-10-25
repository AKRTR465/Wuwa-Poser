@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_FOOT_ROOT=%%~fI"
call "%WW_FOOT_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_FOOT_BUILD=%WW_FOOT_ROOT%\build\foot_calibration_tests"
if not exist "%WW_FOOT_BUILD%" mkdir "%WW_FOOT_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /utf-8 "%~dp0foot_calibration_tests.cpp" /Fo:"%WW_FOOT_BUILD%\foot_calibration.obj" /Fe:"%WW_FOOT_BUILD%\foot_calibration_tests.exe"
if errorlevel 1 exit /b 1
"%WW_FOOT_BUILD%\foot_calibration_tests.exe"
exit /b %errorlevel%
