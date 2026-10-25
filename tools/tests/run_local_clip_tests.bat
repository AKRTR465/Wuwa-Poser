@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..") do set "WW_CLIP_ROOT=%%~fI"
call "%WW_CLIP_ROOT%\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
set "WW_CLIP_BUILD=%WW_CLIP_ROOT%\build\local_clip_tests"
if not exist "%WW_CLIP_BUILD%" mkdir "%WW_CLIP_BUILD%"
cl /nologo /std:c++17 /EHsc /W4 /WX /utf-8 "%~dp0local_clip_tests.cpp" /Fo:"%WW_CLIP_BUILD%\local_clip.obj" /Fe:"%WW_CLIP_BUILD%\local_clip_tests.exe"
if errorlevel 1 exit /b 1
"%WW_CLIP_BUILD%\local_clip_tests.exe"
exit /b %errorlevel%
