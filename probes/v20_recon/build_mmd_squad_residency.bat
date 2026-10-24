@echo off
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 goto failed
if not exist build\mmd_squad mkdir build\mmd_squad
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD mmd_squad_residency.cpp /Fo:build\mmd_squad\mmd_squad.obj /Fe:mmd_squad_residency.dll /link bcrypt.lib psapi.lib user32.lib
if errorlevel 1 goto failed
popd
exit /b 0
:failed
popd
exit /b 1
