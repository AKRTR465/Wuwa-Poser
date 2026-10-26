@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_ANCHORS_ROOT=%%~fI"
call "%WW_ANCHORS_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_ANCHORS_BUILD=%WW_ANCHORS_ROOT%\build\game_object_anchors_tests"
if not exist "%WW_ANCHORS_BUILD%" mkdir "%WW_ANCHORS_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /wd4505 /utf-8 "%~dp0game_object_anchors_tests.cpp" /Fo:"%WW_ANCHORS_BUILD%\anchors.obj" /Fe:"%WW_ANCHORS_BUILD%\anchors.exe" /link user32.lib psapi.lib
if errorlevel 1 exit /b 1
"%WW_ANCHORS_BUILD%\anchors.exe"
exit /b %errorlevel%
