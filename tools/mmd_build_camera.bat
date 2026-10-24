@echo off
setlocal
cd /d "%~dp0"
call "%~dp0msvc_env.bat"
if errorlevel 1 exit /b 1
if not exist mmd_outputs mkdir mmd_outputs
cl /nologo /std:c++17 /O2 /W4 /WX /MD /EHsc /utf-8 mmd_camera_compile.cpp /Fe:mmd_outputs\mmd_camera_compile.exe /Fo:mmd_outputs\mmd_camera_compile.obj
exit /b %errorlevel%

