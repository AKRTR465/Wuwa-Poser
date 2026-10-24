@echo off
setlocal
cd /d "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 exit /b 1
if not exist build mkdir build
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD publish_probe.cpp /Fo:build\publish_probe.obj /Fe:publish_probe_v21.dll /link /LTCG /NODEFAULTLIB:LIBCMT bcrypt.lib psapi.lib user32.lib
exit /b %errorlevel%
