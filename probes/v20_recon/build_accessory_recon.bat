@echo off
setlocal
cd /d "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD accessory_recon_carrier.cpp /Fo:build\accessory_recon.obj /Fe:accessory_recon.dll /link bcrypt.lib psapi.lib user32.lib
exit /b %errorlevel%
