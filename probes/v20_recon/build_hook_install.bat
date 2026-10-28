@echo off
setlocal
cd /d "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
if not exist build mkdir build
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /D_CRT_SECURE_NO_WARNINGS hook_install_v20.cpp /Fo:build\hook_install_v20.obj /Fe:hook_install_v20.exe /link user32.lib
exit /b %errorlevel%
