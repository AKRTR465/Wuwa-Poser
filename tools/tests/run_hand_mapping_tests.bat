@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_HAND_ROOT=%%~fI"
call "%WW_HAND_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_HAND_BUILD=%WW_HAND_ROOT%\build\hand_mapping_tests"
if not exist "%WW_HAND_BUILD%" mkdir "%WW_HAND_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /utf-8 "%~dp0hand_mapping_tests.cpp" /Fo:"%WW_HAND_BUILD%\hand_mapping.obj" /Fe:"%WW_HAND_BUILD%\hand_mapping_tests.exe"
if errorlevel 1 exit /b 1
"%WW_HAND_BUILD%\hand_mapping_tests.exe"
exit /b %errorlevel%
